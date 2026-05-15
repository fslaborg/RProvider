namespace RProvider.Runtime

open System.Collections.Generic
open RBridge
open RBridge.Extensions
open RBridge.Extensions.ActivePatterns
open RProvider.Common
open RProvider.Common.Serialisation
open RProvider.Runtime
open RProvider

/// [omit]
/// The layer that the type provider accesses to
/// interop with R.
module RInterop =

    // TODO Move to more sensible location.
    type private ResultBuilder() =
        member _.Bind(m, f) = Result.bind f m
        member _.Return(x) = Ok x
        member _.ReturnFrom(m: Result<_, _>) = m
        member _.Zero() = Ok()
        member _.Delay(f) = f ()

    let private result = ResultBuilder()

    let private ofOption errMsg =
        function
        | Some o -> Ok o
        | None -> Error errMsg

    /// List packages available in the loaded R instance.
    let getPackages () : string [] =
        LogFile.logf "Communicating with R to get packages"
        let globEnv = Environment.globalEnv Singletons.engine.Value

        match Evaluate.eval globEnv ".packages(all.available=T)" with
        | Error e ->
            LogFile.logf "Failed to get packages from R: %s" e
            Array.empty
        | Ok v ->
            match v with
            | CharacterVector Singletons.engine.Value v ->
                v
                |> Extract.extractStringArray Singletons.engine.Value
                |> Array.choose id
            | _ -> failwith "Unexpected result getting packages"

    /// Get the description for a particular package from R.
    let getPackageDescription packageName : string =
        let globEnv = Environment.globalEnv Singletons.engine.Value

        Evaluate.eval globEnv (sprintf "packageDescription(\"%s\")$Description" packageName)
        |> Result.bind (SymbolicExpression.tryGetValue >> ofOption "[Could not extract package description]")
        |> Result.defaultValue "[Could not get package description from R]"

    /// Read a package's metadata to extract descriptions
    /// of each user-facing function.
    let getFunctionDescriptions packageName : (string * string) array =
        let globEnv = Environment.globalEnv Singletons.engine.Value

        let evalStringArray expr : Result<string [], string> =
            Evaluate.eval globEnv expr
            |> Result.bind (SymbolicExpression.tryGetValue >> ofOption "Could not extract to string array")

        result {
            do! Evaluate.exec globEnv $"rds <- readRDS(system.file('Meta', 'Rd.rds', package = '{packageName}'))"
            let! names = evalStringArray "rds$Name"
            let! titles = evalStringArray "rds$Title"
            return Array.zip names titles
        }
        |> Result.defaultValue [||]

    let loadPackage packageName : unit =
        if not (Singletons.loadedPackages.Contains packageName) then
            let globalEnv = Environment.globalEnv Singletons.engine.Value

            let result =
                Evaluate.eval globalEnv ("require(" + packageName + ")")
                |> Result.defaultWith (fun _ -> failwith "Failed to load package")

            match SymbolicExpression.tryGetValue<bool option> result |> ofOption "Failed to load package" with
            | Error e -> failwith e
            | Ok res ->
                match res with
                | Some res -> if not res then failwithf "Package %s not installed" packageName
                | None -> failwithf "Loading package %s failed" packageName

                Singletons.loadedPackages.Add packageName |> ignore

    /// Determines whether an expression is a value or a function.
    /// If a function, determines which arguments are available.
    let internal bindingInfo sexp : RValue =

        match sexp with
        | Closure Singletons.engine.Value clos ->
            let names =
                match Closures.tryFormals Singletons.engine.Value clos with
                | Some formals -> formals |> List.map (fun f -> f.Name)
                | None -> []

            let hasVarArgs = names |> List.contains "..."
            let args = names |> List.filter ((<>) "...")
            RValue.Function(args, hasVarArgs)

        | ActivePatterns.BuiltinFunction Singletons.engine.Value _
        | ActivePatterns.SpecialFunction Singletons.engine.Value _ ->
            // Don't know how to reflect on builtin or special args so just do as varargs
            RValue.Function([], true)

        | RealVector Singletons.engine.Value _
        | CharacterVector Singletons.engine.Value _
        | LogicalVector Singletons.engine.Value _
        | IntegerVector Singletons.engine.Value _
        | ComplexVector Singletons.engine.Value _
        | RawVector Singletons.engine.Value _
        | List Singletons.engine.Value _ -> RValue.Value

        | _ ->
            // LogFile.logf "Ignoring name of unknown SEXP: %A" (SymbolicExpression.print Singletons.engine.Value sexp)
            RValue.Value

    /// Get bindings representing the named and varargs of
    /// each function in a package.
    let getBindings (packageName: string) =

        // Get the package environment (not namespace environment)
        let pkgNs = Environment.ofNamespace Singletons.engine.Value packageName

        let globalEnv = Environment.globalEnv Singletons.engine.Value
        let names = 
            Call.callFuncByName Convert.toR globalEnv "base" "getNamespaceExports" [] [| pkgNs |]
            |> Result.map (Extract.extractStringArray Singletons.engine.Value >> Array.choose id)
            |> Result.defaultValue [||]

        let lazyData =
            Evaluate.eval globalEnv (sprintf "ls(loadNamespace(\"%s\")$.__NAMESPACE__.$lazydata)" packageName)
            |> Result.map (Extract.extractStringArray Singletons.engine.Value >> Array.choose id)
            |> Result.defaultValue [||]

        names
        |> Array.choose
            (fun name ->
                match Environment.tryGetValue Singletons.engine.Value pkgNs name with
                | None -> None
                | Some sexp ->
                    let forced = Promise.force Singletons.engine.Value sexp
                    let info = bindingInfo forced
                    Some(name, serializeRValue info))
        |> Array.append (lazyData |> Array.map(fun l -> l, serializeRValue RValue.Value))

    let globalEnvironment () = Environment.globalEnv Singletons.engine.Value

    /// Given an R environment scope, call a function given the
    /// named and unnamed arguments.
    let callFunc
        (rEnv: REnvironment)
        (fn: SymbolicExpression)
        (argsByName: seq<KeyValuePair<string, obj>>)
        (varArgs: obj [])
        : SymbolicExpression =

        Call.callFunc Convert.toR rEnv fn argsByName varArgs
        |> Result.defaultWith (fun e -> failwithf "Error in function: %s" e)

    /// Call an R function by name given a function name.
    let callFuncByName
        (rEnv: REnvironment)
        (packageName: string)
        (funcName: string)
        (namedArgs: seq<KeyValuePair<string, obj>>)
        (varArgs: obj array)
        : SymbolicExpression =

        Call.callFuncByName Convert.toR rEnv packageName funcName namedArgs varArgs
        |> Result.defaultWith (fun e -> failwithf "Error in function: %s" e)

    /// Call an R function given arguments, using serialised values (i.e.
    /// from the type provider itself over a socket).
    /// Always uses the global environment.
    let call
        (packageName: string)
        (funcName: string)
        (serializedRVal: string)
        (namedArgs: obj [])
        (varArgs: obj [])
        : SymbolicExpression =

        Call.call Convert.toR packageName funcName serializedRVal namedArgs varArgs
        |> Result.defaultWith (fun e -> failwithf "Error in function: %s" e)

    /// Checks if an R package is installed.
    let isPackageInstalled globEnv (pkgName: string) =
        try
            let res =
                callFuncByName globEnv "base" "requireNamespace"
                    (dict [
                        "package", box pkgName
                        "quietly", box true
                    ]) [||]
            res.FromR<bool>() = true
        with _ -> false


