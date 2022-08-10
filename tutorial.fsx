(**
// can't yet format YamlFrontmatter (["category: Documentation"; "categoryindex: 1"; "index: 1"], Some { StartLine = 2 StartColumn = 0 EndLine = 5 EndColumn = 8 }) to pynb markdown

*)
#r "nuget: RProvider,{{package-version}}"
(**
# R Provider Tutorial

This tutorial demonstrates how to use the R type provider in an F# script. You
can also use the R type provider in other scenarios such as apps and libraries.

## System requirements

Make sure you have set up your system as specified [here](requirements.fsx).

## Referencing the provider

First, make a new F# script (e.g., sample.fsx). In your new script, first load
the R type provider from the NuGet package repository.

    [lang=fsharp]
    #r "nuget:RProvider"

For this tutorial, we use `open` to reference a number of packages 
including `stats`, `tseries` and `zoo`:

*)
open RProvider
open RProvider.graphics
open RProvider.stats
open RProvider.tseries
open RProvider.zoo

open System
open System.Net.Http
(**
If either of the namespaces above are unrecognized, you need to install the package in R
using `install.packages("stats")`.

## Pretty-printing R values

Add this line to your script to tell F# interactive how to print out
the values of R objects:

*)
fsi.AddPrinter FSIPrinters.rValue
(**
## Obtaining data

In this tutorial, we use [F# Data](http://fsharp.github.io/FSharp.Data/) to access stock
prices from the Yahoo Finance portal. For more information, see the documentation for the
[CSV type provider](http://fsharp.github.io/FSharp.Data/library/CsvProvider.html).

The following snippet defines a function `getStockPrices` that returns
array with prices for the specified stock and a specified number of days from a stocks API:

*)
// NB The 'demo' key has very limited usage.
let apiKey = "demo"

// URL of a service that generates price data
let url stock = sprintf "https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol=%s&apikey=%s&datatype=csv" stock apiKey

/// Returns prices (as tuple) of a given stock
let getStockPrices stock count =
    // Download the data and split it into lines
    use wc = new HttpClient()
    let data = wc.GetStringAsync(url stock) |> Async.AwaitTask |> Async.RunSynchronously
    let dataLines = data.Split([| '\n' |], StringSplitOptions.RemoveEmptyEntries)
 
    // Parse lines of the CSV file and take specified
    // number of days using in the oldest to newest order
    seq { for line in dataLines |> Seq.skip 1 do
              let infos = line.Split(',')
              yield float infos.[4] }
    |> Seq.truncate count |> Array.ofSeq |> Array.rev

/// Get opening prices for MSFT for the last 100 days
let msftOpens: float[] = getStockPrices "MSFT" 100
(**
## Calling R functions

Now, we're ready to call R functions using the type provider. The following snippet takes
`msftOpens`, calculates logarithm of the values using `R.log` and then calculates the 
differences of the resulting vector using `R.diff`:

*)
// Retrieve stock price time series and compute returns
let msft = msftOpens |> R.log |> R.diff
(**
If you want to see the resulting values, you can call `msft.AsVector()` in F# Interactive.
Next, we use the `acf` function to display the atuo-correlation and call `adf_test` to
see if the `msft` returns are stationary/non-unit root:

*)
let a = R.acf(msft)
let adf = R.adf_test(msft) 
(**
After running the first snippet, a window similar to the following should appear (note that
it might not appear as a top-most window).

<div style="text-align:center">
<img src="img/acf.png" />
</div>

Finally, we can obtain data for multiple different indicators and use the `R.pairs` function
to produce a matrix of scatter plots:

*)
// Build a list of tickers and get diff of logs of prices for each one
let tickers = 
  [ "MSFT"; "AAPL"; "X"; "VXX"; "SPX"; "GLD" ]
let data =
  [ for t in tickers -> 
      printfn "got one!"
      t, getStockPrices t 255 |> R.log |> R.diff ]

// Create an R data frame with the data and call 'R.pairs'
let df = R.data_frame(namedParams data)
R.pairs(df)
(**
As a result, you should see a window showing results similar to these:

<div style="text-align:center">
<img src="img/pairs.png" />
</div>


*)

