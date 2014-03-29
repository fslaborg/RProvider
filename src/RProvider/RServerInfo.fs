module RServerInfo

open System
open System.Collections.Generic
open System.Reflection
open System.IO
open System.Diagnostics
open System.Threading
open RDotNet
open RProviderServer
open Microsoft.Win32
open System.IO

[<Literal>]
let server = "RProviderServer.exe"

let mutable lastServer = None
let GetServer() =
    match lastServer with
    | Some s -> s
    | None ->
        let channelName =
            let randomSalt = System.Random()
            let pid = System.Diagnostics.Process.GetCurrentProcess().Id
            let tick = System.Environment.TickCount
            let salt = randomSalt.Next()
            sprintf "RProviderServer_%d_%d_%d" pid tick salt

        let createdNew = ref false
        use serverStarted = new EventWaitHandle(false, EventResetMode.ManualReset, channelName, createdNew);
        assert !createdNew

        let exePath = Path.Combine(Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location), server)
        let startInfo = ProcessStartInfo(UseShellExecute = false, CreateNoWindow = true, FileName=exePath, Arguments = channelName, WindowStyle = ProcessWindowStyle.Hidden)
        let p = Process.Start(startInfo, EnableRaisingEvents = true)

        let success = serverStarted.WaitOne()
        assert success
        p.Exited.Add(fun _ -> lastServer <- None)
        let server = Activator.GetObject(typeof<RProviderServer>, "ipc://" + channelName + "/RProviderServer") :?> RProviderServer
        lastServer <- Some server
        server

AppDomain.CurrentDomain.add_AssemblyResolve(ResolveEventHandler(fun _ args ->
    if args.Name = typeof<RProviderServer>.Assembly.FullName then
        typeof<RProviderServer>.Assembly
    else
        null))
