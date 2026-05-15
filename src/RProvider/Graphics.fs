namespace RProvider

open RProvider.Runtime
open RProvider.Common

/// Functions for working with R graphics
module Graphics =

    /// Checks if cairo is available. R's built-in svg depends on cairo.
    let private hasCairo globEnv =
        try
            let res =
                RInterop.callFuncByName globEnv "base" "capabilities"
                    (namedParams ["what", box "cairo"]) [||]
            res.FromR<bool>() = true
        with _ -> false

    /// <summary>Capture the output of an R function that uses a graphics device into a string.</summary>
    /// <param name="width">Width of the SVG to generate</param>
    /// <param name="height">Height of the SVG to generate</param>
    /// <param name="doPlot">A function that has the side-effect of writing to an active R graphics device.</param>
    /// <returns>An SVG-formatted XML string.</returns>
    let svg (width:float) (height:float) (doPlot: unit -> Abstractions.RExpr) =
        
        let globEnv = RInterop.globalEnvironment()
        let tempFileName = System.IO.Path.GetTempFileName()
        LogFile.logf "Making SVG in temp file: %s" tempFileName

        try
            if RInterop.isPackageInstalled globEnv "svglite" then
                LogFile.logf "Using svglite::svglite"
                RInterop.callFuncByName globEnv "svglite" "svglite"
                    (namedParams [
                        "file", box tempFileName
                        "width", box width
                        "height", box height
                    ]) [||]
                |> ignore

            else if hasCairo globEnv then
                LogFile.logf "Using grDevices::svg (cairo available)"
                RInterop.callFuncByName globEnv "grDevices" "svg" (namedParams [
                    "filename", box tempFileName
                    "width", box width
                    "height", box height
                ]) [||] |> ignore

            else failwith "Not able to generate SVG. Package svglite not installed and Cairo not available."

            doPlot() |> ignore
            RInterop.callFuncByName globEnv "grDevices" "dev.off" [] [||] |> ignore
            System.IO.File.ReadAllText tempFileName
        
        finally
            if System.IO.File.Exists tempFileName
            then System.IO.File.Delete tempFileName