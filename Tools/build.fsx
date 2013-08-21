// --------------------------------------------------------------------------------------
// Builds the documentation from FSX files in the 'samples' directory
// (the documentation is stored in the 'docs' directory)
// --------------------------------------------------------------------------------------

#I "References"
#load "References/literate.fsx"
open System.IO
open FSharp.Literate

let (++) a b = Path.Combine(a, b)
let template = __SOURCE_DIRECTORY__ ++ "template.html"
let sources  = __SOURCE_DIRECTORY__ ++ "../Documentation"
let output   = __SOURCE_DIRECTORY__ ++ "../Documentation/output"

// Root URL for the generated HTML
let root = "file:///C:\Tomas\Projects\RProvider\Documentation\output" // "http://fsharp.github.com/FSharp.Data"

let references = 
  [ __SOURCE_DIRECTORY__ ++ "../bin/Debug/RDotNet.dll"
    __SOURCE_DIRECTORY__ ++ "../bin/Debug/RProvider.dll"
    __SOURCE_DIRECTORY__ ++ "../Documentation/lib/FSharp.Data.dll" ]

// Generate HTML from all FSX files in samples & subdirectories
let build() = 
  Literate.ProcessDirectory
    ( sources, template, output, 
      replacements = [ "root", root ],
      compilerOptions = String.concat " " (List.map (sprintf "-r:\"%s\"") references) )

build()