module internal RProvider.RInteropClient

open System
open System.IO.Pipes
open System.Reflection
open System.IO
open System.Diagnostics
open System.Threading
open RProvider.Internal
open PipeMethodCalls
open RProvider.Runtime.Serialisation
open System.Runtime.InteropServices

[<Literal>]
let Server = "RProvider.Server"

/// Thrown when we want to show the specified string as a friendly error message to the user
exception RInitializationException of string

let waitUntilFileDeleted file timeout =
    let dt = DateTime.Now

    while File.Exists(file) && (DateTime.Now - dt).TotalMilliseconds < timeout do
        Thread.Sleep(10)

    not (File.Exists(file))

/// Creates a new channel name in the format: RInteropServer_<pid>_<time>_<random>
let newChannelName () =
    let randomSalt = Random()
    let pid = Process.GetCurrentProcess().Id
    let salt = randomSalt.Next()
    let tick = Environment.TickCount
    sprintf "RInteropServer_%d_%d_%d" pid tick salt

// Global variables for remembering the current server
let mutable lastServer: PipeClient<IRInteropServer> option = None
let serverLock = obj ()

let startNewServerAsync () : Async<PipeClient<IRInteropServer>> =
    Logging.logf "Starting new connection to server from client"
    let channelName = newChannelName ()
    let tempFile = Path.GetTempFileName()

    // Find the location of RProvider.Server.exe (based on non-shadow-copied path!)
    let assem = Assembly.GetExecutingAssembly()
    let assemblyLocation = assem |> Configuration.getAssemblyLocation
    let arguments = channelName + " \"" + tempFile + "\""

    // Find RProvider.Server relevant platform-specific self-contained executable
    let exePath =
        if RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then
            if RuntimeInformation.OSArchitecture = Architecture.Arm64 then
                Path.Combine(Path.GetDirectoryName(assemblyLocation), "server/osx-arm64", Server)
            else
                Path.Combine(Path.GetDirectoryName(assemblyLocation), "server/osx-x64", Server)
        else if RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then
            Path.Combine(Path.GetDirectoryName(assemblyLocation), "server/linux-x64", Server)
        else if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            Path.Combine(Path.GetDirectoryName(assemblyLocation), "server/win-x64", Server + ".exe")
        else
            failwithf "Your OS (%s) is not currently supported by RProvider." RuntimeInformation.FrameworkDescription

    // If this is Mac or Linux, we try to run "chmod" to make the server executable
    if Environment.OSVersion.Platform = PlatformID.Unix || Environment.OSVersion.Platform = PlatformID.MacOSX then
        Logging.logf "Setting execute permission on '%s'" exePath

        try
            Process.Start("chmod", "+x '" + exePath + "'").WaitForExit()
        with
        | _ -> ()

    // Log some information about the process first
    Logging.logf "Starting server '%s' with arguments '%s' (exists=%b)" exePath arguments (File.Exists(exePath))

    let startInfo =
        ProcessStartInfo(
            UseShellExecute = false,
            CreateNoWindow = true,
            FileName = exePath,
            Arguments = arguments,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = Path.GetDirectoryName(assemblyLocation)
        )

    if startInfo.EnvironmentVariables.ContainsKey("R_HOME") |> not then
        Logging.logf "R_HOME not set"

        match RProvider.Internal.RInit.rHomePath.Force() with
        | RInit.RInitResult config ->
            Logging.logf "Setting R_HOME as %s" config.RHome
            startInfo.EnvironmentVariables.Add("R_HOME", config.RHome)
        | RInit.RInitError err ->
            Logging.logf "Starting server process: Unexpected - error not reported: %s" err
            ()

    Logging.logf "R_HOME set as %O" startInfo.EnvironmentVariables.["R_HOME"]

    // Start the process and wait until it is initialized
    // (after initialization, the process deletes the temp file)
    let p = Process.Start(startInfo)

    if not (waitUntilFileDeleted tempFile (20. * 1000.)) then
        failwith (
            "Failed to start the R.NET server within 20 seconds."
            + "To enable logging set RPROVIDER_LOG to an existing file name."
        )

    if not <| isNull p then
        p.EnableRaisingEvents <- true
        p.Exited.Add(fun _ -> lastServer <- None)

    Logging.logf "Attempting to connect via inter-process communication"
    let rawPipeStream = new NamedPipeClientStream(".", channelName, PipeDirection.InOut, PipeOptions.Asynchronous)
    let pipeClient = new PipeClient<IRInteropServer>(NewtonsoftJsonPipeSerializer(), rawPipeStream)
    Logging.logf "Made pipe client with state: %A" pipeClient.State

    async {
        Logging.logf "Attempting to connect pipe client..."
        pipeClient.SetLogger(fun a -> Logging.logf "[Client Pipe log]: %O" a)
        do! pipeClient.ConnectAsync() |> Async.AwaitTask
        return pipeClient
    }

/// Returns an instance of `RInteropServer` started via IPC
/// in a separate `RProvider.Server.dll` process (or if the server
/// is already running, returns an existing instance)
let getServer () =
    Logging.logf "[Get server]"

    lock
        serverLock
        (fun () ->
            Logging.logf "[Check last server]"

            match lastServer with
            | Some s ->
                Logging.logf "[Found lastServer]"
                s
            | None ->
                Logging.logf "[Make new server]"
                // TODO Remove RunSynchronously
                let serverInstance = startNewServerAsync () |> Async.RunSynchronously
                lastServer <- Some serverInstance
                Logging.logf "Got some server"
                serverInstance)

/// Returns Some("...") when there is an 'expected' kind of error that we want
/// to show in the IntelliSense in a pleasant way (R is not installed, registry
/// key is missing or .rprovider.conf is missing)
let tryGetInitializationError () =
    try
        let server = getServer ()
        Logging.logf "Sending command: get init error message..."
        server.InvokeAsync(fun s -> s.InitializationErrorMessage()) |> Async.AwaitTask
    with
    | RInitializationException err -> async { return err }

let withServer f =
    lock serverLock
    <| fun () ->
        let serverInstance = getServer ()
        f serverInstance
