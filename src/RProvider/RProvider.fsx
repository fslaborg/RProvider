#nowarn "211"
// Try including various folders where RProvider might be (version updated by FAKE)
#I "lib"
#I "bin"
#I "../bin"
#I "../../bin"
#I "packages/R.NET.Community.1.5.15/lib/net40"
#I "../packages/R.NET.Community.1.5.15/lib/net40"
#I "../../packages/R.NET.Community.1.5.15/lib/net40"
#I "../../../packages/R.NET.Community.1.5.15/lib/net40"
#I "packages/RProvider.1.0.17/lib/net40"
#I "../packages/RProvider.1.0.17/lib/net40"
#I "../../packages/RProvider.1.0.17/lib/net40"
#I "../../../packages/RProvider.1.0.17/lib/net40"
// Reference RProvider and RDotNet 
#r "RDotNet.dll"
#r "RProvider.dll"
#r "RProvider.Runtime.dll"
open RProvider

do fsi.AddPrinter(fun (synexpr:RDotNet.SymbolicExpression) -> synexpr.Print())

