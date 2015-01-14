namespace RProvider.Internal

/// Interface that is used for communication between the R provider server
/// (RProvider.Server.exe) which communicates with R and the client that runs
/// in the host IDE process (Visual Studio, Xamarin Studio etc.)
///
/// NOTE: In order to support standalone compilation of `RProvider.Server.exe` (which 
/// inlines the F# Core library), the interface does not expose any F# Core types.
type IRInteropServer =
  
  /// If there was an initialization error when loading R provider, this
  /// string returns the error. Otherwise, the value is `null`.
  abstract InitializationErrorMessage : string

  /// Returns an array with the names of all installed packages (e.g. "base", "graphics" etc.)
  abstract GetPackages : unit -> string[]
  /// Loads the package (using R's `require`). This should be called before `GetBindings`.
  abstract LoadPackage : string -> unit
  /// Returns an array with binding information. The first string is the name of the
  /// function. The second string is serialized `RValue` with information about the
  /// kind of the binding and function parameters (use `deserializeRValue`).
  abstract GetBindings : string -> (string * string)[]

  /// Returns an array with pairs consisting of function name and its description
  abstract GetFunctionDescriptions : string -> (string * string)[]
  /// Returns the description (documentation) for a given package
  abstract GetPackageDescription : string -> string

  /// Given an `.rdata` file, returns the names of the symbols in the file, together
  /// with an F# type that it can be converted to (this is done by getting the type
  /// of `symExpr.Value` using currently installed convertors). If the type is not 
  /// available, this returns `null`.
  abstract GetRDataSymbols : string -> (string * System.Type)[]



