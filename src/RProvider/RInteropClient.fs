module internal RProvider.RInteropClient

open System
open System.Collections.Generic
open System.Reflection
open System.IO
open System.Diagnostics
open System.Threading
open Microsoft.Win32
open System.IO
open RProviderServer
open RProvider.Internal
open System.Security.AccessControl
open System.Security.Principal

[<Literal>]
let server = "RProvider.Server.exe"

/// true to load the server in-process, false load the server out-of-process
let localServer = false

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
    if Environment.OSVersion.Platform = PlatformID.Unix then
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
        ProcessStartInfo
         ( UseShellExecute = false, CreateNoWindow = true, FileName="mono", 
           Arguments = sprintf "\"%s\" %s" exePath arguments, WindowStyle = ProcessWindowStyle.Hidden )
      else 
        ProcessStartInfo
          ( UseShellExecute = false, CreateNoWindow = true, FileName=exePath, 
            Arguments = arguments, WindowStyle = ProcessWindowStyle.Hidden )
    
    // Start the process and wait until it starts & deletes temp file
    let p = Process.Start(startInfo)
    try Async.RunSynchronously(waitUntilDeleted tempFile, 10*1000)
    with :? System.TimeoutException ->
        failwith
          ( "Failed to start the R.NET server within 10 seconds." +
            "To enable logging set RPROVIDER_LOG to an existing file name." )

    if p <> null then 
      p.EnableRaisingEvents <- true
      p.Exited.Add(fun _ -> lastServer <- None)

    Logging.logf "Attempting to connect via IPC"
    Activator.GetObject(typeof<RInteropServer>, "ipc://" + channelName + "/RInteropServer") :?> RInteropServer


/// Returns an instance of `RInteropServer` started via IPC
/// in a separate `RProvider.Server.exe` process (or if the server
/// is already running, returns an existing instance)
let getServer() =
  lock serverlock (fun () ->
    match lastServer with
    | Some s -> s
    | None ->
        match localServer with
        | true -> new RInteropServer()
        | false ->
            let server = startNewServer()
            lastServer <- Some server
            Logging.logf "Got some server"
            server )

let withServer f =
    lock serverlock <| fun () ->
    let server = getServer()
    f server