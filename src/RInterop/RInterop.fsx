#nowarn "211"
// Try including various folders where RProvider might be (version updated by FAKE)
#I "../../bin"
#I "../bin"
#I "bin"
#I "lib"
#I "packages/RInterop.1.0.5/lib"
#I "../packages/RInterop.1.0.5/lib"
#I "../../packages/RInterop.1.0.5/lib"
#I "../../../packages/RInterop.1.0.5/lib"
// Reference RInterop and RDotNet (which should be copied to the same directory)
#r "RDotNet.dll"
#r "RInterop.dll"
open RInterop

do fsi.AddPrinter(fun (synexpr:RDotNet.SymbolicExpression) -> synexpr.Print())

