module RProvider.Internal.RInit

open System
open System.IO
open Microsoft.Win32
open RDotNet
open RProvider

/// When set to 'true' before the R engine is initialized, the initialization
/// will set the 'R_CStackLimit' variable to -1 (which disables stack checking
/// in the R engine and makes it possible to call R engine from multiple threads
///
/// This should not generally be done, but it is hard to avoid in a type provider
/// called by the F# compiler and engine called by user code.
///
/// When this is *not* done, the R provider will occasionally report 
/// "Error: C stack usage is too close to the limit" when it is called from 
/// another thread (not concurrently, just from another thread than previously).
///
/// For more information, see somewhat vague comment "8.1.5 Threading issues" 
/// in: www.cran.r-project.org/doc/manuals/R-exts.pdf
///
/// NOTE: For this to work correctly, the R engine must be initialized lazily
/// *after* this variable is set in the static constructor of the RProvider
let mutable DisableStackChecking = false

/// Represents R value used in initialization or information about failure
type RInitResult<'T> =
  | RInitResult of 'T
  | RInitError of string

/// Find the R installation. First check "R_HOME" environment variable, then look 
/// at the SOFTWARE\R-core\R\InstallPath value (using HKCU or, as a second try HKLM root)
let private getRLocation () =
    let getRLocationFromRCoreKey (rCore:RegistryKey) =
        let key = rCore.OpenSubKey "R"
        if key = null then RInitError "SOFTWARE\R-core exists but subkey R does not exist"
        else key.GetValue "InstallPath" |> unbox<string> |> RInitResult

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

/// Lazily initialized R engine. When 'DisableStackChecking' has been set prior
/// to the initialization (in the static constructor of RProvider), then 
/// set the 'R_CStackLimit' variable to -1.
let internal engine = Lazy<_>(fun () ->
    try
        Logging.logf "engine: Creating instance" 
        initResult.Force() |> ignore
        let engine = REngine.GetInstance()
        Logging.logf "engine: Intializing instance"
        engine.Initialize(null, characterDevice)
            
        if DisableStackChecking then
            // This needs to be called *after* the initialization of REngine
            let varAddress = engine.DangerousGetHandle("R_CStackLimit")
            System.Runtime.InteropServices.Marshal.WriteInt32(varAddress, -1)
    
        System.AppDomain.CurrentDomain.DomainUnload.Add(fun _ -> engine.Dispose()) 
        Logging.logf "engine: Created & initialized instance"
        engine
    with e -> 
        Logging.logf "engine: Creating instance failed:\r\n  %O" e
        raise(Exception("Initialization of R.NET failed", e)) )
