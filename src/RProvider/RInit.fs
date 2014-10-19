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

/// Global interceptor that captures R console output
let internal characterDevice = new CharacterDeviceInterceptor()

/// Lazily initialized R engine.
let internal engine = Lazy<_>(fun () ->
    try
        Logging.logf "engine: Creating and initializing instance" 
        (* R.NET needs to initialize the engine, find the shared library and 
           set the appropriate environmental variables. This is a common failure point,
           but fixes should ideally pushed upstream to R.NET, rather than having redundant code here
        *)
        let engine = REngine.GetInstance("", true, null, characterDevice)
        System.AppDomain.CurrentDomain.DomainUnload.Add(fun _ -> engine.Dispose()) 
        Logging.logf "engine: Created & initialized instance"
        engine
    with e -> 
        Logging.logf "engine: Creating instance failed:\r\n  %O" e
        raise(Exception("Initialization of R.NET failed", e)) )
