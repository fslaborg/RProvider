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

let execContext =
    Fake.Core.Context.FakeExecutionContext.Create false "build.fsx" []

Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)
#endif

open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.DotNet

// --------------------------------------------------------------------------------------
// Information about the project to be used at NuGet and in AssemblyInfo files
// --------------------------------------------------------------------------------------

let projectName = "RProvider"

let projectSummary =
    "An F# Type Provider providing strongly typed access to the R statistical package."

let projectDescription =
    """
  An F# Type Provider providing strongly typed access to the R statistical package.
  The type provider automatically discovers available R packages and makes them
  easily accessible from F#, so you can easily call powerful packages and
  visualization libraries from code running on the .NET platform."""

let authors = "BlueMountain Capital;FsLab"
let companyName = "BlueMountain Capital, FsLab"

let tags =
    "F# fsharp R TypeProvider visualization statistics"

let license = "BSD-2-Clause"

let iconUrl =
    "https://raw.githubusercontent.com/fslaborg/RProvider/master/docs/files/misc/logo.png"

let copyright = "(C) 2014 BlueMountain Capital"

let packageProjectUrl = "https://fslab.org/RProvider/"
let repositoryType = "git"
let repositoryUrl = "https://github.com/fslaborg/RProvider"

let repositoryContentUrl =
    "https://raw.githubusercontent.com/fslaborg/RProvider"

let serverRuntimes =
    [ "win-x64"
      "osx-x64"
      "osx-arm64"
      "linux-x64" ]

// --------------------------------------------------------------------------------------
// The rest of the code is standard F# build script
// --------------------------------------------------------------------------------------

// Read release notes & version info from RELEASE_NOTES.md
System.Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let binDir = __SOURCE_DIRECTORY__ @@ "bin"

let release =
    System.IO.File.ReadLines "RELEASE_NOTES.md"
    |> Fake.Core.ReleaseNotes.parse

// Generate assembly info files with the right version & up-to-date information
Target.create
    "AssemblyInfo"
    (fun _ ->
        let fileName = "src/Common/AssemblyInfo.fs"

        AssemblyInfoFile.createFSharpWithConfig
            fileName
            [ Fake.DotNet.AssemblyInfo.Title projectName
              Fake.DotNet.AssemblyInfo.Company companyName
              Fake.DotNet.AssemblyInfo.Product projectName
              Fake.DotNet.AssemblyInfo.Description projectSummary
              Fake.DotNet.AssemblyInfo.Version release.AssemblyVersion
              Fake.DotNet.AssemblyInfo.FileVersion release.AssemblyVersion ]
            (AssemblyInfoFileConfig(false)))

// --------------------------------------------------------------------------------------
// Check code formatting

let sourceFiles = !! "*.fs"

Target.create
    "CheckFormat"
    (fun _ ->
        let result =
            sourceFiles
            |> Seq.map (sprintf "\"%s\"")
            |> String.concat " "
            |> sprintf "%s --check"
            |> DotNet.exec id "fantomas"

        if result.ExitCode = 0 then
            Trace.log "No files need formatting"
        elif result.ExitCode = 99 then
            failwith "Some files need formatting, check output for more info"
        else
            Trace.logf "Errors while formatting: %A" result.Errors)

Target.create
    "Format"
    (fun _ ->
        let result =
            sourceFiles
            |> Seq.map (sprintf "\"%s\"")
            |> String.concat " "
            |> DotNet.exec id "fantomas"

        if not result.OK then
            printfn "Errors while formatting all files: %A" result.Messages)


// --------------------------------------------------------------------------------------
// Clean build results & restore NuGet packages

Target.create
    "Clean"
    (fun _ ->
        Fake.IO.Shell.cleanDirs [ "bin"
                                  "temp" ]

        Fake.IO.Shell.cleanDirs [ "tests/Test.RProvider/bin"
                                  "tests/Test.RProvider/obj" ])

Target.create "CleanDocs" (fun _ -> Fake.IO.Shell.cleanDirs [ ".fsdocs" ])

// --------------------------------------------------------------------------------------
// Build library & test project

Target.create
    "Build"
    (fun _ ->
        Trace.log " --- Building the app --- "

        Fake.DotNet.DotNet.build
            (fun args ->
                { args with
                      Configuration = DotNet.BuildConfiguration.Release })
            (projectName + ".sln"))

Target.create
    "MakeServerExes"
    (fun _ ->
        Trace.log " --- Publishing the RProvider.Server executables --- "

        serverRuntimes
        |> List.iter
            (fun runtime ->
                Trace.logf " --- Publishing RProvider.Server for %s --- " runtime

                DotNet.publish
                    (fun args ->
                        { args with
                              Runtime = Some runtime
                              SelfContained = Some false
                              Configuration = DotNet.BuildConfiguration.Release
                              OutputPath = Some(sprintf "src/RProvider/bin/Release/net5.0/server/%s/" runtime) })
                    "src/RProvider.Server"))

