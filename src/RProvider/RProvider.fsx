#nowarn "211"
// Standard NuGet or Paket location
#I "."
#I "lib/net40"

// Standard NuGet locations for R.NET
#I "../DynamicInterop.0.7.4/lib/net40"
#I "../R.NET.Community.1.6.5/lib/net40"
#I "../R.NET.Community.FSharp.1.6.5/lib/net40"

// Standard Paket locations for R.NET
#I "../DynamicInterop/lib/net40"
#I "../R.NET.Community/lib/net40"
#I "../R.NET.Community.FSharp/lib/net40"

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