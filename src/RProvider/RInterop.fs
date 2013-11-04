namespace RProvider

open Microsoft.FSharp.Reflection
open System
open System.ComponentModel.Composition
open System.ComponentModel.Composition.Hosting
open System.Reflection
open System.IO
open System.Diagnostics
open System.Numerics
open System.Threading
open System.Collections.Generic
open System.Linq
open RDotNet
open RDotNet.ActivePatterns

/// Interface to use via MEF
type IConvertToR<'inType> =     
    abstract member Convert : REngine * 'inType -> SymbolicExpression

// Support conversion to an explicitly requested type.
type IConvertFromR<'outType> =     
    abstract member Convert : SymbolicExpression -> Option<'outType>

// Supporting IDefaultConvertFromR indicates that you provide a default converter
type IDefaultConvertFromR =     
    abstract member Convert : SymbolicExpression -> Option<obj>

module Logging = 
  let logf fmt = 
    fmt |> Printf.kprintf (fun str -> 
      try
        let file = 
          let appd = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
          if not (Directory.Exists(appd + "\\RLogs")) then 
            Directory.CreateDirectory(appd + "\\RLogs") |> ignore
          appd + "\\RLogs\\log.txt"
        use fs = new FileStream(file, FileMode.OpenOrCreate, Security.AccessControl.FileSystemRights.AppendData, FileShare.Write, 4096, FileOptions.None)
        use writer = new StreamWriter(fs)
        writer.AutoFlush <- true
      
        let pid = Process.GetCurrentProcess().Id
        let tid = System.Threading.Thread.CurrentThread.ManagedThreadId
        let apid = System.AppDomain.CurrentDomain.Id
        writer.WriteLine(sprintf "[%s] [Pid:%d, Tid:%d, Apid:%d] %s" (System.DateTime.Now.ToString("G")) pid tid apid str)
      with _ -> (*silently ignoring logging errors*) () )

[<AutoOpen>]
module Helpers = 
    /// Construct named params to pass to function
    let namedParams (s: seq<string*_>) = dict <| Seq.map (fun (n,v) -> n, box v) s

