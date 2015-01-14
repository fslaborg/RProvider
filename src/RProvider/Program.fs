module RProvider.Server.Main

open System
open System.Diagnostics
open System.IO
open System.Reflection
open System.Runtime.Remoting
open System.Runtime.Remoting.Channels
open System.Threading
open RProvider.Internal
open RProvider.Internal.Configuration

/// Process.WaitForExit does not seem to be working reliably
/// on Mono, so instead we loop asynchronously until the process is gone
let rec asyncWaitForExit pid = async {
  let parentProcess = try Process.GetProcessById(pid) with _ -> null
  if parentProcess <> null then
    do! Async.Sleep(1000)
    return! asyncWaitForExit pid }

/// Start the server using the specified channel name (which
/// contains the parent PID) and delete tempFile once we're running
let startServer channelName tempFile =

  // Create an IPC channel that exposes RInteropServer instance
  let chan = new Ipc.IpcChannel(channelName)
  Logging.logf "Registering RInteropServer at channel '%s'" channelName
  ChannelServices.RegisterChannel(chan, false)
  let serviceEntry =
    new WellKnownServiceTypeEntry(typeof<RInteropServer>, "RInteropServer", WellKnownObjectMode.Singleton)
  RemotingConfiguration.RegisterWellKnownServiceType(serviceEntry)

  // Delete the temp file to signal that we're ready
  Logging.logf "Ready for connections.."
  File.Delete(tempFile)

  // Get the parent PID - when the parent stops, we Stop the event loop
  let parentPid = channelName.Split('_').[1]
  let parentProcess = Process.GetProcessById(int parentPid)
  Logging.logf "Waiting for parent process pid=%d (%A)" (int parentPid) parentProcess
  async {
    do! asyncWaitForExit (int parentPid)
    Logging.logf "Posting Stop command"
    EventLoop.queue.Add(Stop) } |> Async.Start 



[<STAThreadAttribute>]
[<EntryPoint>]
let main argv =
  try
    Logging.logf "Starting 'RProvider.Server' with arguments '%A'" argv

    // When RProvider is installed via NuGet, the RDotNet assembly and plugins
    // will appear typically in "../../*/lib/net40". To support this, we look at
    // RProvider.dll.config which has this pattern in custom key "ProbingLocations".
    // Here, we resolve assemblies by looking into the specified search paths.
    AppDomain.CurrentDomain.add_AssemblyResolve(fun source args ->
      resolveReferencedAssembly args.Name)

    // The first argument is the IPC channel to create; The second argument
    // is a temp file that we delete, once we setup the IPC channel to 
    // signal back that we are ready (in a Unix-compatible low-tech way)
    if argv.Length <> 2 then 
      failwith "Expected usage: RProvider.Server.exe <ipc channel> <temp file name>"
    if not (File.Exists(argv.[1])) then
      failwith "File passed as the second argument must exist!"

    // Expose the server object via remoting
    startServer argv.[0] argv.[1]
    Logging.logf "Server started, running event loop"

    // Run Event Loop until the parent process stops
    EventLoop.startEventLoop()
    Logging.logf "Event loop finished, shutting down"
    0
  with e -> 
    Logging.logf "RProvider.Server' failed: %A" e
    reraise()
