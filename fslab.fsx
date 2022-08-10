(**
// can't yet format YamlFrontmatter (["category: Documentation"; "categoryindex: 1"; "index: 6"], Some { StartLine = 2 StartColumn = 0 EndLine = 5 EndColumn = 8 }) to pynb markdown

*)
#r "nuget: RProvider,{{package-version}}"
(**
Working with the FsLab ecosystem
===============================

The R type provider is interoperable with other packages in
FsLab through its plugin architecture. Some examples are shown
below. If you would like to see better interoperability between
R and other FsLab packages, submit an issue to their repository
for the creation of an RProvider plugin.

### Deedle - data frame manipulation

Deedle provides types for F# data frame and time series manipulation.
To use with RProvider, first install the Deedle.RPlugin package from
nuget; once this is installed, you do not need to reference it in your
script files.

*)
#r "nuget:Deedle.RPlugin"
(**
In a new F# script file, first open Deedle and RProvider: 

    [lang=fsharp]
    #r "nuget:RProvider"

*)
#r "nuget:Deedle"

open RProvider
open RProvider.``base``
open RProvider.datasets
open Deedle
(**
The Deedle R plugin should be loaded by the R type provider automatically.
You can now convert back and forth between R data frames and Deedle frames
by using type annotations:

*)
let mtcars : Frame<string, string> = R.mtcars.GetValue()

// Pass Deedle data to R and print the R output
R.as_data_frame(mtcars)

// Pass Deedle data to R and get column means
R.colMeans(mtcars)

