module internal RProvider.RInteropClient

open System
open System.Reflection
open System.IO
open System.Diagnostics
open System.Threading
open RProvider.Internal
open PipeMethodCalls
open PipeMethodCalls.NetJson

[<Literal>]
let Server = "RProvider.Server.exe"

/// Thrown when we want to show the specified string as a friendly error message to the user
exception RInitializationException of string

let waitUntilFileDeleted file timeout = 
    let dt = DateTime.Now 
    while File.Exists(file) && (DateTime.Now - dt).TotalMilliseconds < timeout do
      Thread.Sleep(10)
    not (File.Exists(file))

/// Creates a new channel name in the format: RInteropServer_<pid>_<time>_<random>
let newChannelName() = 
    let randomSalt = Random()
    let pid = Process.GetCurrentProcess().Id
    let salt = randomSalt.Next()
    let tick = Environment.TickCount
    $"RInteropServer_%d{pid}_%d{tick}_%d{salt}"

/// On Mac and Linux, we need to run the server using 64 bit version of mono
/// There is no standard location for this, so the user needs ~/.rprovider.conf
/// TODO Run on dotnet runtime rather than mono
let get64bitMonoExecutable() = 
    if Configuration.isUnixOrMac() then
        match Configuration.getRProviderConfValue "MONO64" with
        | Some exe -> exe
        | None -> raise (RInitializationException("Mono 64bit executable not set (~/.rprovider.conf missing or invalid)"))
    else "mono" // On non-*nix systems, we *try* running just mono

// Global variables for remembering the current server
let mutable lastServer : IRInteropServer option = None
let serverLock = obj()

let startNewServerAsync() : Async<IRInteropServer> = 
    let channelName = newChannelName()
    let tempFile = Path.GetTempFileName()

    // Find the location of RProvider.Server.exe (based on non-shadow-copied path!)
    let assem = Assembly.GetExecutingAssembly()
    let assemblyLocation = assem |> Configuration.getAssemblyLocation
    let exePath = Path.Combine(Path.GetDirectoryName(assemblyLocation), Server)
    let arguments = channelName + " \"" + tempFile + "\""

    // If this is Mac or Linux, we try to run "chmod" to make the server executable
    if Environment.OSVersion.Platform = PlatformID.Unix ||
       Environment.OSVersion.Platform = PlatformID.MacOSX then
        Logging.logf $"Setting execute permission on '%s{exePath}'"
        try Process.Start("chmod", "+x " + exePath).WaitForExit()
        with _ -> ()

    // Log some information about the process first
    Logging.logf 
        "Starting server '%s' with arguments '%s' (exists=%b)" 
        exePath arguments (File.Exists(exePath))

    // If we are running on Mono, then the safer way to start the process 
    // seems to be to use 'mono /foo/bar/RProvider.Server.exe'
    let runningOnMono = try isNull (Type.GetType("Mono.Runtime")) with e -> false 
    let startInfo = 
      if runningOnMono then
        let monoExecutable = get64bitMonoExecutable ()
        ProcessStartInfo
         ( UseShellExecute = false, CreateNoWindow = true, FileName=monoExecutable, 
           Arguments = $"\"%s{exePath}\" %s{arguments}", WindowStyle = ProcessWindowStyle.Hidden )
      else 
        ProcessStartInfo
          ( UseShellExecute = false, CreateNoWindow = true, FileName=exePath, 
            Arguments = arguments, WindowStyle = ProcessWindowStyle.Hidden )
    
    // Start the process and wait until it is initialized
    // (after initialization, the process deletes the temp file)
    let p = Process.Start(startInfo)
    if not(waitUntilFileDeleted tempFile (20.*1000.)) then
        failwith
          ( "Failed to start the R.NET server within 20 seconds." +
            "To enable logging set RPROVIDER_LOG to an existing file name." )

    if isNull p then 
      p.EnableRaisingEvents <- true
      p.Exited.Add(fun _ -> lastServer <- None)

    Logging.logf "Attempting to connect via IPC"
    let pipeClient = new PipeClient<IRInteropServer>(NetJsonPipeSerializer(), channelName)
    async {
      do! pipeClient.ConnectAsync() |> Async.AwaitTask
      return! pipeClient.InvokeAsync(id) |> Async.AwaitTask
    }

/// Returns an instance of `RInteropServer` started via IPC
/// in a separate `RProvider.Server.exe` process (or if the server
/// is already running, returns an existing instance)
let getServer() =
  lock serverLock (fun () ->
    match lastServer with
    | Some s -> s
    | None ->
        // TODO Remove RunSynchronously
        let serverInstance = startNewServerAsync() |> Async.RunSynchronously
        lastServer <- Some serverInstance
        Logging.logf "Got some server"
        serverInstance)

/// Returns Some("...") when there is an 'expected' kind of error that we want
/// to show in the IntelliSense in a pleasant way (R is not installed, registry
/// key is missing or .rprovider.conf is missing)
let tryGetInitializationError () =
    try getServer().InitializationErrorMessage 
    with RInitializationException err -> err

let withServer f =
    lock serverLock <| fun () ->
    let serverInstance = getServer()
    f serverInstance