(**
---
category: Core Concepts
categoryindex: 3
index: 3
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


open RProvider

(**
# Passing Data into R functions from F#

R functions - as defined in RProvider - do not have strong types for their parameters. How to pass data and functions into R from F# therefore becomes a key consideration.

## A note on NA and NaN values

In R, NA is a distinct value on the base atomic types. For example, a factor, numeric, or character vector may contain NA values. In addition, numeric types may be NaN, which is distinct from NA.

F# does not have a natural representation of NA values within its core types. To mirror R's behaviour, we use option values throughout F#-R data processing in RProvider. You may request non-option values, but if NAs are present any extraction functions will fail with an error to this effect.

## Passing different types of values

### Existing R values

If you pass `RExpr` values (or any semantic R type wrappers, e.g. `Factor`, `DataFrame`, `RComplex`) to any R functions, they will be passed directly to the function without conversion or moving into F# memory space. In effect, F# becomes a typed layer over R. By using semantic R type wrappers, you can orchestrate R computations through the typed view provided by RProvider.
*)

open RProvider.stats

R.rnorm(n = 20, mean = 0.2, sd = 0.02)
|> R.mean

(**
### R function parameters

R has some high-level functions (e.g. sapply) that require a function parameter. Although F# has first-class support of functional programming and provides better functionality and syntax for apply-like operations, which often makes it sub-optimal to call apply-like high-level functions in R, the need for parallel computing in R, which is not yet directly supported by F# parallelism to R functions, requires users to pass a function as parameter. Here is an example way to create and pass an R function:
*)
let fun1 = R.eval(R.parse(text="function(i) {mean(rnorm(i))}"))
let nums = R.sapply(R.c(1,2,3), fun1)

(**
### F# values

RProvider contains layers that allow passing F# values to R in either an explicit and implicit way:

* Explicit: You create an R semantic type from an F# value, which copies it into R memory space.
* Implicit: You specify the F# value as a function parameter to a `R.*` function, causing an implicit conversion.

#### Explicit conversion

Using the semantic R types layer, you may pass F# values into appropriate supported R core types.
*)

// The namespace containing core types:
open RProvider.Runtime.RTypes

let vector = Real.Vector.fromFloats [ 1. .. 10. ]

(**
See [R semantic types](semantic-types.html) for more information.

#### Implicit conversion (as R function arguments)

RProvider defines all of the parameters of R functions are obj, meaning that *any* F# type may be passed. Internally, RProvider checks deterministically for a plausable conversion of any F# type specified as a function parameter.

#### Parameter Types

Since all arguments to functions are of type obj, it is not necessarily obvious what you can pass. Ultimately, you will need to know what the underlying function is expecting; intellisense and the package's documentation should indicate this.

Once you have determined *what* R is expecting, you may pass an appropriate F# value. The below table indicates which F# types map to which R types.

| R type | F# type | Notes |
| --- | --- | --- |
| character | string [ + array / list ] [ + option ] | |
| complex | RComplex [ + array / list ] [ + option ] | |
| integer | int [ + array / list ] [ + option ] | |
| logical | bool [ + array / list ] [ + option ] | |
| numeric | float [ + array / list ] [ + option ] | |
| list | - | Call R.list, passing the list elements as the arguments |
| dataframe | - | Call R.data_frame, using a list of named args to indicate the column names |

Importantly, RProvider accepts single values (e.g. 1), lists ([ 1 ]), arrays ([| 1 |]), and also option values (Some 1), option lists ([ Some 1 ]), and option arrays ([| Some 1 |]). Option values here represent NA in R.

## Consuming R function results

Functions exposed by the RProvider return the erased type `RExpr`. This keeps all return data inside R data structures, so does not impose any data marshalling overhead.  If you want to pass the value in as an argument to another R function, you can simply do so.

RProvider supports two ways of accessing results:

* Explicit: typed access into semantic wrappers and type-specific extraction members / functions.
* Implicit: conversion into F# values using `.FromR`.

See [working with R expressions](expressions.html) for more information.

### Explicit typed access

RProvider includes typed access into R values using the semantic type layer.

If there are no implicit conversions available, you must either pass the value to another R function for further processing, or use typed access into a relevant semantic type.

If there are no supported conversions, you can access the data through the RDotNet object model.  RDotNet exposes properties, members and extension members (available only if you open the RDotNet namespace) that allow you to access the underlying data directly.  So, for example:
*)

let res = R.sum([|1;2;3;4|])
res.AsTyped
(*** include-it ***)

(**

### Implicit conversion

Conversion is available to `RExpr` values by using either the `RExpr` module's functions, or using members on the value.

Without specifying a type, the functions default to obj, so further type checking will be required.
*)

let pi = R.pi

RExpr.getValue pi
RExpr.tryGetValue pi

pi.FromR()
pi.TryFromR()

(**
It is better to specify the type, so that a strongly typed value may be retrieved:
*)

RExpr.getValue<float> pi
RExpr.tryGetValue<float> pi

pi.FromR<float>()
pi.TryFromR<float>()

(**
Type conversions supported are:

| R Type | Requested F#/.NET Type |
| --- | --- |
| character (when vector is length 1) | string or string[] (+ option) |
| character | string[] (+ option) |
| complex (when vector is length 1) | RComplex or RComplex[] (+ option) |
| complex | RComplex[] (+ option) |
| integer (when vector is length 1) | int or int[] (+ option) |
| integer | int[] (+ option) |
| logical (when vector is length 1) | bool or bool[] (+ option) |
| logical | bool[] (+ option) |
| numeric (when vector is length 1) | float or float[] (+ option) |
| numeric | float[] (+ option) |

## Design notes

R has a very different specification than F#:

* All R formal parameters have names, and you can always pass their values either by name or positionally.  If you pass by name, you can skip arguments in your actual argument list.  We simply map these onto F# arguments, which you can also pass by name or positionally.

* In R, essentially all arguments are optional (even if no default value is specified in the function argument list).  It's up to the receiving function to determine whether to error if the value is missing.   So we make all arguments optional.

* R functions support ... (varargs/paramarray).  We map this onto a .NET ParamArray, which allows an arbitrary number of arguments to be passed.  However, there are a couple of kinks with this:

* R allows named arguments to appear _after_ the ... argument, whereas .NET requires the ParamArray argument to be at the end.  Some R functions use this convention because their primary arguments are passed in the ... argument and the named arguments will sometimes be used to modify the behavior of the function.  From the RProvider you will to supply values for the positional arguments before you can pass to the ... argument.  If you don't want to supply a value to one of these arguments, you can explicitly pass System.Reflection.Missing.

* Parameters passed to the R ... argument can also be passed using a name.  Those names are accessible to the calling function.  Example are list and dataframe construction (R.list, and R.data_frame).  To pass arguments this way, you can use the overload of each function that takes an IDictionary<string, obj>, either directly, or using the namedParams function.  For example:

    `R.data_frame [ "A", [|1;2;3|]; "B", [|4;5;6|] ]`

*)