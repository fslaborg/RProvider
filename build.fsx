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
let authors = ["BlueMountain Capital"; "FsLab"]
let companyName = "BlueMountain Capital, FsLab"
let tags = "F# fsharp R TypeProvider visualization statistics"

let gitHome = "https://github.com/fslaborg"
let gitName = "RProvider"

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
    Fake.IO.Shell.cleanDirs ["docs/output"]
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
    Target.activateFinal "CloseTestRunner"
    Fake.DotNet.DotNet.test id (projectName + ".Tests.sln")
)

Target.createFinal "CloseTestRunner" (fun _ ->
    Process.killAllByName "xunit.console.clr4.exe"
)

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target.create "NuGet" (fun _ ->
    // Format the description to fit on a single line (remove \r\n and double-spaces)
    let specificVersion (name, version) = name, sprintf "[%s]" version
    let projectDescription = projectDescription.Replace("\r", "").Replace("\n", "").Replace("  ", " ")
    Fake.DotNet.NuGet.NuGet.NuGet (fun p ->
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
              [ "R.NET.Community", Fake.DotNet.NuGet.NuGet.GetPackageVersion "packages" "R.NET.Community"
                "DynamicInterop", Fake.DotNet.NuGet.NuGet.GetPackageVersion "packages" "DynamicInterop"
                "R.NET.Community.FSharp", Fake.DotNet.NuGet.NuGet.GetPackageVersion "packages" "R.NET.Community.FSharp" ]
              |> List.map specificVersion
            AccessKey = Fake.Core.Environment.environVarOrDefault "nugetkey" ""
            Publish = Fake.Core.Environment.hasEnvironVar "nugetkey" })
        "nuget/RProvider.nuspec"
)

//--------------------------------------------------------------------------------------
//Generate the documentation

Target.create "GenerateDocs" (fun _ ->
    Fsi.exec (fun p -> 
        { p with 
            TargetProfile = Fsi.Profile.NetStandard
            WorkingDirectory = "docs/tools"
            ToolPath = Fsi.FsiTool.Default
        }) "generate.fsx" ["--define:RELEASE"] |> ignore
)

// --------------------------------------------------------------------------------------
// Release Scripts

Target.create "ReleaseDocs" (fun _ ->
    Fake.Tools.Git.Repository.clone "" (gitHome + "/" + gitName + ".git") "temp/gh-pages"
    Fake.Tools.Git.Branches.checkoutBranch "temp/gh-pages" "gh-pages"
    Fake.IO.Shell.copyRecursive "docs/output" "temp/gh-pages" true |> printfn "%A"
    Fake.Tools.Git.CommandHelper.runSimpleGitCommand "temp/gh-pages" "add ." |> printfn "%s"
    let cmd = sprintf """commit -a -m "Update generated documentation for version %s""" release.NugetVersion
    Fake.Tools.Git.CommandHelper.runSimpleGitCommand "temp/gh-pages" cmd |> printfn "%s"
    Fake.Tools.Git.Branches.push "temp/gh-pages"
)

Target.create "ReleaseBinaries" (fun _ ->
    Fake.Tools.Git.Repository.clone "" (gitHome + "/" + gitName + ".git") "temp/release"
    Fake.Tools.Git.Branches.checkoutBranch "temp/release" "release"
    Fake.IO.Shell.copyRecursive "bin" "temp/release" true |> printfn "%A"
    let cmd = sprintf """commit -a -m "Update binaries for version %s""" release.NugetVersion
    Fake.Tools.Git.CommandHelper.runSimpleGitCommand "temp/release" cmd |> printfn "%s"
    Fake.Tools.Git.Branches.push "temp/release"
)

Target.create "TagRelease" (fun _ ->
    // Concatenate notes & create a tag in the local repository
    let notes = (String.concat " " release.Notes).Replace("\n", ";").Replace("\r", "")
    let tagName = "v" + release.NugetVersion
    let cmd = sprintf """tag -a %s -m "%s" """ tagName notes
    Fake.Tools.Git.CommandHelper.runSimpleGitCommand "." cmd |> printfn "%s"

    // Find the main remote (fslaborg GitHub)
    let _, remotes, _ = Fake.Tools.Git.CommandHelper.runGitCommand "." "remote -v"
    let main = remotes |> Seq.find (fun s -> s.Contains("(push)") && s.Contains("fslaborg/RProvider"))
    let remoteName = main.Split('\t').[0]
    Fake.Tools.Git.Branches.pushTag "." remoteName tagName
)

Target.create "Release" ignore

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target.create "All" ignore
Target.create "AllCore" ignore

"Clean"
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

Target.runOrDefault "All"
