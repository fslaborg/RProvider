namespace RProvider

open Microsoft.FSharp.Reflection
open RDotNet
open RProvider.Internal
open RProvider.Internal.Logging
open RProvider.Internal.RInit
open RProvider.RInterop
open RProvider.RInteropInternal
open System.Runtime.CompilerServices
open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Reflection
open System.Threading

type public SessionConfig(hostName: string, port: int, blocking: bool, ?timeout: int) =
    member this.hostName = hostName
    member this.port = port
    member this.blocking = blocking
    member this.timeout = defaultArg timeout 2

type RemoteSymbolicExpression(getValue: RemoteSymbolicExpression -> SymbolicExpression, name) =
    member this.name = name
    
    // Retrieves the value of the handle from the remote session
    member this.GetValue () =
        getValue(this)

type LaunchResult<'T> =
    | LaunchResult of 'T
    | LaunchError of string

type RemoteConnection(connectionName) as this=
    static let getConnection (host : string option) (port : int option) (blocking : bool option) (timeout : int option) =
        let host = defaultArg host "localhost"
        let port = defaultArg port 8888
        let blocking =
            match blocking with
            | Some true -> "TRUE"
            | _ -> "FALSE"
        let timeout = defaultArg timeout 2 // seconds
        let connectionName = getNextSymbolName()
        loadPackage("svSocket")
        let conn = eval(sprintf "%s <- tryCatch( {socketConnection(host='%s', port='%d', blocking='%s', timeout='%d')}, error=function(cond) {return(NULL)}, warning=function(cond) {return(NULL)})" connectionName host port blocking timeout)
        if (eval(sprintf "is.null(%s)" connectionName).GetValue<bool>()) then
            failwith (sprintf "Failed to connect to remote R session with host %s on port %d. Are you sure that the R host application is running?" host port)
        connectionName

    static member LaunchRProfile port = 
        let rprofileFmt = sprintf """
        .First <- function() {
            if (!(require(svSocket))) {
                install.packages("svSocket", dependencies = TRUE)
                # library will raise an error if the package is still not installed
                library(svSocket)
            }
            startSocketServer(port = %d)
        }

        .Last <- function() {
            closeSocketClients(sockets = "all", serverport = %d)
            stopSocketServer(port = %d)
        }
        """
        rprofileFmt port port port

    static member GetConnection(?host, ?port, ?blocking, ?timeout) =
        new RemoteConnection(getConnection host port blocking timeout)
        
    static member GetConnection(config: SessionConfig) =
        RemoteConnection.GetConnection(host=config.hostName, port=config.port, blocking=config.blocking, timeout=config.timeout)

    static member LaunchRGui (?port: int, ?fileName: string) =
        match RInit.initResult.Value with
        | RInitError error -> LaunchError error
        | RInitResult location ->
            try
                let tempDirectory = Path.GetTempFileName()
                File.Delete(tempDirectory)
                Directory.CreateDirectory(tempDirectory) |> ignore
                let rprofilePath = Path.Combine(tempDirectory, ".Rprofile")
                let tempFS = new StreamWriter(rprofilePath)
                let port = defaultArg port 8888
                let fileName = defaultArg fileName "Rgui"
                tempFS.Write(RemoteConnection.LaunchRProfile port)
                tempFS.Flush()
                tempFS.Close()
                let startInfo = ProcessStartInfo(UseShellExecute=false, FileName=fileName, WorkingDirectory=tempDirectory)
                let p = Process.Start(startInfo, EnableRaisingEvents=false)
                p.Exited.Add(fun _ ->
                    File.Delete(rprofilePath)
                    Directory.Delete(tempDirectory)
                    )
                LaunchResult p
            with e ->
                reraise()

    new (?host, ?port, ?blocking, ?timeout) =
        new RemoteConnection(getConnection host port blocking timeout)

    member this.connectionName = connectionName
    member this.isClosed = false

    member this.makeSafeExpr (expr: string) =
        expr.Replace("\"","\\\\\"").Replace("'", "\\\\\\'").Replace("\n", "\\\\n").Replace("\r", "")
        
    member this.evalToSymbolicExpression expr =
        let expr = this.makeSafeExpr expr
        eval(sprintf """evalServer(%s, '%s')""" connectionName expr)

    member this.getHandleValue (handle: RemoteSymbolicExpression) =
        this.evalToSymbolicExpression(handle.name)

    member this.toHandle value =
        let handleName = getNextSymbolName()
        let rName, rValue = toR value
        this.assign handleName rValue

    member this.evalToHandle expr =
        let handleName = getNextSymbolName()
        let expr = this.makeSafeExpr expr
        eval(sprintf "evalServer(%s, '%s <- %s; TRUE')" connectionName handleName expr) |> ignore
        new RemoteSymbolicExpression(this.getHandleValue, handleName)

    member this.exec expr =
        let expr = this.makeSafeExpr expr
        eval(sprintf "evalServer(%s, '%s'); TRUE');" this.connectionName expr) |> ignore

    member this.assign name (value : obj) =
        let symbolName, se = toR value
        eval(sprintf "evalServer(%s, %s, %s)" this.connectionName name symbolName) |> ignore
        new RemoteSymbolicExpression(this.getHandleValue, name)
        
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
            this.assign symbolName arg |> ignore
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

    member this.getBindingsFromR =
        lazy
            let funcHandle = this.evalToHandle RInterop.getBindingsDefn
            fun packageName -> this.evalToSymbolicExpression (sprintf "%s('%s')" funcHandle.name packageName)

    member this.getBindings packageName =
        getBindings_ this.getBindingsFromR.Value packageName

    member this.serializeRValue = serializeRValue

    member this.deserializeRValue = deserializeRValue

    member this.close () =
        eval(sprintf "close(%s)" this.connectionName) |> ignore

    override this.Finalize () =
        if not this.isClosed then
            this.close()

type RemoteSession (remoteConnection : RemoteConnection) as x =
    new (?host, ?port, ?blocking, ?timeout) =
        new RemoteSession(new RemoteConnection(?host=host, ?port=port, ?blocking=blocking, ?timeout=timeout))

    new (config : SessionConfig) =
        new RemoteSession(RemoteConnection.GetConnection(config))

    member x.Connection = remoteConnection

    static member (?<-) (session : RemoteSession, name : string, value) =
        session.Connection.assign name value

    static member (?) (session : RemoteSession, name : string) =
        session.Connection.evalToHandle name

[<ExtensionAttribute>]
type RemoteSessionUtil =
    [<ExtensionAttribute>]
    static member ToLocalValue (value : obj) =
        toR value

    [<ExtensionAttribute>]
    static member ToLocalValue (value : RemoteSymbolicExpression) =
        value.GetValue()

    [<ExtensionAttribute>]
    static member ToRemoteValue (value : obj, session : RemoteSession) =
        session.Connection.toHandle value
