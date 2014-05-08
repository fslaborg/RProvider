namespace RProviderServer

module Main =
    open System
    open System.Diagnostics
    open System.IO
    open System.Reflection
    open System.Runtime.Remoting
    open System.Runtime.Remoting.Channels
    open System.Threading
    open RProvider.Configuration

    [<STAThreadAttribute>]
    [<EntryPoint>]
    let main argv =
        // When RProvider is installed via NuGet, the RDotNet assembly and plugins
        // will appear typically in "../../*/lib/net40". To support this, we look at
        // RProvider.dll.config which has this pattern in custom key "ProbingLocations".
        // Here, we resolve assemblies by looking into the specified search paths.
        AppDomain.CurrentDomain.add_AssemblyResolve(fun source args ->
          resolveReferencedAssembly args.Name)

        // Create an IPC channel that exposes RInteropServer instance
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