#i "nuget:https://www.nuget.org/api/v2"
#i @"nuget:/Volumes/Server HD/GitHub Projects/RProvider/bin"
#r "nuget:RProvider,2.0.2"
#r "nuget:Deedle,2.5.0"
//#r "nuget:Deedle.RProvider.Plugin,2.5.0"

(* This sample shows a plugin for RProvider, which converts
   R values into .NET types. Here, the Deedle RProvider plugin
   (from the `Deedle.RProvider.Plugin` package) automatically
   converts an R data frame into a Deedle frame by adding a
   type signature in both directions. *)

open RProvider
open RProvider.``base``
open RProvider.datasets
open Deedle

do fsi.AddPrinter(fun (synexpr:RDotNet.SymbolicExpression) -> synexpr.Print())

// Get mtcars as an untyped object
R.mtcars.Value

// Get mtcars as a typed Deedle frame
let mtcars : Frame<string, string> = R.mtcars.GetValue()

// Pass Deedle data to R and print the R output
R.as_data_frame(mtcars)

// Pass Deedle data to R and get column means
R.colMeans(mtcars)