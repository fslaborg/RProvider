module RProviderServer

open Microsoft.FSharp.Core.CompilerServices
open Microsoft.Win32
open RDotNet
open RInterop
open RInterop.RInteropInternal
open System

type RProviderServer() =
    inherit MarshalByRefObject()
    static do RInit.DisableStackChecking <- true
    let enginelock = "enginelock"

    let initResultValue = RInit.initResult.Force()

    member x.Engine =
        RInit.engine

    member x.RInitValue =
        match initResultValue with
        | RInit.RInitError error -> Some error
        | _ -> None

    member x.GetPackages() =
        lock enginelock (fun () -> RInterop.getPackages())

    member x.LoadPackage(package) =
        lock enginelock (fun () ->
            RInterop.loadPackage package
        )

    member x.GetBindings(package) =
        lock enginelock (fun () ->
            RInterop.getBindings package
        )

    member x.GetFunctionDescriptions(package:string) =
        lock enginelock (fun () ->
            RInterop.getFunctionDescriptions package
        )

    member x.SerializeRValue(rval) =
        RInterop.serializeRValue rval
        
    member x.GetPackageDescription(package) =
        lock enginelock (fun () ->
            RInterop.getPackageDescription package
        )

    member x.MakeSafeName(name) =
        makeSafeName name

module Main =
    open System
    open System.Diagnostics
    open System.Runtime.Remoting
    open System.Runtime.Remoting.Channels
    open System.Threading

    [<STAThreadAttribute>]
    [<EntryPoint>]
    let main argv =
        let channelName = argv.[0]
        let event = EventWaitHandle.OpenExisting(name = channelName)
        let chan = new Ipc.IpcChannel(channelName)
        ChannelServices.RegisterChannel(chan, false)
        let server = new RProviderServer()
        let objRef = RemotingServices.Marshal(server, "RProviderServer")
        RemotingConfiguration.RegisterActivatedServiceType(typeof<RProviderServer>)
        let success = event.Set()
        assert success
        let parentPid = channelName.Split('_').[1]
        let parentProcess = Process.GetProcessById(int parentPid)
        parentProcess.WaitForExit()
        0