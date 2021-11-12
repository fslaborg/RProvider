namespace RProvider

open RDotNet

/// Print functions that may be used in
/// F# interactive to 'pretty-print' R types to the
/// console window. Use in your scripts by
/// passing to `fsi.AddPrinter`.
module FSIPrinters =

    /// Print any `SymbolicExpression` using R's built-in
    /// `print` function.
    let rValue (synexpr:SymbolicExpression) = synexpr.Print()