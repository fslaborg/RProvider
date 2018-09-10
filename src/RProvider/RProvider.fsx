#nowarn "211"
// Standard NuGet or Paket location
#I "."
#I "lib/net40"

// Standard NuGet locations for R.NET
#I "../DynamicInterop.0.8.1/lib/netstandard1.2"
#I "../R.NET.1.7.0/lib/net40"
#I "../R.NET.FSharp.1.7.0/lib/net40"

// Standard Paket locations for R.NET
#I "../DynamicInterop/lib/netstandard1.2"
#I "../R.NET/lib/net40"
#I "../R.NET.FSharp/lib/net40"

// Try various folders that people might like
#I "bin"
#I "../bin"
#I "../../bin"
#I "lib"

// Reference RProvider and RDotNet 
#r "DynamicInterop.dll"
#r "RDotNet.dll"
#r "RDotNet.FSharp.dll"
#r "RProvider.dll"
#r "RProvider.Runtime.dll"

open RProvider
do fsi.AddPrinter(fun (synexpr:RDotNet.SymbolicExpression) -> synexpr.Print())