// include Fake lib
#r @"tools\FAKE\tools\FakeLib.dll"
open Fake 
open Fake.AssemblyInfoFile

// Assembly / NuGet package properties
let projectName = "RProvider"
let companyName = "Blue Mountain Capital"
let version = "1.0.2"
let projectSummary = "An F# Type Provider providing strongly typed access to the R statistical package."
let projectDescription = "An F# type provider for interoperating with R"
let authors = ["Blue Mountain Capital"]

// Folders
let buildDir = @".\build\"
let nugetDir = @".\nuget\"

// Restore NuGet packages
!+ "./**/packages.config"
  ++ "./packages.config"
    |> ScanImmediately
    |> Seq.iter (RestorePackage (fun p -> 
        {p with 
            ToolPath = "./.nuget/NuGet.exe"}))
// Targets

// Update assembly info
Target "UpdateAssemblyInfo" (fun _ ->
    CreateFSharpAssemblyInfo ".\AssemblyInfo.fs"
        [ Attribute.Product projectName
          Attribute.Title projectName
          Attribute.Description projectDescription
          Attribute.Company companyName
          Attribute.Version version ]
)

// Clean build directory
Target "Clean" (fun _ ->
    CleanDir buildDir
)

// Build RProvider
Target "BuildRProvider" (fun _ ->
    !! @"rprovider.fsproj"
      |> MSBuildRelease buildDir "Build"
      |> Log "AppBuild-Output: "
)

// Clean NuGet directory
Target "CleanNuGet" (fun _ ->
    CleanDir nugetDir
)

// Create NuGet package
Target "CreateNuGet" (fun _ ->
    System.IO.Directory.CreateDirectory(nugetDir @@ "tools") |> ignore
    System.IO.File.Copy(@".\init.ps1", nugetDir @@ "tools\init.ps1")

    XCopy @".\build\" (nugetDir @@ "lib")
    !+ @"nuget/lib/*.*"
      -- @"nuget/lib/RProvider*.*"
        |> ScanImmediately
        |> Seq.iter (System.IO.File.Delete)


    "RProvider.nuspec"
      |> NuGet (fun p -> 
            {p with
                Project = projectName
                Authors = authors
                Version = version
                Description = projectDescription
                Summary = projectSummary
                NoPackageAnalysis = true
                ToolPath = @".\.nuget\Nuget.exe" 
                WorkingDir = nugetDir
                OutputPath = nugetDir })
)

// Default target
Target "Default" (fun _ ->
    trace "Building R Provider"
)

// Dependencies
"UpdateAssemblyInfo"
  ==> "Clean"
  ==> "BuildRProvider"
  ==> "CleanNuGet"
  ==> "CreateNuGet"
  ==> "Default"

// start build
Run "Default"