// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#I "packages/FAKE/tools"
#r "packages/FAKE/tools/FakeLib.dll"
open System
open Fake
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open Fake.Testing

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
let authors = ["BlueMountain Capital"]
let companyName = "BlueMountain Capital"
let tags = "F# fsharp R TypeProvider visualization statistics"

let gitHome = "https://github.com/BlueMountainCapital"
let gitName = "FSharpRProvider"

// --------------------------------------------------------------------------------------
// The rest of the code is standard F# build script
// --------------------------------------------------------------------------------------

// Read release notes & version info from RELEASE_NOTES.md
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let binDir = __SOURCE_DIRECTORY__ @@ "bin"
let release = IO.File.ReadLines "RELEASE_NOTES.md" |> parseReleaseNotes

// Generate assembly info files with the right version & up-to-date information
Target "AssemblyInfo" (fun _ ->
  let fileName = "src/Common/AssemblyInfo.fs"
  CreateFSharpAssemblyInfoWithConfig fileName
      [ Attribute.Title projectName
        Attribute.Company companyName
        Attribute.Product projectName
        Attribute.Description projectSummary
        Attribute.Version release.AssemblyVersion
        Attribute.FileVersion release.AssemblyVersion ]
      (AssemblyInfoFileConfig(false))
)

// --------------------------------------------------------------------------------------
// Update the assembly version numbers in the script file.

open System.IO

Target "UpdateFsxVersions" (fun _ ->
    let path = "./src/RProvider/RProvider.fsx"
    let mutable text = File.ReadAllText(path)
    for package in [ "DynamicInterop"; "R.NET.Community"; "R.NET.Community.FSharp" ] do
      let version = GetPackageVersion "packages" package
      let pattern = "\\.\\./" + package + ".([0-9\\.]*)/lib"
      let replacement = sprintf "../%s.%s/lib" package version
      text <- Text.RegularExpressions.Regex.Replace(text, pattern, replacement)
    File.WriteAllText(path, text)
)

// --------------------------------------------------------------------------------------
// Clean build results & restore NuGet packages

Target "Clean" (fun _ ->
    CleanDirs ["bin"; "temp" ]
    CleanDirs ["tests/Test.RProvider/bin"; "tests/Test.RProvider/obj" ]
)

Target "CleanDocs" (fun _ ->
    CleanDirs ["docs/output"]
)

// --------------------------------------------------------------------------------------
// Build library & test project

Target "Build" (fun _ ->
    !! (projectName + ".sln")
    |> MSBuildRelease "" "Rebuild"
    |> Log "AppBuild-Output: "
)

Target "BuildTests" (fun _ ->
    !! (projectName + ".Tests.sln")
    |> MSBuildRelease "" "Rebuild"
    |> Log "AppBuild-Output: "
)

// --------------------------------------------------------------------------------------
// Run the unit tests using test runner & kill test runner when complete


Target "RunTests" (fun _ ->
    let xunitPath = "packages/xunit.runners/tools/xunit.console.clr4.exe"

    ActivateFinalTarget "CloseTestRunner"

    !! "tests/Test.RProvider/bin/**/Test*.dll"
    |> xUnit (fun p ->
            {p with
                ToolPath = xunitPath
                ShadowCopy = false
                HtmlOutputPath = Some "."
                XmlOutputPath = Some "." })
)

FinalTarget "CloseTestRunner" (fun _ ->
    ProcessHelper.killProcess "xunit.console.clr4.exe"
)

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target "NuGet" (fun _ ->
    // Format the description to fit on a single line (remove \r\n and double-spaces)
    let specificVersion (name, version) = name, sprintf "[%s]" version
    let projectDescription = projectDescription.Replace("\r", "").Replace("\n", "").Replace("  ", " ")
    NuGet (fun p ->
        { p with
            Authors = authors
            Project = projectName
            Summary = projectSummary
            Description = projectDescription
            Version = release.NugetVersion
            ReleaseNotes = String.concat " " release.Notes
            Tags = tags
            OutputPath = "bin"
            Dependencies =
              [ "R.NET.Community", GetPackageVersion "packages" "R.NET.Community"
                "DynamicInterop", GetPackageVersion "packages" "DynamicInterop"
                "R.NET.Community.FSharp", GetPackageVersion "packages" "R.NET.Community.FSharp" ]
              |> List.map specificVersion
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey" })
        "nuget/RProvider.nuspec"
)

// --------------------------------------------------------------------------------------
// Generate the documentation

Target "GenerateDocs" (fun _ ->
    executeFSIWithArgs "docs/tools" "generate.fsx" ["--define:RELEASE"] [] |> ignore
)

// --------------------------------------------------------------------------------------
// Release Scripts

Target "ReleaseDocs" (fun _ ->
    Repository.clone "" (gitHome + "/" + gitName + ".git") "temp/gh-pages"
    Branches.checkoutBranch "temp/gh-pages" "gh-pages"
    CopyRecursive "docs/output" "temp/gh-pages" true |> printfn "%A"
    CommandHelper.runSimpleGitCommand "temp/gh-pages" "add ." |> printfn "%s"
    let cmd = sprintf """commit -a -m "Update generated documentation for version %s""" release.NugetVersion
    CommandHelper.runSimpleGitCommand "temp/gh-pages" cmd |> printfn "%s"
    Branches.push "temp/gh-pages"
)

Target "ReleaseBinaries" (fun _ ->
    Repository.clone "" (gitHome + "/" + gitName + ".git") "temp/release"
    Branches.checkoutBranch "temp/release" "release"
    CopyRecursive "bin" "temp/release" true |> printfn "%A"
    let cmd = sprintf """commit -a -m "Update binaries for version %s""" release.NugetVersion
    CommandHelper.runSimpleGitCommand "temp/release" cmd |> printfn "%s"
    Branches.push "temp/release"
)

Target "TagRelease" (fun _ ->
    // Concatenate notes & create a tag in the local repository
    let notes = (String.concat " " release.Notes).Replace("\n", ";").Replace("\r", "")
    let tagName = "v" + release.NugetVersion
    let cmd = sprintf """tag -a %s -m "%s" """ tagName notes
    CommandHelper.runSimpleGitCommand "." cmd |> printfn "%s"

    // Find the main remote (BlueMountain GitHub)
    let _, remotes, _ = CommandHelper.runGitCommand "." "remote -v"
    let main = remotes |> Seq.find (fun s -> s.Contains("(push)") && s.Contains("BlueMountainCapital/FSharpRProvider"))
    let remoteName = main.Split('\t').[0]
    Fake.Git.Branches.pushTag "." remoteName tagName
)

Target "Release" DoNothing

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target "All" DoNothing
Target "AllCore" DoNothing

"Clean"
  ==> "UpdateFsxVersions"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "BuildTests"
  ==> "RunTests"
  ==> "All"

"All"
  ==> "CleanDocs"
  ==> "GenerateDocs"
  ==> "ReleaseDocs"
  ==> "ReleaseBinaries"
  ==> "Release"

"All" ==> "NuGet" ==> "Release"
"All" ==> "TagRelease" ==> "Release"

RunTargetOrDefault "All"
