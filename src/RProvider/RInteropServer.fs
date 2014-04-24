namespace RProvider

open Microsoft.FSharp.Core.CompilerServices
open Microsoft.Win32
open RDotNet
open RInterop
open RProvider.Internal
open RProvider.RInteropInternal
open System

type RInteropServer() =
    inherit MarshalByRefObject()
    
    // Set the R 'R_CStackLimit' variable to -1 when initializing the R engine
    // (the engine is initialized lazily, so the initialization always happens
    // after the static constructor is called - by doing this in the static constructor
    // we make sure that this is *not* set in the normal execution)
    static do RInit.DisableStackChecking <- true
    
    let initResultValue = RInit.initResult.Force()

    member x.RInitValue
        with get() =
            RSafe <| fun () ->
            match initResultValue with
            | RInit.RInitError error -> Some error
            | _ -> None

    member x.GetPackages() =
         RSafe <| fun () ->
            RInterop.getPackages()

    member x.LoadPackage(package) =
        RSafe <| fun () ->
            RInterop.loadPackage package
        
    member x.GetBindings(package) =
        RSafe <| fun () ->
            RInterop.getBindings package
        
    member x.GetFunctionDescriptions(package:string) =
        RSafe <| fun () ->
            RInterop.getFunctionDescriptions package
        
    member x.GetPackageDescription(package) =
        RSafe <| fun () ->
            RInterop.getPackageDescription package
        
    member x.MakeSafeName(name) =
        RSafe <| fun () ->
            makeSafeName name


