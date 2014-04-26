namespace RProvider

open System
open System.Collections.Generic
open System.Reflection
open System.IO
open System.Diagnostics
open System.Threading
open Microsoft.Win32
open System.IO

module internal RInteropClient =

    [<Literal>]
    let server = "RProvider.Server.exe"

    // true to load the server in-process, false load the server out-of-process
    let localServer = false

    let mutable lastServer = None
    let serverlock = "serverlock"
    let GetServer() =
        lock serverlock (fun () ->
            match lastServer with
            | Some s -> s
            | None ->
                match localServer with
                | true -> new RInteropServer()
                | false ->
                    let channelName =
                        let randomSalt = System.Random()
                        let pid = System.Diagnostics.Process.GetCurrentProcess().Id
                        let tick = System.Environment.TickCount
                        let salt = randomSalt.Next()
                        sprintf "RInteropServer_%d_%d_%d" pid tick salt

                    let createdNew = ref false
                    use serverStarted = new EventWaitHandle(false, EventResetMode.ManualReset, channelName, createdNew);
                    assert !createdNew
                    let fsharpCoreName = System.Reflection.AssemblyName("FSharp.Core")
                    let fsharpCoreAssembly =
                        System.AppDomain.CurrentDomain.GetAssemblies()
                        |> Seq.tryFind(
                            fun a-> System.Reflection.AssemblyName.ReferenceMatchesDefinition(fsharpCoreName, a.GetName()))
                    let exePath = Path.Combine(Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location), server)
                    let arguments = channelName
                    let startInfo = ProcessStartInfo(UseShellExecute = false, CreateNoWindow = true, FileName=exePath, Arguments = arguments, WindowStyle = ProcessWindowStyle.Hidden)
                    let p = Process.Start(startInfo, EnableRaisingEvents = true)

                    let success = serverStarted.WaitOne()
                    assert success
                    p.Exited.Add(fun _ -> lastServer <- None)
                    let server = Activator.GetObject(typeof<RInteropServer>, "ipc://" + channelName + "/RInteropServer") :?> RInteropServer
                    lastServer <- Some server
                    server
                    )

    let withServer f =
        lock serverlock <| fun () ->
        let server = GetServer()
        f server

    AppDomain.CurrentDomain.add_AssemblyResolve(ResolveEventHandler(fun _ args ->
        let name = System.Reflection.AssemblyName(args.Name)
        let existingAssembly =
            System.AppDomain.CurrentDomain.GetAssemblies()
            |> Seq.tryFind(fun a -> System.Reflection.AssemblyName.ReferenceMatchesDefinition(name, a.GetName()))
        match existingAssembly with
        | Some a -> a
        | None -> null
        ))