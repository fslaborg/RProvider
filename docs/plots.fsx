(**
---
title: Plots and graphics
category: Guides
categoryindex: 4
index: 2
---
*)

(*** condition: prepare ***)
#nowarn "211"
#r "nuget: RProvider, 0.0.1-local"
(*** condition: fsx ***)
#if FSX
#r "nuget: RProvider,{{package-version}}"
#endif // FSX
(*** condition: ipynb ***)
#if IPYNB
#r "nuget: RProvider,{{package-version}}"
#endif // IPYNB

(** 
# Creating Plots and Graphics

One of the compelling features of R is its ability to create beautiful plots.
With the R Type Provider, you can use all of R capabilities from F#, 
and create simple plots quickly to explore and visualize your data on-the-fly, 
as well as generate publication quality graphics that can be exported to virtually any format.

## Basic R plots

Basic plots can be found in the graphics package.
Assuming you are using an F# script, 
you can reference the required libraries and packages this way:

    [lang=fsharp]
    #r "nuget: RProvider,2.0.2"
*)

open RProvider
open RProvider.graphics

(**
RProvider includes a `Graphics` module for capturing R plots.
You may also use many (but not all) R graphics devices directly.
See [graphics](graphics.html) for more details.

The primary helper function is for outputting vector-based non-interactive
plots as svg graphics. Wrap your plot-producing code in a function within `Graphics.svg` as below:
*)

let widgets = [ 3; 8; 12; 15; 19; 18; 18; 20; ]

Graphics.svg 7 4 (fun _ -> R.plot widgets)
(*** include-it-raw ***)

Graphics.svg 7 4 (fun _ -> R.barplot widgets)
(*** include-it-raw ***)

(**
## Using ggplot2

ggplot2 is an R package that expresses the grammar of graphics.
Using RProvider, we can plot F# data in publication-quality plots
using ggplot2.
*)

open RProvider.ggplot2
open RProvider.datasets

let (++) a b = R.ggplot__add (a,b)

Graphics.svg 7 4 (fun _ ->
    R.ggplot(R.mtcars, R.aes(x = "mpg", y = "disp")) ++
    R.geom__point()
)
(*** include-it-raw ***)


(**
## Exporting and Saving Charts

The RProvider Graphics.svg callback (above) may be used to generate
svg graphics.

Alternatively, you may use R graphics devices directly and manipulate
them through R functions.

**Note. Some graphics devices are not happy running when R is embedded in another process like RProvider.**
For example, on macOS calling Quartz will crash the process, as it will not run outside of the main thread of a process.
X11 is more stable on macOS.

An example is shown below of using the PNG device.
*)

open RProvider.grDevices

// Open the device and create the file as a png.
// R.bmp, R.jpeg, R.pdf, ... will generate other formats.
R.png(filename = "test.png", height = 200, width = 300, bg = "white")
// Create the chart into the file
R.barplot widgets
// Close the device once the chart is complete
R.dev_off ()


(**
## R plot arguments

Named parameters allow you to specify every argument supported by R, 
as an list of label and value tuples.

An example of using named arguments is below.
*)

open RProvider.Operators

let sprokets = [ 5.3; 6.5; 1.2; 5.3; 4.; 18.; 15.2; 12.1 ]

Graphics.svg 7 4 (fun _ ->
    R.plot [
        "x" => widgets
        "type" => "o"
        "col" => "blue"
        "ylim" => [0; 25] ] |> ignore
    R.lines [
        "x" => sprokets
        "type" => "o"
        "pch" => 22
        "lty" => 2
        "col" => "red" ]
)
(*** include-it-raw ***)
