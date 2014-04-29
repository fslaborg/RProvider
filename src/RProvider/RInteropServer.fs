namespace RProviderServer

open Microsoft.FSharp.Core.CompilerServices
open Microsoft.Win32
open RDotNet
open RProvider
open RProvider.RInterop
open RProvider.Internal
open System

type RInteropServer() =
    inherit MarshalByRefObject()
    
    let initResultValue = RInit.initResult.Force()

    let exceptionSafe f =
        try
            f()
        with
        | ex when ex.GetType().IsSerializable -> raise ex
        | ex ->
            failwith ex.Message

    let mutable remoteSessions = Map.empty

    member x.RInitValue =
        match initResultValue with
        | RInit.RInitError error -> Some error
        | _ -> None

    member private x.GetRemoteSession(config:SessionConfig) =
        let sessionKey = (config.hostName, config.port, config.blocking)
        if not (remoteSessions.ContainsKey sessionKey) then 
            remoteSessions <- remoteSessions.Add(sessionKey, RemoteSession.GetConnection(config))
        remoteSessions.[sessionKey]

    member x.GetPackages() =
        exceptionSafe <| fun () ->
            getPackages()

    member x.GetPackages(remoteSession) =
        withLock <| fun () ->
            x.GetRemoteSession(remoteSession).getPackages()
         
    member x.LoadPackage(package) =
        exceptionSafe <| fun () ->
            loadPackage package

    member x.LoadPackage(package, remoteSession) =
        withLock <| fun () ->
            x.GetRemoteSession(remoteSession).loadPackage package
        
    member x.GetBindings(package, remoteSession) =
        withLock <| fun () ->
            x.GetRemoteSession(remoteSession).getBindings package

    member x.GetBindings(package) =
        exceptionSafe <| fun () ->
            getBindings package
        
    member x.GetFunctionDescriptions(package:string, remoteSession) =
        withLock <| fun () ->
            x.GetRemoteSession(remoteSession).getFunctionDescriptions package
    
    member x.GetFunctionDescriptions(package:string) =
        exceptionSafe <| fun () ->
            getFunctionDescriptions package
        
    member x.GetPackageDescription(package, remoteSession) =
        withLock <| fun () ->
            x.GetRemoteSession(remoteSession).getPackageDescription package
    
    member x.GetPackageDescription(package) =
        exceptionSafe <| fun () ->
            getPackageDescription package
        
    member x.MakeSafeName(name) =
        exceptionSafe <| fun () ->
            makeSafeName name

    member x.GetRDataSymbols(file) =
        exceptionSafe <| fun () ->
            let env = REnv(file) 
            [| for k in env.Keys ->
                  let v = env.Get(k)
                  let typ = try Some(v.Value.GetType()) with _ -> None
                  k, typ |]