module internal RInteropInternal =
    type RParameter = string
    type HasVarArgs = bool

    type RValue =
        | Function of RParameter list * HasVarArgs
        | Value

    [<Literal>] 
    let RDateOffset = 25569.

    let characterDevice = new CharacterDeviceInterceptor()

    let engine = 
        try
            Logging.logf "Creating instance" 
            let engine = REngine.CreateInstance(System.AppDomain.CurrentDomain.Id.ToString())
            Logging.logf "Intializing instance"
            do engine.Initialize(null, characterDevice)
            Logging.logf "Created & initialized instance"
            engine
        with
        | e -> 
            Logging.logf "Creating instance failed:\r\n  %O" e
            raise(Exception("Initialization of R.NET failed", e))

    do System.AppDomain.CurrentDomain.DomainUnload.Add(fun _ -> engine.Dispose())

    let private mefContainer = 
        lazy
            // Look for plugins co-located with RProvider.dll
            let assem = typeof<IConvertToR<_>>.Assembly
            let catalog = new DirectoryCatalog(Path.GetDirectoryName(assem.Location),"*.Plugin.dll")
            let assemCatalog = new AssemblyCatalog(assem)
            new CompositionContainer(new AggregateCatalog(catalog, assemCatalog))
                
    let internal toRConv = Collections.Generic.Dictionary<Type, REngine -> obj -> SymbolicExpression>()

    /// Register a function that will convert from a specific type to a value in R.
    /// Alternatively, you can build a MEF plugin that exports IConvertToR.
    /// registerToR is more suitable for experimentation in F# interactive.
    let registerToR<'inType> (conv: REngine -> 'inType -> SymbolicExpression) = 
        let conv' rengine (value: obj) = unbox value |> conv rengine 
        toRConv.[typeof<'inType>] <- conv'

    let internal convertToR<'inType> (engine: REngine) (value: 'inType) =
        let concreteType = value.GetType()
        let gt = typedefof<IConvertToR<_>>

        // Returns an ordered sequence of types that should be considered for the purpose of converting.
        // We look at interfaces introduced on the current type before traversing to the base type.
        let rec types (vt: Type) = seq {
            // First consider the type itself
            yield vt

            // Now consider interfaces implemented on this type that are not implemented on the base
            let baseInterfaces = if vt.BaseType = null then Array.empty else vt.BaseType.GetInterfaces()
            for iface in vt.GetInterfaces() do
                if not(baseInterfaces.Contains(iface)) then
                    yield iface

            // Now consider the base type (plus its interfaces etc.)
            if vt.BaseType <> null then
                yield! types vt.BaseType
        }

        // Try to get a converter for the given type        
        let tryGetConverter (vt: Type) = 
            // See if a MEF finds a converter for the type - these take precedence over built-ins
            let interfaceType = gt.MakeGenericType([|vt|])

            // If there are multiple plugins registered, we arbitrarily use the "first"
            match mefContainer.Value.GetExports(interfaceType, null, null).FirstOrDefault() with                   
            // Nothing from MEF, try to find a built-in
            | null ->   match toRConv.TryGetValue(vt) with
                        | (true, conv) -> Some conv
                        | _ -> None

            // Use MEF converter
            | conv ->   let convMethod = interfaceType.GetMethod("Convert")
                        Some(fun engine value -> convMethod.Invoke(conv.Value, [| engine; value |]) :?> SymbolicExpression )
        
        match Seq.tryPick tryGetConverter (types concreteType) with
        | Some conv -> conv engine value
        | None -> failwithf "No converter registered for type %s or any of its base types" concreteType.FullName
        
    let internal convertFromRBuiltins<'outType> (sexp: SymbolicExpression) : Option<'outType> = 
        let retype (x: 'b) : Option<'a> = x |> box |> unbox |> Some
        let at = typeof<'outType>
        match sexp with
        | CharacterVector(v) when at = typeof<string list>  -> retype <| List.ofSeq(v)
        | CharacterVector(v) when at = typeof<string[]>     -> retype <| v.ToArray()
        | CharacterVector(v) when at = typeof<string>       -> retype <| v.Single()
        | ComplexVector(v) when at = typeof<Complex list>   -> retype <| List.ofSeq(v)
        | ComplexVector(v) when at = typeof<Complex[]>      -> retype <| v.ToArray()
        | ComplexVector(v) when at = typeof<Complex>        -> retype <| v.Single()
        | IntegerVector(v) when at = typeof<int list>       -> retype <| List.ofSeq(v)
        | IntegerVector(v) when at = typeof<int[]>          -> retype <| v.ToArray()
        | IntegerVector(v) when at = typeof<int>            -> retype <| v.Single()
        | LogicalVector(v) when at = typeof<bool list>      -> retype <| List.ofSeq(v)
        | LogicalVector(v) when at = typeof<bool[]>         -> retype <| v.ToArray()
        | LogicalVector(v) when at = typeof<bool>           -> retype <| v.Single()
        | NumericVector(v) when at = typeof<double list>    -> retype <| List.ofSeq(v)
        | NumericVector(v) when at = typeof<double[]>       -> retype <| v.ToArray()
        | NumericVector(v) when at = typeof<double>         -> retype <| v.Single()
        | NumericVector(v) when at = typeof<DateTime list>  -> retype <| [ for n in v -> DateTime.FromOADate(n + RDateOffset) ]
        | NumericVector(v) when at = typeof<DateTime[]>     -> retype <| [| for n in v -> DateTime.FromOADate(n + RDateOffset) |]
        | NumericVector(v) when at = typeof<DateTime>       -> retype <| DateTime.FromOADate(v.Single() + RDateOffset)
        // Empty vectors in R are represented as null
        | Null() when at = typeof<string list>              -> retype <| List.empty<string>
        | Null() when at = typeof<string[]>                 -> retype <| Array.empty<string>
        | Null() when at = typeof<Complex list>             -> retype <| List.empty<Complex>
        | Null() when at = typeof<Complex[]>                -> retype <| Array.empty<Complex>
        | Null() when at = typeof<int list>                 -> retype <| List.empty<int>
        | Null() when at = typeof<int[]>                    -> retype <| Array.empty<int>
        | Null() when at = typeof<bool list>                -> retype <| List.empty<bool>
        | Null() when at = typeof<bool[]>                   -> retype <| Array.empty<bool>
        | Null() when at = typeof<double list>              -> retype <| List.empty<double>
        | Null() when at = typeof<double[]>                 -> retype <| Array.empty<double>
        | Null() when at = typeof<DateTime list>            -> retype <| List.empty<DateTime>
        | Null() when at = typeof<DateTime[]>               -> retype <| Array.empty<DateTime>

        | _                                                 -> None

    let internal convertFromR<'outType> (sexp: SymbolicExpression) : 'outType = 
        let concreteType = typeof<'outType>
        let vt = typeof<IConvertFromR<'outType>>

        let converters = mefContainer.Value.GetExports<IConvertFromR<'outType>>()
        match converters |> Seq.tryPick (fun conv -> conv.Value.Convert sexp) with
        | Some res  -> res
        | None      -> match convertFromRBuiltins<'outType> sexp with
                       | Some res -> res
                       | _ ->  failwithf "No converter registered to convert from R %s to type %s" (sexp.Type.ToString()) concreteType.FullName

    let internal defaultConvertFromRBuiltins (sexp: SymbolicExpression) : Option<obj> = 
        let wrap x = box x |> Some
        match sexp with
        | CharacterVector(v) ->     wrap <| v.ToArray()
        | ComplexVector(v) ->       wrap <| v.ToArray()
        | IntegerVector(v) ->       wrap <| v.ToArray()
        | LogicalVector(v) ->       wrap <| v.ToArray()        
        | NumericVector(v) ->       match v.GetAttribute("class") with
                                    | CharacterVector(cv) when cv.ToArray() = [| "Date" |] 
                                        -> wrap <| [| for n in v -> DateTime.FromOADate(n + RDateOffset) |]
                                    | _ -> wrap <| v.ToArray()        
        | List(v) ->                wrap <| v
        | Pairlist(pl) ->           wrap <| (pl |> Seq.map (fun sym -> sym.PrintName, sym.AsSymbol().Value))
        | Null() ->                 wrap <| null
        | Symbol(s) ->              wrap <| (s.PrintName, s.Value)
        | _ ->                      None

    let internal defaultConvertFromR (sexp: SymbolicExpression) : obj =
        let converters = mefContainer.Value.GetExports<IDefaultConvertFromR>()
        match converters |> Seq.tryPick (fun conv -> conv.Value.Convert sexp) with
        | Some res  -> res
        | None      -> match defaultConvertFromRBuiltins sexp with
                       | Some res -> res
                       | _ ->  failwithf "No default converter registered from R %s " (sexp.Type.ToString())
        

    let createDateVector (dv: seq<DateTime>) = 
        let vec = engine.CreateNumericVector [| for x in dv -> x.ToOADate() - RDateOffset |]
        vec.SetAttribute("class", engine.CreateCharacterVector [|"Date"|])
        vec

    do
        registerToR<SymbolicExpression> (fun engine v -> v)

        registerToR<string>  (fun engine v -> upcast engine.CreateCharacterVector [|v|])
        registerToR<Complex> (fun engine v -> upcast engine.CreateComplexVector [|v|])
        registerToR<int>     (fun engine v -> upcast engine.CreateIntegerVector [|v|])
        registerToR<bool>    (fun engine v -> upcast engine.CreateLogicalVector [|v|])
        registerToR<byte>    (fun engine v -> upcast engine.CreateRawVector [|v|])
        registerToR<double>  (fun engine v -> upcast engine.CreateNumericVector [|v|])
        registerToR<DateTime> (fun engine v -> upcast createDateVector [|v|])
        
        registerToR<string seq>  (fun engine v -> upcast engine.CreateCharacterVector v)
        registerToR<Complex seq> (fun engine v -> upcast engine.CreateComplexVector v)
        registerToR<int seq>     (fun engine v -> upcast engine.CreateIntegerVector v)
        registerToR<bool seq>    (fun engine v -> upcast engine.CreateLogicalVector v)
        registerToR<byte seq>    (fun engine v -> upcast engine.CreateRawVector v)
        registerToR<double seq>  (fun engine v -> upcast engine.CreateNumericVector v)
        registerToR<DateTime seq> (fun engine v -> upcast createDateVector v)

        registerToR<string[,]>  (fun engine v -> upcast engine.CreateCharacterMatrix v)
        registerToR<Complex[,]> (fun engine v -> upcast engine.CreateComplexMatrix v)
        registerToR<int[,]>     (fun engine v -> upcast engine.CreateIntegerMatrix v)
        registerToR<bool[,]>    (fun engine v -> upcast engine.CreateLogicalMatrix v)
        registerToR<byte[,]>    (fun engine v -> upcast engine.CreateRawMatrix v)
        registerToR<double[,]>  (fun engine v -> upcast engine.CreateNumericMatrix v)

    type RDotNet.REngine with
        member this.SetValue(value: obj, ?symbolName: string) : SymbolicExpression =            
            let se = convertToR this value
            if symbolName.IsSome then engine.SetSymbol(symbolName.Value, se)
            se

    let mutable symbolNum = 0
    let pid = System.Diagnostics.Process.GetCurrentProcess().Id;

    /// Get next symbol name
    let getNextSymbolName() : string =
        symbolNum <- symbolNum + 1
        sprintf "fsr_%d_%d" pid symbolNum
    
    let toR (value: obj) =
        let symbolName = getNextSymbolName()
        let se = engine.SetValue(value, symbolName)
        symbolName, se

    let makeSafeName (name: string) = name.Replace("_","__").Replace(".", "_")

    let eval (expr: string) = 
      try 
        Logging.logf "eval(%s)" expr
        let res = engine.Evaluate(expr)
        Logging.logf "eval(%s) finished" expr
        res
      with e -> 
        Logging.logf "eval(%s) failed:\r\n  %O" expr e
        //System.Diagnostics.Debugger.Break()
        System.Diagnostics.Debug.Write(e)
        System.Diagnostics.Debug.Write(expr)
        reraise()

    let evalTo   (expr: string) (symbol: string) = engine.SetSymbol(symbol, engine.Evaluate(expr))
    let exec     (expr: string) : unit = use res = engine.Evaluate(expr) in ()

open RInteropInternal

[<AutoOpen>]
module RDotNetExtensions =
    type RDotNet.SymbolicExpression with
        member this.Class : string[] = match this.GetAttribute("class") with
                                       | null -> [| |]
                                       | attrs -> attrs.AsCharacter().ToArray()
        member this.GetValue<'a>() : 'a = convertFromR<'a> this
        member this.Value = defaultConvertFromR this

module RInterop =
    let internal bindingInfo (name: string) : RValue = 
        Logging.logf "Getting bindingInfo: %s" name
        match eval("typeof(get(\"" + name + "\"))").GetValue() with
        | "closure" ->
            let argList = 
                try
                    match eval("names(formals(\"" + name + "\"))").GetValue<string[]>() with
                    | null ->  []
                    | args ->  List.ofArray args
                with 
                    | e ->     []

            let hasVarArgs = argList |> List.exists (fun p -> p = "...")
            let argList = argList |> List.filter (fun p -> p <> "...")
            RValue.Function(argList, hasVarArgs) 
        | "builtin" | "special" -> 
            // Don't know how to reflect on builtin or special args so just do as varargs
            RValue.Function([], true)
        | "double" | "character" | "list" | "logical" ->
            RValue.Value
        | something ->
            printfn "Ignoring name %s of type %s" name something
            RValue.Value      

    let internal getPackages() : string[] =
        eval(".packages(all.available=T)").GetValue()

    let internal getPackageDescription packageName: string = 
        eval("packageDescription(\"" + packageName + "\")$Description").GetValue()

    let internal getFunctionDescriptions packageName : Map<string, string> =
        exec <| sprintf """rds = readRDS(system.file("Meta", "Rd.rds", package = "%s"))""" packageName
        Map.ofArray <| Array.zip ((eval "rds$Name").GetValue()) ((eval "rds$Title").GetValue())

    let private packages = System.Collections.Generic.HashSet<string>()

    let internal loadPackage packageName : unit =
        if not(packages.Contains packageName) then
            if not(eval("require(" + packageName + ")").GetValue()) then
                failwithf "Loading package %s failed" packageName
            packages.Add packageName |> ignore

    let internal getBindings packageName : Map<string, RValue> =
        // TODO: Maybe get these from the environments?
        let names = eval(sprintf """ls("package:%s")""" packageName).GetValue()
        names
        |> Array.map (fun name -> name, bindingInfo name)
        |> Map.ofSeq

    let callFunc (packageName: string) (funcName: string) (argsByName: seq<KeyValuePair<string, obj>>) (varArgs: obj[]) : SymbolicExpression =
            // We make sure we keep a reference to any temporary symbols until after exec is called, 
            // so that the binding is kept alive in R
            // TODO: We need to figure out how to unset the symvol
            let tempSymbols = System.Collections.Generic.List<string * SymbolicExpression>()
            let passArg (arg: obj) : string = 
                match arg with
                    | :? Missing            -> failwithf "Cannot pass Missing value"
                    | :? int | :? double    -> arg.ToString()
                    //  This doesn't handle escaping so we fall through to using toR 
                    //| :? string as sval     -> "\"" + sval + "\""
                    | :? bool as bval       -> if bval then "TRUE" else "FALSE"
                    // We allow pairs to be passed, to specify parameter name
                    | _ when arg.GetType().IsConstructedGenericType && arg.GetType().GetGenericTypeDefinition() = typedefof<_*_> 
                                            -> match FSharpValue.GetTupleFields(arg) with
                                               | [| name; value |] when name.GetType() = typeof<string> ->
                                                    let name = name :?> string
                                                    tempSymbols.Add(name, engine.SetValue(value, name))
                                                    name
                                               | _ -> failwithf "Pairs must be string * value"
                    | _                     -> let sym,se = toR arg
                                               tempSymbols.Add(sym, se)
                                               sym
            
            let argList = [|
                // Pass the named arguments as name=val pairs
                for kvp in argsByName do
                    if not(kvp.Value = null || kvp.Value :? Missing) then
                        yield kvp.Key + "=" + passArg kvp.Value
                            
                // Now yield any varargs
                if varArgs <> null then
                    for argVal in varArgs -> 
                        passArg argVal
            |]

            let expr = sprintf "%s::`%s`(%s)" packageName funcName (String.Join(", ", argList))
            eval expr
        
    let call (packageName: string) (funcName: string) (namedArgs: obj[]) (varArgs: obj[]) : SymbolicExpression =
        loadPackage packageName

        match bindingInfo funcName with
        | RValue.Function(rparams, hasVarArg) ->
            let argNames = rparams
            let namedArgCount = argNames.Length
            
(*            // TODO: Pass this in so it is robust to change
            if namedArgs.Length <> namedArgCount then
                failwithf "Function %s expects %d named arguments and you supplied %d" funcName namedArgCount namedArgs.Length 
*)            
            let argsByName = seq { for n,v in Seq.zip argNames namedArgs -> KeyValuePair(n, v) }
            callFunc packageName funcName argsByName varArgs

        | RValue.Value ->
            let expr = sprintf "%s::%s" packageName funcName
            eval expr

    /// Convert a value to a value in R.
    /// Generally you shouldn't use this function - it is mainly for testing.
    let toR (value: obj) = RInteropInternal.toR value |> snd

    /// Convert a symbolic expression to some default .NET representation
    let defaultFromR (sexp: SymbolicExpression) = RInteropInternal.defaultConvertFromR sexp

[<AutoOpen>]
module RDotNetExtensions2 = 
    type RDotNet.SymbolicExpression with
        /// Call the R print function and return output as a string
        member this.Print() : string = 
            characterDevice.BeginCapture()
            RInterop.call "base" "print" [| this |] [| |] |> ignore
            characterDevice.EndCapture()