Target.create
    "BuildTests"
    (fun _ ->
        Trace.log " --- Building tests --- "

        DotNet.build
            (fun args ->
                { args with
                      Configuration = DotNet.BuildConfiguration.Release })
            "tests/Test.RProvider/Test.RProvider.fsproj")

// --------------------------------------------------------------------------------------
// Run the unit tests using test runner & kill test runner when complete

Target.create
    "RunTests"
    (fun _ ->
        let rHome = Environment.environVarOrFail "R_HOME"
        Trace.logf "R_HOME is set as %s" rHome

        let result =
            DotNet.exec
                (fun args ->
                    { args with
                          Verbosity = Some Fake.DotNet.DotNet.Verbosity.Normal
                          CustomParams = Some "-c Release" })
                "test"
                "tests/Test.RProvider/Test.RProvider.fsproj"

        if result.ExitCode <> 0 then
            failwith "Tests failed")

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target.create
    "NuGet"
    (fun _ ->
        // Format the description to fit on a single line (remove \r\n and double-spaces)
        let projectDescription =
            projectDescription
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace("  ", " ")

        // Format the release notes
        let releaseNotes = release.Notes |> String.concat "\n"

        let properties =
            [ ("Version", release.NugetVersion)
              ("Authors", authors)
              ("PackageProjectUrl", packageProjectUrl)
              ("PackageTags", tags)
              ("RepositoryType", repositoryType)
              ("RepositoryUrl", repositoryUrl)
              ("PackageLicenseExpression", license)
              ("PackageRequireLicenseAcceptance", "false")
              ("PackageReleaseNotes", releaseNotes)
              ("Summary", projectSummary)
              ("PackageDescription", projectDescription)
              ("PackageIcon", "logo.png")
              ("PackageIconUrl", iconUrl)
              ("EnableSourceLink", "true")
              ("PublishRepositoryUrl", "true")
              ("EmbedUntrackedSources", "true")
              ("IncludeSymbols", "true")
              ("IncludeSymbols", "false")
              ("SymbolPackageFormat", "snupkg")
              ("Copyright", copyright) ]

        DotNet.pack
            (fun p ->
                { p with
                      Configuration = DotNet.BuildConfiguration.Release
                      OutputPath = Some "bin"
                      MSBuildParams =
                          { p.MSBuildParams with
                                Properties = properties } })
            "src/RProvider/RProvider.fsproj")

//--------------------------------------------------------------------------------------
//Generate the documentation

Target.create
    "DocsMeta"
    (fun _ ->
        [ "<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">"
          "<PropertyGroup>"
          sprintf "<Copyright>%s</Copyright>" copyright
          sprintf "<Authors>%s</Authors>" authors
          sprintf "<PackageProjectUrl>%s</PackageProjectUrl>" packageProjectUrl
          sprintf "<RepositoryUrl>%s</RepositoryUrl>" repositoryUrl
          sprintf "<PackageLicense>%s</PackageLicense>" license
          sprintf "<PackageReleaseNotes>%s</PackageReleaseNotes>" (List.head release.Notes)
          sprintf "<PackageIconUrl>%s/master/docs/content/logo.png</PackageIconUrl>" repositoryContentUrl
          sprintf "<PackageTags>%s</PackageTags>" tags
          sprintf "<Version>%s</Version>" release.NugetVersion
          sprintf "<FsDocsLogoSource>%s/master/docs/img/logo.png</FsDocsLogoSource>" repositoryContentUrl
          sprintf "<FsDocsLicenseLink>%s/blob/master/LICENSE.md</FsDocsLicenseLink>" repositoryUrl
          sprintf "<FsDocsReleaseNotesLink>%s/blob/master/RELEASE_NOTES.md</FsDocsReleaseNotesLink>" repositoryUrl
          "<FsDocsWarnOnMissingDocs>true</FsDocsWarnOnMissingDocs>"
          "<FsDocsTheme>default</FsDocsTheme>"
          "</PropertyGroup>"
          "</Project>" ]
        |> Fake.IO.File.write false "Directory.Build.props")

Target.create
    "GenerateDocs"
    (fun _ ->
        Fake.IO.Shell.cleanDir ".fsdocs"

        DotNet.exec
            id
            "fsdocs"
            ("build --clean --properties Configuration=Release --parameters fsdocs-package-version "
             + release.NugetVersion)
        |> ignore)

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target.create "All" ignore

"Clean"
==> "AssemblyInfo"
==> "MakeServerExes"
==> "Build"

"Build"
==> "CleanDocs"
==> "DocsMeta"
==> "GenerateDocs"
==> "All"

"Build" ==> "NuGet" ==> "All"
"Build" ==> "All"
"Build" ==> "BuildTests" ==> "RunTests" ==> "All"

Target.runOrDefault "All"
