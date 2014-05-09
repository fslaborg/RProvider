#nowarn "211"
// Try including various folders where RProvider might be (version updated by FAKE)
#I "lib"
#I "bin"
#I "../bin"
#I "../../bin"
#I "packages/R.NET.1.5.5/lib/net40"
#I "../packages/R.NET.1.5.5/lib/net40"
#I "../../packages/R.NET.1.5.5/lib/net40"
#I "../../../packages/R.NET.1.5.5/lib/net40"
#I "packages/RProvider.1.0.8-alpha/lib"
#I "../packages/RProvider.1.0.8-alpha/lib"
#I "../../packages/RProvider.1.0.8-alpha/lib"
#I "../../../packages/RProvider.1.0.8-alpha/lib"
// Reference RProvider and RDotNet 
#r "RDotNet.dll"
#r "RProvider.dll"
#r "RProvider.Runtime.dll"
open RProvider

do fsi.AddPrinter(fun (synexpr:RDotNet.SymbolicExpression) -> synexpr.Print())

