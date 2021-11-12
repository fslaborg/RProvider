#i "nuget:https://www.nuget.org/api/v2"
#i @"nuget:/Volumes/Server HD/GitHub Projects/RProvider/bin"
#r "nuget:RProvider,2.0.2"

open System
open RDotNet
open RProvider
open RProvider.graphics
open RProvider.stats
open RProvider.rstan
open RProvider.ggplot2
open RProvider.methods
open RProvider.Rcpp
open RProvider.StanHeaders
open RProvider.rstudioapi

let fit=R.stan(file= @"files/normal.stan")