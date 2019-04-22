#nowarn "211"
// Standard NuGet or Paket location
#I "."
#I "lib/net40"

// Standard NuGet locations for R.NET
#I "../DynamicInterop.0.9.1/lib/netstandard2.0"
#I "../R.NET.1.8.0-alpha1/lib/netstandard2.0"
#I "../R.NET.FSharp.1.8.0-alpha/lib/netstandard2.0"

// Standard Paket locations for R.NET
#I "../DynamicInterop/lib/netstandard2.0"
#I "../R.NET/lib/netstandard2.0"
#I "../R.NET.FSharp/lib/netstandard2.0"

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