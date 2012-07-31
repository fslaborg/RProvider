namespace RProvider

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

/// Interface to use via MEF
type IConvertToR<'inType> =     
    abstract member Convert : REngine * 'inType -> SymbolicExpression

// Support conversion to an explicitly requested type.
type IConvertFromR<'outType> =     
    abstract member Convert : SymbolicExpression -> Option<'outType>

// Supporting IDefaultConvertFromR indicates that you provide a default converter
type IDefaultConvertFromR =     
    abstract member Convert : SymbolicExpression -> Option<obj>


[<AutoOpen>]
module Helpers = 
    open RDotNet.Internals

    /// Construct named params to pass to function
    let namedParams (s: seq<string*_>) = dict <| Seq.map (fun (n,v) -> n, box v) s

    let (|CharacterVector|_|) (sexp: SymbolicExpression)  = if sexp.Type = SymbolicExpressionType.CharacterVector then Some(sexp.AsCharacter()) else None
    let (|ComplexVector|_|)   (sexp: SymbolicExpression)  = if sexp.Type = SymbolicExpressionType.ComplexVector   then Some(sexp.AsComplex()) else None
    let (|IntegerVector|_|)   (sexp: SymbolicExpression)  = if sexp.Type = SymbolicExpressionType.IntegerVector   then Some(sexp.AsInteger()) else None
    let (|LogicalVector|_|)   (sexp: SymbolicExpression)  = if sexp.Type = SymbolicExpressionType.LogicalVector   then Some(sexp.AsLogical()) else None
    let (|NumericVector|_|)   (sexp: SymbolicExpression)  = if sexp.Type = SymbolicExpressionType.NumericVector   then Some(sexp.AsNumeric()) else None

    let (|Function|_|)        (sexp: SymbolicExpression)  = 
        if sexp.Type = SymbolicExpressionType.BuiltinFunction || sexp.Type = SymbolicExpressionType.Closure || sexp.Type = SymbolicExpressionType.SpecialFunction then 
            Some(sexp.AsFunction()) else None

    let (|BuiltinFunction|_|) (sexp: SymbolicExpression)  = if sexp.Type = SymbolicExpressionType.BuiltinFunction then Some(sexp.AsFunction() :?> BuiltinFunction) else None
    let (|Closure|_|)         (sexp: SymbolicExpression)  = if sexp.Type = SymbolicExpressionType.Closure then Some(sexp.AsFunction() :?> Closure) else None
    let (|SpecialFunction|_|) (sexp: SymbolicExpression)  = if sexp.Type = SymbolicExpressionType.SpecialFunction then Some(sexp.AsFunction() :?> SpecialFunction) else None

    let (|Environment|_|)   (sexp: SymbolicExpression)    = if sexp.Type = SymbolicExpressionType.Environment  then Some(sexp.AsEnvironment()) else None
    let (|Expression|_|)    (sexp: SymbolicExpression)    = if sexp.Type = SymbolicExpressionType.ExpressionVector then Some(sexp.AsExpression()) else None
    let (|Language|_|)      (sexp: SymbolicExpression)    = if sexp.Type = SymbolicExpressionType.LanguageObject then Some(sexp.AsLanguage()) else None
    let (|List|_|)          (sexp: SymbolicExpression)    = if sexp.Type = SymbolicExpressionType.List then Some(sexp.AsList()) else None     
    let (|Pairlist|_|)      (sexp: SymbolicExpression)    = if sexp.Type = SymbolicExpressionType.Pairlist then Some(sexp :?> Pairlist) else None     
    let (|Null|_|)          (sexp: SymbolicExpression)    = if sexp.Type = SymbolicExpressionType.Null then Some() else None
    let (|Symbol|_|)        (sexp: SymbolicExpression)    = if sexp.Type = SymbolicExpressionType.Symbol then Some(sexp.AsSymbol()) else None

