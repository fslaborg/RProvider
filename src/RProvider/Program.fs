namespace RProvider

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
//        Debugger.Launch() |> ignore
        AppDomain.CurrentDomain.add_AssemblyResolve(fun source args ->
            let libraryName = 
              let idx = args.Name.IndexOf(',') 
              if idx > 0 then args.Name.Substring(0, idx) else args.Name

            let asm =
              getProbingLocations()
              |> Seq.tryPick (fun dir ->
                  let library = Path.Combine(dir, libraryName+".dll")
                  if File.Exists(library) then 
                    let asm = Assembly.LoadFrom(library)
                    if asm.FullName = args.Name then Some(asm) else None
                  else None)
             
            defaultArg asm null)

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