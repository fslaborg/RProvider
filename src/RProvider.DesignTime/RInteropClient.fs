module internal RProvider.RInteropClient

open System
open System.Collections.Generic
open System.Reflection
open System.IO
open System.Diagnostics
open System.Threading
open Microsoft.Win32
open System.IO
open RProvider.Internal
open System.Security.AccessControl
open System.Security.Principal

[<Literal>]
let server = "RProvider.Server.exe"

/// Thrown when we want to show the specified string as a friendly error message to the user
exception RInitializationError of string

/// Asynchronously waits until the specifid file is deleted using FileSystemWatcher
let waitUntilDeleted file = async {
    use watcher = 
        new FileSystemWatcher
          ( Path.GetDirectoryName(file), Path.GetFileName(file), 
            EnableRaisingEvents=true )
    let! _ = Async.AwaitEvent(watcher.Deleted)
    return () }


/// Creates a new channel name in the format: RInteropServer_<pid>_<time>_<random>
let newChannelName() = 
    let randomSalt = System.Random()
    let pid = System.Diagnostics.Process.GetCurrentProcess().Id
    let salt = randomSalt.Next()
    let tick = System.Environment.TickCount
    sprintf "RInteropServer_%d_%d_%d" pid tick salt

/// On Mac and Linux, we need to run the server using 64 bit version of mono
/// There is no standard location for this, so the user needs ~/.rprovider.conf 
let get64bitMonoExecutable() = 
    if Environment.OSVersion.Platform = PlatformID.Unix ||
       Environment.OSVersion.Platform = PlatformID.MacOSX then
        try
            let home = Environment.GetEnvironmentVariable("HOME")
            Logging.logf "get64bitMonoExecutable - Home: '%s'" home
            let config = home + "/.rprovider.conf"
            IO.File.ReadLines(config) 
            |> Seq.pick (fun line ->
                match line.Split('=') with
                | [| "MONO64"; exe |] -> Some exe
                | _ -> None )
        with e -> raise (RInitializationError("Mono 64bit executable not set (~/.rprovider.conf missing or invalid)"))
    else "mono" // On non-*nix systems, we *try* running just mono

// Global variables for remembering the current server
let mutable lastServer = None
let serverlock = obj()

let startNewServer() = 
    let channelName = newChannelName()
    let tempFile = Path.GetTempFileName()
            
    // Find the location of RProvider.Server.exe (based on non-shadow-copied path!)
    let assem = Assembly.GetExecutingAssembly()
    let assemblyLocation = assem |> RProvider.Internal.Configuration.getAssemblyLocation
    let exePath = Path.Combine(Path.GetDirectoryName(assemblyLocation), server)
    let arguments = channelName + " \"" + tempFile + "\""

    // If this is Mac or Linux, we try to run "chmod" to make the server executable
    if Environment.OSVersion.Platform = PlatformID.Unix ||
       Environment.OSVersion.Platform = PlatformID.MacOSX then
        Logging.logf "Setting execute permission on '%s'" exePath
        try System.Diagnostics.Process.Start("chmod", "+x " + exePath).WaitForExit()
        with _ -> ()

    // Log some information about the process first
    Logging.logf "Starting server '%s' with arguments '%s'" exePath arguments
    Logging.logf "Exists: %A" (File.Exists(exePath))

    // If we are running on Mono, then the safer way to start the process 
    // seems to be to use 'mono /foo/bar/RProvider.Server.exe'
    let runningOnMono = try System.Type.GetType("Mono.Runtime") <> null with e -> false 
    let startInfo = 
      if runningOnMono then
        let monoExecutable = get64bitMonoExecutable ()
        ProcessStartInfo
         ( UseShellExecute = false, CreateNoWindow = true, FileName=monoExecutable, 
           Arguments = sprintf "\"%s\" %s" exePath arguments, WindowStyle = ProcessWindowStyle.Hidden )
      else 
        ProcessStartInfo
          ( UseShellExecute = false, CreateNoWindow = true, FileName=exePath, 
            Arguments = arguments, WindowStyle = ProcessWindowStyle.Hidden )
    
    // Start the process and wait until it starts & deletes temp file
    let p = Process.Start(startInfo)
    try Async.RunSynchronously(waitUntilDeleted tempFile, 20*1000)
    with :? System.TimeoutException ->
        failwith
          ( "Failed to start the R.NET server within 20 seconds." +
            "To enable logging set RPROVIDER_LOG to an existing file name." )

    if p <> null then 
      p.EnableRaisingEvents <- true
      p.Exited.Add(fun _ -> lastServer <- None)

    Logging.logf "Attempting to connect via IPC"
    Activator.GetObject(typeof<IRInteropServer>, "ipc://" + channelName + "/RInteropServer") :?> IRInteropServer


/// Returns an instance of `RInteropServer` started via IPC
/// in a separate `RProvider.Server.exe` process (or if the server
/// is already running, returns an existing instance)
let getServer() =
  lock serverlock (fun () ->
    match lastServer with
    | Some s -> s
    | None ->
        let server = startNewServer()
        lastServer <- Some server
        Logging.logf "Got some server"
        server )

/// Returns Some("...") when there is an 'expected' kind of error that we want
/// to show in the IntelliSense in a pleasant way (R is not installed, registry
/// key is missing or .rprovider.conf is missing)
let tryGetInitializationError () =
    try getServer().InitializationErrorMessage 
    with RInitializationError err -> err

let withServer f =
    lock serverlock <| fun () ->
    let server = getServer()
    f server