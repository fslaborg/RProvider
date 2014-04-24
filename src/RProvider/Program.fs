namespace RProvider

module Main =
    open System
    open System.Diagnostics
    open System.Runtime.Remoting
    open System.Runtime.Remoting.Channels
    open System.Threading

    [<STAThreadAttribute>]
    [<EntryPoint>]
    let main argv =
        //Debugger.Launch() |> ignore
        let channelName = argv.[0]
        let event = EventWaitHandle.OpenExisting(name = channelName)
        let chan = new Ipc.IpcChannel(channelName)
        ChannelServices.RegisterChannel(chan, false)
        let server = new RInteropServer()
        let objRef = RemotingServices.Marshal(server, "RInteropServer")
        RemotingConfiguration.RegisterActivatedServiceType(typeof<RInteropServer>)
        let success = event.Set()
        assert success
        let parentPid = channelName.Split('_').[1]
        let parentProcess = Process.GetProcessById(int parentPid)
        parentProcess.WaitForExit()
        0