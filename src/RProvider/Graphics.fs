namespace RProvider

open RProvider.Runtime
open RProvider.Common

/// Functions for working with R graphics
module Graphics =

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
            RInterop.callFuncByName globEnv "grDevices" "svg" (namedParams [
                "filename", box tempFileName
                "width", box width
                "height", box height
            ]) [||] |> ignore
            doPlot() |> ignore
            RInterop.callFuncByName globEnv "grDevices" "dev.off" [] [||] |> ignore
            System.IO.File.ReadAllText tempFileName
        
        finally
            if System.IO.File.Exists tempFileName
            then System.IO.File.Delete tempFileName