module internal RInteropInternal =
    type RParameter = {
        Name: string
        Optional: bool
    }

    type HasVarArgs = bool

    type RValue =
        | Function of RParameter list * HasVarArgs
        | Value

    open Microsoft.Win32

    let characterDevice = new CharacterDeviceInterceptor()

    // If the registry is set up, use that for configuring the path
    let rCore = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\R-core")
    if rCore <> null then
        let is64bit = Environment.Is64BitProcess
        let subKeyName = if is64bit then "R64" else "R"
        let key = rCore.OpenSubKey(subKeyName)
        if key = null then
            failwithf "SOFTWARE\R-core exists but subkey %s does not exist" subKeyName

        let installPath = key.GetValue("InstallPath") :?> string
        let binPath = Path.Combine(installPath, "bin", if is64bit then "x64" else "i386")

        // Set the path
        Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + binPath)

    let engine = REngine.CreateInstance("RProvider")
    do engine.Initialize(null, characterDevice)

    let private mefContainer = 
        lazy
            // Look for plugins co-located with RProvider.dll
            let assem = typeof<IConvertToR<_>>.Assembly
            let catalog = new DirectoryCatalog(Path.GetDirectoryName(assem.Location),"*.Plugin.dll")
            new CompositionContainer(catalog)
                
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

        // Get a conversion function for the type.  
        // Recurses down base types until it finds a converter, or fails.
        let rec get (vt: Type) = 
            if vt = null then
                failwithf "No converter registered for type %s or any of its base types" concreteType.FullName
            
            // First we look in our dictionary of explicitly registered converters - they take precedence
            // if a plugin is also registered for the same type.  But if a plugin exists for a more specific
            // type, it will still take precedence.
            match toRConv.TryGetValue(vt) with
            | (true, conv) -> conv
            | _ -> // No converter function is registered, so ask MEF if any plugins export the conversion interface
                   let interfaceType = gt.MakeGenericType([|vt|])
                   // If there are multiple plugins registered, we arbitrarily use the "first"
                   match mefContainer.Value.GetExports(interfaceType, null, null).FirstOrDefault() with
                   | null -> get vt.BaseType
                   | conv -> let convMethod = interfaceType.GetMethod("Convert")
                             fun engine value -> convMethod.Invoke(conv.Value, [| engine; value |]) :?> SymbolicExpression
        
        get concreteType engine value
        
    let internal convertFromRBuiltins<'outType> (sexp: SymbolicExpression) : Option<'outType> = 
        let retype (x: 'b) : Option<'a> = x |> box |> unbox |> Some
        let at = typeof<'outType>
        match sexp with
        | CharacterVector(v) when at = typeof<string[]>     -> retype <| v.ToArray()
        | CharacterVector(v) when at = typeof<string>       -> retype <| v.Single()
        | ComplexVector(v) when at = typeof<Complex[]>      -> retype <| v.ToArray()
        | ComplexVector(v) when at = typeof<Complex>        -> retype <| v.Single()
        | IntegerVector(v) when at = typeof<int[]>          -> retype <| v.ToArray()
        | IntegerVector(v) when at = typeof<int>            -> retype <| v.Single()
        | LogicalVector(v) when at = typeof<bool[]>         -> retype <| v.ToArray()
        | LogicalVector(v) when at = typeof<bool>           -> retype <| v.Single()
        | NumericVector(v) when at = typeof<double[]>       -> retype <| v.ToArray()
        | NumericVector(v) when at = typeof<double>         -> retype <| v.Single()        
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
        | NumericVector(v) ->       wrap <| v.ToArray()
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
        
    do
        registerToR<SymbolicExpression> (fun engine v -> v)

        registerToR<string>  (fun engine v -> upcast engine.CreateCharacterVector [|v|])
        registerToR<Complex> (fun engine v -> upcast engine.CreateComplexVector [|v|])
        registerToR<int>     (fun engine v -> upcast engine.CreateIntegerVector [|v|])
        registerToR<bool>    (fun engine v -> upcast engine.CreateLogicalVector [|v|])
        registerToR<byte>    (fun engine v -> upcast engine.CreateRawVector [|v|])
        registerToR<double>  (fun engine v -> upcast engine.CreateNumericVector [|v|])
        
        registerToR<string[]>  (fun engine v -> upcast engine.CreateCharacterVector v)
        registerToR<Complex[]> (fun engine v -> upcast engine.CreateComplexVector v)
        registerToR<int[]>     (fun engine v -> upcast engine.CreateIntegerVector v)
        registerToR<bool[]>    (fun engine v -> upcast engine.CreateLogicalVector v)
        registerToR<byte[]>    (fun engine v -> upcast engine.CreateRawVector v)
        registerToR<double[]>  (fun engine v -> upcast engine.CreateNumericVector v)

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

    let eval (expr: string) = engine.Evaluate(expr)
    let evalTo   (expr: string) (symbol: string) = engine.SetSymbol(symbol, engine.Evaluate(expr))
    let exec     (expr: string) : unit = use res = engine.Evaluate(expr) in ()

open RInteropInternal

[<AutoOpen>]
module RDotNetExtensions = 
    type RDotNet.SymbolicExpression with
        member this.Class : string[] = match this.GetAttribute("class") with 
                                       | null -> [| |] 
                                       | attrs -> attrs.GetValue()
        member this.GetValue<'a>() : 'a = convertFromR<'a> this
        member this.Value = defaultConvertFromR this

module RInterop =
    let internal bindingInfo (name: string) : RValue = 
        match eval("typeof(get(\"" + name + "\"))").GetValue() with
        | "closure" ->
            let argList = 
                try
                    match eval("names(formals(\"" + name + "\"))").GetValue<string[]>() with
                    | null ->  []
                    | args ->  [ for arg in args -> { Name = arg; Optional = true } ]
                with 
                    | e ->     []

            let hasVarArgs = argList |> List.exists (fun p -> p.Name = "...")
            let argList = argList |> List.filter (fun p -> p.Name <> "...")
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
                    | :? string as sval     -> "\"" + sval + "\""
                    | :? bool as bval       -> if bval then "TRUE" else "FALSE"
                    | _                     -> let sym,se = toR arg
                                               tempSymbols.Add(sym, se)
                                               sym
            
            let argList = [|
                // Pass the named arguments as name=val pairs
                for kvp in argsByName do
                    if not(kvp.Value :? Missing) then
                        yield kvp.Key + "=" + passArg kvp.Value
                            
                // Now yield any varargs
                if varArgs <> null then
                    for argVal in varArgs -> 
                        passArg argVal
            |]

            loadPackage packageName

            let expr = sprintf "%s::%s(%s)" packageName funcName (String.Join(", ", argList))
            eval expr
        
    let call (packageName: string) (funcName: string) (namedArgs: obj[]) (varArgs: obj[]) : SymbolicExpression =
        match bindingInfo funcName with
        | RValue.Function(rparams, hasVarArg) ->
            let argNames = [| for arg in rparams -> arg.Name |]
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

    let sexpToString (sexp: SymbolicExpression) : string =
        characterDevice.BeginCapture()
        call "base" "print" [| sexp |] [| |] |> ignore
        characterDevice.EndCapture()
