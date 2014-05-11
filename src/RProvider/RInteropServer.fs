namespace RProviderServer

open Microsoft.FSharp.Core.CompilerServices
open Microsoft.Win32
open RDotNet
open RProvider.RInterop
open RProvider.Internal
open System

type RInteropServer() =
    inherit MarshalByRefObject()
    
    let initResultValue = RInit.initResult.Force()

    member x.RInitValue =
        match initResultValue with
        | RInit.RInitError error -> Some error
        | _ -> None

    member x.GetPackages() =
        getPackages()

    member x.LoadPackage(package) =
        loadPackage package
        
    member x.GetBindings(package) =
        getBindings package
        
    member x.GetFunctionDescriptions(package:string) =
        getFunctionDescriptions package
        
    member x.GetPackageDescription(package) =
        getPackageDescription package
        
    member x.MakeSafeName(name) =
        makeSafeName name


