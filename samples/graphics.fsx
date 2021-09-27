#i "nuget:https://www.nuget.org/api/v2"
#i @"nuget:/Volumes/Server HD/GitHub Projects/RProvider/bin"
#r "nuget:RProvider,2.0.0-beta"

open RProvider
open RProvider.graphics
open RProvider.grDevices
open RProvider.datasets

R.x11()

// Calculate sin using the R 'sin' function
// (converting results to 'float') and plot it
[ for x in 0.0 .. 0.1 .. 3.14 -> 
    R.sin(x).GetValue<float>() ]
|> R.plot

// Plot the data from the standard 'Nile' data set
R.plot(R.Nile)