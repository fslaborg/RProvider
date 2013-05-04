// include Fake lib
#r @"tools\FAKE\tools\FakeLib.dll"
open Fake 

// Properties
let buildDir = @".\build\"

// Targets
Target "Clean" (fun _ ->
    CleanDir buildDir
)

Target "BuildApp" (fun _ ->
    !! @"rprovider.fsproj"
      |> MSBuildRelease buildDir "Build"
      |> Log "AppBuild-Output: "
)
// Default target
Target "Default" (fun _ ->
    trace "Building R Provider"
)

// Dependencies
"Clean"
  ==> "BuildApp"
  ==> "Default"

// start build
Run "Default"