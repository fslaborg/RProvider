#I "../src/RProvider/bin/Release/net5.0/"
#r "RDotNet.dll"
#r "RProvider.DesignTime.dll"
#r "RProvider.Runtime.dll"
#r "RProvider.dll"

open RDotNet
open RProvider
open RProvider.graphics
open RProvider.stats
// If either of the namespaces below are unrecognized, you need to install the package in R
open RProvider.tseries
open RProvider.zoo
 
open System
open System.Net

// You can add an API key for AlphaVantage here.
// NB The 'demo' key has very limited usage.
let apiKey = "demo"

// URL of a service that generates price data
let url stock = sprintf "https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol=%s&apikey=%s&datatype=csv" stock apiKey

/// Returns prices (as tuple) of a given stock
let getStockPrices stock count =
    // Download the data and split it into lines
    let wc = new WebClient()
    let data = wc.DownloadString(url stock)
    let dataLines = data.Split([| '\n' |], StringSplitOptions.RemoveEmptyEntries)
 
    // Parse lines of the CSV file and take specified
    // number of days using in the oldest to newest order
    seq { for line in dataLines |> Seq.skip 1 do
              let infos = line.Split(',')
              yield float infos.[4] }
    |> Seq.truncate count |> Array.ofSeq |> Array.rev

//retrieve stock price time series and compute returns
let msft = getStockPrices "MSFT" 255 |> R.log |> R.diff 
 
//compute the autocorrelation of msft stock returns
let a = R.acf(msft)

//lets see if the msft returns are stationary/non-unit root
let adf = R.adf_test(msft) 

//lets look at some pair plots
let tickers = [ "MSFT"; "AAPL"; "X"; "VXX"; "SPX"; "GLD" ]
let data = [ for t in tickers -> t, getStockPrices t 255 |> R.log |> R.diff ]
let df = R.data_frame(namedParams data)
R.pairs(df)

