(**
---
title: Quickstart: stats
category: Getting Started
categoryindex: 2
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
# Quickstart: Using Statistical Packages

A strong R community has contributed over 20,000 packages to CRAN,
R's central package registry. The F# R Type Provider enables you to
use every single one of them from within the F# environment.

Using RRrovider, you can orchestrate R workflows and manipulate R data,
pass in F# values, and extract R values back to F#.

For this example, we simply demonstrate some basic RProvider concepts
using the built-in `stats` package.

## Example: Linear Regression

Let's perform a simple linear regression from the F# interactive, 
using the R.lm function.

Once you have referenced RProvider's nuget package in your script,
library, or app, you can reference the required libraries and packages this way:
*)

open RProvider
open RProvider.Operators

open RProvider.graphics
open RProvider.stats

(**
Once the libraries and packages have been loaded, 
Imagine that our true model is

Y = 5.0 + 3.0 * X1 - 2.0 * X2 + noise

Let's generate a fake dataset using F# that follows this model:
*)

// Random number generator
let rng = System.Random()
let rand () = rng.NextDouble()

// Generate fake X1 and X2 
let X1s = [ for i in 0 .. 9 -> 10. * rand () ]
let X2s = [ for i in 0 .. 9 -> 5. * rand () ]

// Build Ys, following the "true" model
let Ys = [ for i in 0 .. 9 -> 5. + 3. * X1s.[i] - 2. * X2s.[i] + rand () ]

(**
Using linear regression on this dataset, we should be able to 
estimate the coefficients 5.0, 3.0 and -2.0, with some imprecision
due to the "noise" part.

Let's first put our dataset into a R dataframe; this allows us
to name our vectors, and use these names in R formulas afterwards:
*)

let dataset = [ 
    "Y" => Ys
    "X1" => X1s
    "X2" => X2s ] |> R.data_frame

(**
We can now use R to perform a linear regression.
We call the [R.lm function](http://stat.ethz.ch/R-manual/R-patched/library/stats/html/lm.html),
passing it the formula we want to estimate. 
(See the [R manual on formulas](http://stat.ethz.ch/R-manual/R-patched/library/stats/html/formula.html) 
for more on their somewhat esoteric construction) 
*)

let result = R.lm(formula = "Y~X1+X2", data = dataset)

(**
## Extracting Results from R to F#

The result we get back from R is a R Expression. 
The R Type Provider tries as much as possible to keep data
as R Expressions, rather than converting back-and-forth
between F# and R types. It limits translations 
between the 2 languages, which has performance benefits, 
and simplifies composing R operations. On the other hand, 
we need to extract the results from the R expression 
into F# types.

The [R docs for lm](http://stat.ethz.ch/R-manual/R-patched/library/stats/html/lm.html) 
describes what R.lm returns: a R List. We can now retrieve each element, 
accessing it by name (as defined in the documentation). 
For instance, let's retrieve the coefficients and residuals, 
which are both R vectors containg floats:
*)

let coefficients = result?coefficients.AsVector().AsReal()
let residuals = result?residuals.AsVector().AsReal()


(**
We can also produce summary statistics about our model,
like R^2, which measures goodness-of-fit - close to 0
indicates a very poor fit, and close to 1 a good fit.
See [R docs for the details on Summary](http://stat.ethz.ch/R-manual/R-patched/library/stats/html/summary.lm.html).
*)

let summary = R.summary result

summary?``r.squared``.AsScalar()
(*** include-it ***)

(**
Finally, we can directly pass results, which is a R expression,
to R.plot, to produce some fancy charts describing our model:
*)

Graphics.svg 8 4 (fun _ -> R.plot result)
(*** include-it-raw ***)

(**
That's it - while simple, we hope this example illustrate
how you would go about to use any existing R statistical package. 
While the details would differ, the general approach would
remain the same. Happy modelling!
*)