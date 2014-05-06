#r @"..\..\bin\RDotNet.dll"
#r @"..\..\bin\RProvider.dll"
#r @"..\..\bin\RProvider.Runtime.dll"
#r @"..\..\bin\RDataProvider.dll"

open RProvider
open RProvider.``base``

type Foo = RData<"C:\\Temp\\Test.rdata">
let f = new Foo()
f.cars
f.bar



// Define your library scripting code here

System.Environment.GetEnvironmentVariables()

