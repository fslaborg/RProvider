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

    member x.RInitValue =
        match initResultValue with
        | RInit.RInitError error -> Some error
        | _ -> None

    member x.GetPackages() =
        exceptionSafe <| fun () ->
            getPackages()

    member x.LoadPackage(package) =
        exceptionSafe <| fun () ->
            loadPackage package
        
    member x.GetBindings(package) =
        exceptionSafe <| fun () ->
            getBindings package
        
    member x.GetFunctionDescriptions(package:string) =
        exceptionSafe <| fun () ->
            getFunctionDescriptions package
        
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