/// Printing using R's internal print() function.
module Printing =

    /// Print an R expression in R's console.
    let internal callPrint (env: REnvironment) (sexp: SymbolicExpression) =
        RInterop.callFuncByName env "base" "print" [] [| sexp |] |> ignore

    /// Sink redirects output from R into a file.
    let callSink (env: REnvironment) (file: string option) =
        match file with
        | Some path -> RInterop.callFuncByName env "base" "sink" (dict [ "file", box path ]) [||] |> ignore
        | None -> RInterop.callFuncByName env "base" "sink" [] [||] |> ignore

    /// Print by redirecting the output to a temp file.
    let internal printUsingTempFile (sexp: SymbolicExpression) =
        let temp = System.IO.Path.GetTempFileName()

        try
            let env = Environment.globalEnv Singletons.engine.Value
            callSink env (Some temp)
            callPrint env sexp
            callSink env None
            System.IO.File.ReadAllText temp
        finally
            System.IO.File.Delete temp


namespace RProvider

/// Contains helper functions for calling the functions generated by the R provider,
/// such as the `namedParams` function for specifying named parameters.
/// The module is automatically opened when you open the `RProvider` namespace.
[<AutoOpen>]
module Helpers =

    /// Construct a dictionary of named params to pass to an R function.
    ///
    /// ## Example
    /// For example, if you want to call the `R.plot` function with named parameters
    /// specifying `x`, `type`, `col` and `ylim`, you can use the following:
    ///
    ///     [ "x", box widgets;
    ///       "type", box "o";
    ///       "col", box "blue";
    ///       "ylim", box [0; 25] ]
    ///     |> namedParams |> R.plot
    ///
    let namedParams (s: seq<string * _>) = dict <| Seq.map (fun (n, v) -> n, box v) s
