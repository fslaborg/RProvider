module internal RProvider.Logging

open System
open System.IO
open System.Diagnostics
open Microsoft.FSharp.Reflection

/// Change this constant to enable logging
let [<Literal>] loggingEnabled = false

/// Log file (by default "C:\Users\<user>\AppData\Roaming\RLogs\log.txt")
let private logFile = 
  try
    let appd = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
    if not (Directory.Exists(appd + "\\RLogs")) then Directory.CreateDirectory(appd + "\\RLogs") |> ignore
    appd + "\\RLogs\\log.txt"
  with _ -> (* Silently ignoring logging errors *) null

/// Append string to a log file
let private writeString str =
  try
    // This serializes all writes to the log file (from multiple processes)
    use fs = new FileStream(logFile, FileMode.OpenOrCreate, Security.AccessControl.FileSystemRights.AppendData, FileShare.Write, 4096, FileOptions.None)
    use writer = new StreamWriter(fs)
    writer.AutoFlush <- true
      
    let pid = Process.GetCurrentProcess().Id
    let tid = System.Threading.Thread.CurrentThread.ManagedThreadId
    let apid = System.AppDomain.CurrentDomain.Id
    writer.WriteLine(sprintf "[%s] [Pid:%d, Tid:%d, Apid:%d] %s" (System.DateTime.Now.ToString("G")) pid tid apid str)
  with _ -> (*silently ignoring logging errors*) () 

/// Log formatted string to a log file
let logf fmt = 
    let f = if loggingEnabled then writeString else ignore
    Printf.kprintf f fmt

/// Run the specified function and log potential expceptions, as well
/// as the output written to console (unless characterDevice.IsCapturing)
let logWithOutput (characterDevice:CharacterDeviceInterceptor) f =
  if loggingEnabled then 
    try 
      // If the device is capturing stuff for someone else, then
      // we do not want to touch it (it is a minor limitation only)
      let capturing = characterDevice.IsCapturing
      try
        if not capturing then characterDevice.BeginCapture()
        f()
      finally
        let out = 
          if not capturing then characterDevice.EndCapture()
          else "(could not be captured)"
        logf "Output: %s" out
    with e -> 
      logf "Operation failed:\r\n  %O" e
      reraise()
  else f ()  