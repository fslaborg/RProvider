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
        | None      -> RInitError "SOFTWARE\R-core exists but no InstallPath value was found under it."

    let locateRfromRegistry () =
        match Registry.LocalMachine.OpenSubKey @"SOFTWARE\R-core", Registry.CurrentUser.OpenSubKey @"SOFTWARE\R-core" with
        | null, null -> RInitError "Reg key Software\R-core does not exist; R is likely not installed on this computer"
        | null, x 
        | x, _ -> getRLocationFromRCoreKey x

    Logging.logf "getRLocation"
    match Environment.GetEnvironmentVariable "R_HOME" with
    | null -> locateRfromRegistry()
    | rPath -> RInitResult rPath 

/// Find the R installation using 'getRLocation' and add the directory to the
/// current environment varibale PATH (so that later loading can find 'R.dll')
let private setupPathVariable () =
    try
      Logging.logf "setupPathVariable"
      match getRLocation() with
      | RInitError error -> RInitError error
      | RInitResult location ->
          let isLinux = 
              let platform = Environment.OSVersion.Platform 
              // The guide at www.mono-project.com/FAQ:_Technical says to also check for the
              // value 128, but that is only relevant to old versions of Mono without F# support
              platform = PlatformID.MacOSX || platform = PlatformID.Unix              
          let binPath = 
              if isLinux then 
                  Path.Combine(location, "lib") 
              else
                  Path.Combine(location, "bin", if Environment.Is64BitProcess then "x64" else "i386")
          // Set the path
          if not ((Path.Combine(binPath, "libR.so") |> File.Exists) || (Path.Combine(binPath,"R.dll") |> File.Exists)) then
              RInitError (sprintf "No R engine at %s" binPath)
          else
              // Set the path
              REngine.SetEnvironmentVariables(binPath, location)
              Logging.logf "setupPathVariable completed"
              RInitResult ()
    with e ->
      Logging.logf "setupPathVariable failed: %O" e
      reraise()

/// Global interceptor that captures R console output
let internal characterDevice = new CharacterDeviceInterceptor()

/// Lazily initialized value that, when evaluated, sets the PATH variable
/// to include the R location, or fails and returns RInitError
let initResult = Lazy<_>(fun () -> setupPathVariable())

/// Lazily initialized R engine.
let internal engine = Lazy<_>(fun () ->
    try
        Logging.logf "engine: Creating and initializing instance" 
        initResult.Force() |> ignore
        let engine = REngine.GetInstance(null, true, null, characterDevice)
        System.AppDomain.CurrentDomain.DomainUnload.Add(fun _ -> engine.Dispose()) 
        Logging.logf "engine: Created & initialized instance"
        engine
    with e -> 
        Logging.logf "engine: Creating instance failed:\r\n  %O" e
        raise(Exception("Initialization of R.NET failed", e)) )
