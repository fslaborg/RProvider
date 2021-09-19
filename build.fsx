// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#if FAKE
#r "paket:
nuget FAKE.Core.Target
nuget FAKE.Core.ReleaseNotes
nuget FAKE.DotNet.Cli
nuget FAKE.DotNet.Fsi
nuget FAKE.DotNet.AssemblyInfoFile
nuget FAKE.Tools.Git
nuget FAKE.DotNet.Testing.XUnit2"
#load "./.fake/build.fsx/intellisense.fsx"
#else
#r "nuget: FAKE.Core.Target"
#r "nuget: FAKE.Core.ReleaseNotes"
#r "nuget: FAKE.DotNet.Cli"
#r "nuget: FAKE.DotNet.Fsi"
#r "nuget: FAKE.DotNet.AssemblyInfoFile"
#r "nuget: FAKE.Tools.Git"
#r "nuget: FAKE.DotNet.Testing.XUnit2"
let execContext = Fake.Core.Context.FakeExecutionContext.Create false "build.fsx" []
Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)
#endif

open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO.FileSystemOperators
open Fake.DotNet

// --------------------------------------------------------------------------------------
// Information about the project to be used at NuGet and in AssemblyInfo files
// --------------------------------------------------------------------------------------

let projectName = "RProvider"
let projectSummary = "An F# Type Provider providing strongly typed access to the R statistical package."
let projectDescription = """
  An F# Type Provider providing strongly typed access to the R statistical package.
  The type provider automatically discovers available R packages and makes them
  easily accessible from F#, so you can easily call powerful packages and
  visualization libraries from code running on the .NET platform."""
let authors = "BlueMountain Capital;FsLab"
let companyName = "BlueMountain Capital, FsLab"
let tags = "F# fsharp R TypeProvider visualization statistics"

let packageProjectUrl = "https://fslaborg.org/RProvider/"
let repositoryType = "git"
let repositoryUrl = "https://github.com/fslaborg/RProvider"

// --------------------------------------------------------------------------------------
// The rest of the code is standard F# build script
// --------------------------------------------------------------------------------------

// Read release notes & version info from RELEASE_NOTES.md
System.Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let binDir = __SOURCE_DIRECTORY__ @@ "bin"
let release = System.IO.File.ReadLines "RELEASE_NOTES.md" |> Fake.Core.ReleaseNotes.parse

// Generate assembly info files with the right version & up-to-date information
Target.create "AssemblyInfo" (fun _ ->
  let fileName = "src/Common/AssemblyInfo.fs"
  AssemblyInfoFile.createFSharpWithConfig fileName
      [ Fake.DotNet.AssemblyInfo.Title projectName
        Fake.DotNet.AssemblyInfo.Company companyName
        Fake.DotNet.AssemblyInfo.Product projectName
        Fake.DotNet.AssemblyInfo.Description projectSummary
        Fake.DotNet.AssemblyInfo.Version release.AssemblyVersion
        Fake.DotNet.AssemblyInfo.FileVersion release.AssemblyVersion ]
      (AssemblyInfoFileConfig(false))
)

// --------------------------------------------------------------------------------------
// Clean build results & restore NuGet packages

Target.create "Clean" (fun _ ->
    Fake.IO.Shell.cleanDirs ["bin"; "temp" ]
    Fake.IO.Shell.cleanDirs ["tests/Test.RProvider/bin"; "tests/Test.RProvider/obj" ]
)

Target.create "CleanDocs" (fun _ ->
    Fake.IO.Shell.cleanDirs [".fsdocs"]
)

// --------------------------------------------------------------------------------------
// Build library & test project

Target.create "Build" (fun _ ->
    Trace.log " --- Building the app --- "
    Fake.DotNet.DotNet.build id (projectName + ".sln")
)

Target.create "BuildTests" (fun _ ->
    Trace.log " --- Building tests --- "
    Fake.DotNet.DotNet.build id (projectName + ".Tests.sln")
)

// --------------------------------------------------------------------------------------
// Run the unit tests using test runner & kill test runner when complete

Target.create "RunTests" (fun _ ->
    let rHome = Environment.environVarOrFail "R_HOME"
    Trace.logf "R_HOME is set as %s" rHome
    let result = Fake.DotNet.DotNet.exec id "test" (projectName + ".Tests.sln")
    if result.ExitCode <> 0 then failwith "Tests failed"
)

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target.create "NuGet" (fun _ ->
    // Format the description to fit on a single line (remove \r\n and double-spaces)
    let specificVersion (name, version) = name, sprintf "[%s]" version
    let projectDescription = projectDescription.Replace("\r", "").Replace("\n", "").Replace("  ", " ")
    
    // Format the release notes
    let releaseNotes = release.Notes |> String.concat "\n"

    let properties = [
        ("Version", release.NugetVersion)
        ("Authors", authors)
        ("PackageProjectUrl", packageProjectUrl)
        ("PackageTags", tags)
        ("RepositoryType", repositoryType)
        ("RepositoryUrl", repositoryUrl)
        //("PackageLicenseExpression", license) // TODO Enable license after stop packing PipeMethodCalls
        ("PackageReleaseNotes", releaseNotes)
        ("Summary", projectSummary)
        ("PackageDescription", projectDescription)
        ("EnableSourceLink", "true")
        ("PublishRepositoryUrl", "true")
        ("EmbedUntrackedSources", "true")
        //("IncludeSymbols", "true") //TODO Enable symbols
        ("IncludeSymbols", "false")
        ("SymbolPackageFormat", "snupkg")
    ]

    DotNet.pack (fun p ->
        { p with
            Configuration = DotNet.BuildConfiguration.Release
            OutputPath = Some "bin"
            MSBuildParams = { p.MSBuildParams with Properties = properties}
        }
    ) "src/RProvider/RProvider.fsproj")

//--------------------------------------------------------------------------------------
//Generate the documentation

Target.create "GenerateDocs" (fun _ ->
   Fake.IO.Shell.cleanDir ".fsdocs"
   DotNet.exec id "fsdocs" "build --clean" |> ignore
)

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target.create "All" ignore

"Clean" ==> "AssemblyInfo" ==> "Build"
"Build" ==> "CleanDocs" ==> "GenerateDocs" ==> "All"
"Build" ==> "NuGet" ==> "All"
"Build" ==> "All"
"BuildTests" ==> "RunTests" ==> "All"

Target.runOrDefault "All"
