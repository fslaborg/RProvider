namespace RProvider

open Microsoft.FSharp.Reflection
open RDotNet
open RProvider.Internal.Logging
open RProvider.RInterop
open RProvider.RInteropInternal
open System
open System.Collections.Generic
open System.Reflection

type RemoteSymbolicExpression(getValue: RemoteSymbolicExpression -> SymbolicExpression, name) =
    member this.name = name
    
    // Retrieves the value of the handle from the remote session
    member this.GetValue () =
        getValue(this)

type RemoteSession(connectionName) as this=
    static member GetConnection(?host, ?port, ?blocking) =
        let host = defaultArg host "localhost"
        let port = defaultArg port 8888
        let blocking =
            match blocking with
            | Some true -> "TRUE"
            | _ -> "FALSE"
        let connectionName = getNextSymbolName()
        loadPackage("svSocket")
        eval(sprintf "%s <- socketConnection(host='%s', port='%d', blocking='%s')" connectionName host port blocking) |> ignore
        new RemoteSession(connectionName)

    member this.connectionName = connectionName
    member this.isClosed = false

    member this.makeSafeExpr (expr: string) =
        expr.Replace("\"","\\\\\"").Replace("'", "\\\\\\'").Replace("\n", "\\\\n").Replace("\r", "")
        
    member this.evalToSymbolicExpression expr =
        let expr = this.makeSafeExpr expr
        eval(sprintf """evalServer(%s, '%s')""" connectionName expr)

    member this.getHandleValue (handle: RemoteSymbolicExpression) =
        this.evalToSymbolicExpression(handle.name)

    member this.evalToHandle expr =
        let handleName = getNextSymbolName()
        let expr = this.makeSafeExpr expr
        eval(sprintf "evalServer(%s, '%s <- %s; TRUE')" connectionName handleName expr) |> ignore
        new RemoteSymbolicExpression(this.getHandleValue, handleName)

    member this.exec expr =
        let expr = this.makeSafeExpr expr
        eval(sprintf "evalServer(%s, 'exec(%s); TRUE');" this.connectionName expr) |> ignore

    member this.assign name value =
        let symbolName, se = toR value
        eval(sprintf "evalServer(%s, %s, %s)" this.connectionName name symbolName) |> ignore
        
    member this.getRemoteSymbol name =
        this.evalToSymbolicExpression name

    member this.resolveHandle (arg: obj) (temporaryHandles: System.Collections.Generic.List<string>) =
        match arg with
        | null -> null
        | arg when arg.GetType() = typeof<RemoteSymbolicExpression> ->
            new StringLiteral((arg :?> RemoteSymbolicExpression).name) :> obj
        | arg ->
            let symbolName = getNextSymbolName()
            temporaryHandles.Add(symbolName)
            this.assign symbolName arg
            new StringLiteral(symbolName) :> obj
    
    member this.resolveHandles (args: obj[]) (temporaryHandles: System.Collections.Generic.List<string>) =
        if args <> null then
            [| for arg in args -> this.resolveHandle arg temporaryHandles |]
        else args

    member this.clearTemporaryHandles (temporaryHandles: System.Collections.Generic.List<string>) =
        eval(sprintf "evalServer(%s, 'rm(%s); TRUE')" this.connectionName (System.String.Join(",", temporaryHandles))) |> ignore
        
    member this.call (packageName: string) (funcName: string) (serializedRVal:string) (namedArgs: obj[]) (varArgs: obj[]) : RemoteSymbolicExpression =
        let temporaryHandles = System.Collections.Generic.List<string>()
        let namedArgs = this.resolveHandles namedArgs temporaryHandles
        let varArgs = this.resolveHandles varArgs temporaryHandles
        let result = call_ this.evalToHandle packageName funcName serializedRVal namedArgs varArgs
        this.clearTemporaryHandles temporaryHandles
        result

    member this.callFunc (packageName: string) (funcName: string) (argsByName: seq<KeyValuePair<string, obj>>) (varArgs: obj[]) : RemoteSymbolicExpression =
        let temporaryHandles = System.Collections.Generic.List<string>()
        let argsByName = 
            Seq.map 
                (fun (a: KeyValuePair<string,obj>) -> new KeyValuePair<string,obj>(a.Key, (this.resolveHandle a.Value temporaryHandles))) 
                argsByName
        let result = callFunc_ this.evalToHandle packageName funcName argsByName varArgs
        this.clearTemporaryHandles temporaryHandles
        result

//        member this.bindingInfo (name: string) : RValue =
//            bindingInfo_ this.evalToSymbolicExpression name

    member val cache_getPackages = lazy(getPackages_ this.evalToSymbolicExpression)
    
    member this.getPackages ?useCache : string[] =
        let useCache = defaultArg useCache true
        match useCache with
        | true -> this.cache_getPackages.Force()
        | false -> getPackages_ this.evalToSymbolicExpression

    member this.getCached useCache (cache : Dictionary<_,_>) key lookup =
        match useCache with
        | true when cache.ContainsKey key -> cache.[key]
        | true ->
            let value = lookup()
            cache.Add(key, value)
            value
        | false -> lookup() 
    
    member this.cache_getPackageDescription = new Dictionary<string,string>()

    member this.getPackageDescription(packageName, ?useCache) : string =
        this.getCached
            (defaultArg useCache true)
            this.cache_getPackageDescription 
            packageName 
            (fun () -> getPackageDescription_ this.evalToSymbolicExpression packageName)
        
    member this.cache_getFunctionDescriptions = new Dictionary<string,Map<string,string>>()

    member this.getFunctionDescriptions(packageName, ?useCache) : Map<string, string> =
        this.getCached
            (defaultArg useCache true)
            this.cache_getFunctionDescriptions
            packageName
            (fun () -> getFunctionDescriptions_ this.exec this.evalToSymbolicExpression packageName)

    member this.packages = System.Collections.Generic.HashSet<string>()

    member this.loadPackage packageName =
        loadPackage_ this.evalToSymbolicExpression this.packages packageName

    member this.getBindings packageName =
        getBindings_ this.evalToSymbolicExpression packageName

    member this.serializeRValue = serializeRValue

    member this.deserializeRValue = deserializeRValue

    member this.close () =
        eval(sprintf "close(%s)" this.connectionName) |> ignore

    override this.Finalize () =
        if not this.isClosed then
            this.close()