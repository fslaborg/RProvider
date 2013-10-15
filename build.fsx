// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------

#r "tools/FAKE/tools/FakeLib.dll"
open System
open System.IO
open Fake 
open Fake.Git
open Fake.AssemblyInfoFile

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

let files includes = 
  { BaseDirectories = [__SOURCE_DIRECTORY__]
    Includes = includes
    Excludes = [] } |> Scan

// Information about the project to be used at NuGet and in AssemblyInfo files
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

// Read additional information from the release notes document
// Expected format: "0.9.0-beta - Foo bar." or just "0.9.0 - Foo bar."
// (We need to extract just the number for AssemblyInfo & all version for NuGet
let versionAsm, versionNuGet, releaseNotes = 
    let lastItem = File.ReadLines "RELEASE_NOTES.md" |> Seq.last
    let firstDash = lastItem.IndexOf(" - ")
    let notes = lastItem.Substring(firstDash + 2).Trim()
    let version = lastItem.Substring(0, firstDash).Trim([|'*'|]).Trim()
    // Get just numeric version, if it contains dash
    let versionDash = version.IndexOf('-')
    if versionDash = -1 then version, version, notes
    else version.Substring(0, versionDash), version, notes

// --------------------------------------------------------------------------------------
// Generate assembly info files with the right version & up-to-date information

Target "AssemblyInfo" (fun _ ->
  let fileName = "src/Common/AssemblyInfo.fs"
  CreateFSharpAssemblyInfo fileName
      [ Attribute.Title projectName
        Attribute.Company companyName
        Attribute.Product projectName
        Attribute.Description projectSummary
        Attribute.Version versionAsm
        Attribute.FileVersion versionAsm ] 
)

// --------------------------------------------------------------------------------------
// Clean build results & restore NuGet packages

Target "RestorePackages" (fun _ ->
    !! "./**/packages.config"
    |> Seq.iter (RestorePackage (fun p -> { p with ToolPath = "./.nuget/NuGet.exe" }))
)

Target "Clean" (fun _ ->
    CleanDirs ["build"; "gh-pages"; "release" ]
)

Target "CleanDocs" (fun _ ->
    CleanDirs ["generated"]
)

// --------------------------------------------------------------------------------------
// Build library & test project

Target "Build" (fun _ ->
    (files ["RProvider.sln"; "RProvider.Tests.sln"])
    |> MSBuildRelease "" "Rebuild"
    |> Log "AppBuild-Output: "
)

// --------------------------------------------------------------------------------------
// Run the unit tests using test runner & kill test runner when complete
//
// TODO: The tests are using xUnit, so tests are not run as part of FAKE currently :-(
//
(*

Target "RunTests" (fun _ ->
    let nunitVersion = GetPackageVersion "packages" "NUnit.Runners"
    let nunitPath = sprintf "packages/NUnit.Runners.%s/Tools" nunitVersion

    ActivateFinalTarget "CloseTestRunner"

    (files ["tests/*/bin/Release/Test.RProvider.dll"])
    |> NUnit (fun p ->
        { p with
            ToolPath = nunitPath
            DisableShadowCopy = true
            TimeOut = TimeSpan.FromMinutes 20.
            OutputFile = "TestResults.xml" })
)

FinalTarget "CloseTestRunner" (fun _ ->  
    ProcessHelper.killProcess "nunit-agent.exe"
)
*)
Target "RunTests" DoNothing

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target "NuGet" (fun _ ->
    // Format the description to fit on a single line (remove \r\n and double-spaces)
    let projectDescription = projectDescription.Replace("\r", "").Replace("\n", "").Replace("  ", " ")
    let nugetPath = ".nuget/nuget.exe"
    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = projectName
            Summary = projectSummary
            Description = projectDescription
            Version = versionNuGet
            ReleaseNotes = releaseNotes
            Tags = tags
            OutputPath = "build"
            ToolPath = nugetPath
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey" })
        "nuget/RProvider.nuspec"
)

// --------------------------------------------------------------------------------------
// Generate the documentation

Target "JustGenerateDocs" (fun _ ->
    executeFSI "tools" "build.fsx" [] |> ignore
)

Target "GenerateDocs" DoNothing
"CleanDocs" ==> "JustGenerateDocs" ==> "GenerateDocs"

// --------------------------------------------------------------------------------------
// Release Scripts

let gitHome = "https://github.com/tpetricek" // TODO: Use "BlueMountainCapital"

Target "ReleaseDocs" (fun _ ->
    Repository.clone "" (gitHome + "/FSharp.RProvider.git") "gh-pages" // TODO: Use "FSharpRProvider"
    Branches.checkoutBranch "gh-pages" "gh-pages"
    CopyRecursive "generated" "gh-pages" true |> printfn "%A"
    CommandHelper.runSimpleGitCommand "gh-pages" "add ." |> printfn "%s"
    let cmd = sprintf """commit -a -m "Update generated documentation for version %s""" versionNuGet
    CommandHelper.runSimpleGitCommand "gh-pages" cmd |> printfn "%s"
    Branches.push "gh-pages"
)

Target "ReleaseBinaries" (fun _ ->
    Repository.clone "" (gitHome + "/FSharp.RProvider.git") "release" // dtto.
    Branches.checkoutBranch "release" "release"
    CopyRecursive "build" "release" true |> printfn "%A"
    let cmd = sprintf """commit -a -m "Update binaries for version %s""" versionNuGet
    CommandHelper.runSimpleGitCommand "release" cmd |> printfn "%s"
    Branches.push "release"
)

Target "Release" DoNothing

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target "All" DoNothing

"Clean"
  ==> "RestorePackages"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "GenerateDocs"
  ==> "RunTests"
  ==> "All"

"All" 
  ==> "ReleaseDocs"
  ==> "ReleaseBinaries"
  ==> "NuGet"
  ==> "Release"

RunTargetOrDefault "All"