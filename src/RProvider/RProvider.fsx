#nowarn "211"
// Try including various folders where RProvider might be (version updated by FAKE)
#I "lib"
#I "bin"
#I "../bin"
#I "../../bin"
// With version number when referenced through NuGet
#I "packages/R.NET.Community.1.5.16/lib/net40"
#I "../packages/R.NET.Community.1.5.16/lib/net40"
#I "../../packages/R.NET.Community.1.5.16/lib/net40"
#I "../../../packages/R.NET.Community.1.5.16/lib/net40"
#I "packages/R.NET.Community.FSharp.0.1.9/lib/net40"
#I "../packages/R.NET.Community.FSharp.0.1.9/lib/net40"
#I "../../packages/R.NET.Community.FSharp.0.1.9/lib/net40"
#I "../../../packages/R.NET.Community.FSharp.0.1.9/lib/net40"
#I "packages/RProvider.1.1.6/lib/net40"
#I "../packages/RProvider.1.1.6/lib/net40"
#I "../../packages/RProvider.1.1.6/lib/net40"
#I "../../../packages/RProvider.1.1.6/lib/net40"
// Without version number when referenced through Paket
#I "packages/R.NET.Community/lib/net40"
#I "../packages/R.NET.Community/net40"
#I "../../packages/R.NET.Community/lib/net40"
#I "../../../packages/R.NET.Community/lib/net40"
#I "packages/R.NET.Community.FSharp/lib/net40"
#I "../packages/R.NET.Community.FSharp/lib/net40"
#I "../../packages/R.NET.Community.FSharp/lib/net40"
#I "../../../packages/R.NET.Community.FSharp/lib/net40"
#I "packages/RProvider/lib/net40"
#I "../packages/RProvider/lib/net40"
#I "../../packages/RProvider/lib/net40"
#I "../../../packages/RProvider/lib/net40"
// Reference RProvider and RDotNet 
#r "RDotNet.dll"
#r "RDotNet.FSharp.dll"
#r "RProvider.dll"
#r "RProvider.Runtime.dll"
open RProvider

do fsi.AddPrinter(fun (synexpr:RDotNet.SymbolicExpression) -> synexpr.Print())

