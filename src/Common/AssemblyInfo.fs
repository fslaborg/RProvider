namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("RProvider")>]
[<assembly: AssemblyCompanyAttribute("BlueMountain Capital")>]
[<assembly: AssemblyProductAttribute("RProvider")>]
[<assembly: AssemblyDescriptionAttribute("An F# Type Provider providing strongly typed access to the R statistical package.")>]
[<assembly: AssemblyVersionAttribute("1.0.12")>]
[<assembly: AssemblyFileVersionAttribute("1.0.12")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0.12"
