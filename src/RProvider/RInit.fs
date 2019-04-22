/// [omit]
module RProvider.Internal.RInit

open System
open System.IO
open Microsoft.Win32
open RDotNet
open RProvider

/// Represents R value used in initialization or information about failure
type RInitResult<'T> =
  | RInitResult of 'T
  | RInitError of string

/// Find the R installation. First check "R_HOME" environment variable, then look 
/// at the SOFTWARE\R-core\R\InstallPath value (using HKCU or, as a second try HKLM root)
let private getRLocation () =
    let rec getRLocationFromRCoreKey (rCore:RegistryKey) =
        /// Iterates over all subkeys in "SOFTWARE\R-core". If a key with name "R"
        /// exists, then it is returned first (because that's the one where the 
        /// InstallPath should be according to the documentation).
        let keys (rCore:RegistryKey) =
            let rec loop (root:RegistryKey) = seq { 
                yield root
                for subKeyName in root.GetSubKeyNames() do
                    yield! loop <| root.OpenSubKey(subKeyName) }
            seq { let key = rCore.OpenSubKey "R" 
                  if key <> null then yield key
                  yield! loop rCore }

        let hasInstallPath (key:RegistryKey) =
            key.GetValueNames()
            |> Array.exists (fun valueName -> valueName = "InstallPath")

        match rCore |> keys |> Seq.tryFind (fun key -> key |> hasInstallPath) with
        | Some(key) -> key.GetValue("InstallPath") |> unbox<string> |> RInitResult
        | None      -> RInitError "R was not found (SOFTWARE\R-core exists but subkey R does not)"

    let locateRfromRegistry () =
        Logging.logf "Scanning the registry"
        match Registry.LocalMachine.OpenSubKey @"SOFTWARE\R-core", Registry.CurrentUser.OpenSubKey @"SOFTWARE\R-core" with
        | null, null -> RInitError "R is not installed (Software\R-core does not exist)"
        | null, x 
        | x, _ -> getRLocationFromRCoreKey x

    let locateRfromConfFile () = 
        let res = Configuration.getRProviderConfValue "R_HOME"
        Logging.logf "Checking '~/.rprovider.conf' for R_HOME value: %A" res
        res

    let locateRfromShellR () = 
        Logging.logf "Calling 'R --print-home'"
        try
            // Run the process & read standard output
            let ps = System.Diagnostics.ProcessStartInfo
                      ( FileName = "R", Arguments = "--print-home",
                        RedirectStandardOutput = true, UseShellExecute = false)
            let p = System.Diagnostics.Process.Start(ps)
            p.WaitForExit()
            let path = p.StandardOutput.ReadToEnd()
            Logging.logf "R --print-home returned: %s" path
            RInitResult(path.Trim())
        with e -> 
            Logging.logf "Calling 'R --print-home' failed with: %A" e
            RInitError("R is not installed (R_HOME not set in ~/.rprovider.conf and 'R --print-home' failed")

    // First, check R_HOME. If that's not set, then on Mac or Unix, we use 
    // `R --print-home` and on Windows, we look at "SOFTWARE\R-core" in registry
    Logging.logf "getRLocation"

    // On Mac and Unix we first check `.rprovider.conf` in PATH for 
    // R_HOME variable; then try to run "R --print-home" hoping that R is in PATH
    match Environment.GetEnvironmentVariable "R_HOME" with
    | null -> 
        if Configuration.isUnixOrMac() then 
            match locateRfromConfFile() with
            | Some rPath -> RInitResult rPath
            | _ -> locateRfromShellR()
        else locateRfromRegistry()
    | rPath -> RInitResult rPath 

/// Find the R installation using 'getRLocation' and add the directory to the
/// current environment varibale PATH (so that later loading can find 'R.dll')
let private findRHomePath () =
    try
      Logging.logf "findRHomePath"
      match getRLocation() with
      | RInitError error -> RInitError error
      | RInitResult location ->
          let binPath = 
              if Configuration.isUnixOrMac() then 
                  Path.Combine(location, "lib") 
              else
                  Path.Combine(location, "bin", if Environment.Is64BitProcess then "x64" else "i386")

          // Set the path
          let optRLibrary = 
              [ Path.Combine(binPath, "libR.dylib")
                Path.Combine(binPath, "libR.so")
                Path.Combine(binPath, "R.dll") ]
              |> Seq.tryFind File.Exists
          match optRLibrary with
          | None ->
              RInitError (sprintf "No R engine at %s" binPath)
          | Some libraryFile ->
              Logging.logf "findRHomePath: file='%s'" libraryFile
              RInitResult(libraryFile)
    with e ->
      Logging.logf "findRHomePath failed: %O" e
      reraise()

/// Global interceptor that captures R console output
let internal characterDevice = new CharacterDeviceInterceptor()

/// Lazily initialized value that, find the R location or fails and returns RInitError
let rHomePath = Lazy<_>(fun () -> findRHomePath())

/// Lazily initialized R engine.
let internal engine = Lazy<_>(fun () ->
    try
        Logging.logf "engine: Creating and initializing instance (sizeof<IntPtr>=%d)" IntPtr.Size 
        let lib = 
            // If the value was `RInitError`, the error has already been reported by
            // `RInteropServer.InitializationErrorMessage` and so we never even get here
            match rHomePath.Force() with
            | RInitResult res -> res
            | RInitError err -> 
                Logging.logf "engine: Unexpected - error not reported: %s" err
                null

        let engine = REngine.GetInstance(lib, true, null, characterDevice, AutoPrint=false)
        System.AppDomain.CurrentDomain.DomainUnload.Add(fun _ -> engine.Dispose()) 
        Logging.logf "engine: Created & initialized instance"
        engine
    with e -> 
        Logging.logf "engine: Creating instance failed:\r\n  %O" e
        raise(Exception("Initialization of R.NET failed: " + e.Message, e)) )
