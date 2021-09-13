(*** hide ***)
#I "../../packages/FSharp.Data.2.0.7/lib/net40/"
#I "../../bin/"
#r "RDotNet.dll"
#r "RDotNet.FSharp.dll"
#r "RProvider.dll"
#r "RProvider.Runtime.dll"
open System
(**
Reading and writing RData files
===============================

When using R, you can save and load data sets as `*.rdata` files. These can be easily
exported and consumed using the R provider too, so if you want to perform part of your
data acquisition, analysis and visualization using F# and another part using R, you 
can easily pass the data between F# and R as `*.rdata` files.

Passing data from R to F#
-------------------------

Let's say that you have some data in R and want to pass them to F#. To do that, you
can use the `save` function in R. The following R snippet creates a simple `*.rdata`
file containing a couple of symbols from the sample `volcano` data set:

    [lang=text]
    require(datasets)
    volcanoList <- unlist(as.list(volcano))
    volcanoMean <- mean(volcanoList)
    symbols <- c("volcano", "volcanoList", "volcanoMean")
    save(list=symols, file="C:/data/sample.rdata")

To import the data on the F# side, you can use the `RData` type provider that is
available in the `RProvider` namespace. It takes a static parameter specifying the
path of the file (absolute or relative) and generates a type that exposes all the
saved values as static members:
*)
open RProvider

type Sample = RData<"data/sample.rdata">
let sample = Sample()

// Easily access saved values
sample.volcano
sample.volcanoList
sample.volcanoMean

(**
When accessed, the type provider automatically converts the data from the R format
to F# format. In the above example, `volcanoList` is imported as `float[]` and
the `volcanoMean` value is a singleton array. (The provider does not detect that 
this is a singleton, so you can get the value using `sample.volcanoMean.[0]`).
For the `sample.volcano` value, the R provider does not have a default conversion
and so it is exposed as `SymbolicExpression`. 

When you have a number of `*.rdata` files containing data in the same format, you can
pick one of them as a sample (which will be used to determine the fields of the type)
and then pass the file name to the constructor of the generated type to load it.
For example, if we had files `data/sample_1.rdata` to `data/sample_10.rdata`, we could
read them as:
*)
let means = 
  [ for i in 1 .. 10 ->
      let data = Sample(sprintf "data/sample_%d.rdata" i)
      data.volcanoMean.[0] ]
(**
Note that the default conversions available depend on the plugins that are currently
available. For example, when you install the enrie [FsLab](http://www.fslab.org) package
with the [Deedle](https://fslab.org/Deedle/) library, the `RData` 
provider will automatically expose data frames as Deedle `Frame<string, string>` values.

Passing data from F# to R
-------------------------

If you perform data acquisition in F# and then want to pass the data to R, you 
can use the standard R functions for saving the `*.rdata` files. The easiest 
option is to call the `R.assign` function to define named values in the R environment
and then use `R.save` to save the environment to a file:
*)
// Calculate sum of square differences
let avg = sample.volcanoList |> Array.average
let sqrs = 
  sample.volcanoList 
  |> Array.map (fun v -> pown (v - avg) 2)

// Save the squares to an RData file
R.assign("volcanoDiffs", sqrs)
R.save(list=[ "volcanoDiffs" ], file="C:/temp/volcano.rdata")
(**
It is recommended to use the `list` parameter of the `save` function to specify the
names of the symbols that should be saved, rather than saving *all* symbols. The R
provider uses additional temporary symbols and so the saved file would otherwise contain
unnecessary fileds.

Once you save the file using the above command, you can re-load it again using
the `RData` type provider, such as: `new RData<"C:/temp/volcano.rdata">()`.
*)