(** 
---
title: R Semantic Types
category: Core Concepts
categoryindex: 3
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
Semantic Types
===============

RProvider includes a semantic type layer that provides strongly typed wrappers around R values.
*)

(*** hide ***)
open RProvider
open RProvider.Operators

open RProvider.datasets
open RProvider.stats

fsi.AddPrinter FSIPrinters.rValue

(**
## Numeric

In R, as in F#, there is a distinction between integer and real (floating-point) numeric values.

R represents floating point numbers through the 'real' data type. Here, we include semantic types for R's real numbers that include full arithmetic support. Similarly, the integer semantic type includes arithmetic support.

**Important.** R automatically casts integers to real numbers during any mathematical operations, even if only integers are involved. Our semantic types therefore follow the same casting rules.

### Scalars

R does not make a distinction between scalars and vectors; scalars are simply vectors of length one. Here, we have made a distinction.

To create an integer from an F# value:
*)

open FSharp.Data.UnitSystems.SI.UnitSymbols

let i = Runtime.RTypes.Integer.Scalar.fromInt 1<kg> |> Option.get

(**
Similarly, to create R-based real (floating point) numbers from F# values:
*)

let x = Runtime.RTypes.Real.Scalar.fromFloat 2.1<m> |> Option.get
let y = Runtime.RTypes.Real.Scalar.fromFloat 54.9<s> |> Option.get

let z = x / y


(**
### Vectors

Vectors may be created within R memory in a similar way to scalars:
*)

let dist = Runtime.RTypes.Real.Vector.fromFloats [ 1.<m> .. 1.<m> .. 10.<m> ]
let speed = Runtime.RTypes.Real.Vector.fromFloats [ 1.<m/s> .. 1.<m/s> .. 10.<m/s> ]

// Pass vectors into R functions
let m = R.median dist

// Unit-aware arithmetic
let time = dist / speed

(**
### Arithmetic

The int- and float-based R semantic types support F# operators; underneath, these call
R's operators. In effect, you can orchestrate R calculations using the F# typed view
of the values.
*)

let ex1 = x + x
let ex2 = x * y
let ex3 = ex1 / ex2

(**
Of course, RProvider's basic type inference means that we do not know the unit-typing of R's functions, so at present any returned values from R functions are dimensionless, regardless of inputs.

Note that any operations on integers in R results in real numbers as the return type.

### Named vectors

In R, vectors may be named such that each value has a label. RProvider is configured to allow
dotting into the elements of a vector to retrieve a scalar based on either an integer index
or a string name.
*)

RExpr.typedVectorByName
RExpr.typedVectorByIndex

(**
## Factors

Factors are a core R data type and are used to represent categorical variables. In essence, they are a list of classes plus an integer-based vector of indices. You may inspect and extract R factors semantically, as in the below example which again is using the built-in penguin data frame:
*)

let species = R.penguins?species.AsFactor()

species.Levels.Value
(*** include-it ***)

species.Indices

// Extract the factor to an F# list of strings
species.AsStringVector.Value

(**
## Heterogeneous Lists (HLists)

In R, lists may contain values of disparate types. In F#, lists must be homogeneous.

For example, the let's make a new list:
*)

let hetL = R.list("apple", [Some "pear"; None], 123.0)

hetL
(*** include-it ***)

hetL.Type
(*** include-it ***)

(**
RProvider includes a semantic type for R's heterogeneous lists.
We can type an R value into the semantic type as follows:
*)

let hetLSem = hetL.AsList()

hetLSem.Length
(*** include-it ***)

hetLSem.[2]
(*** include-it ***)

(**
If the list has named items, you may also dot in using a string name. 

## Data Frames

Data frames are a core base type in R. Internally, they are simply lists.

*)

let iris = R.iris.AsDataFrame()

iris.Names
(*** include-it ***)

(**
Columns may be accessed using `.Column`. The return type is a `Column`, which
is a discriminated union of possible R data types.
*)

iris.Column "Sepal.Width"
(*** include-it ***)

(**
We use a DU because the column may be one of many data types, as internally the
data frame is just a heterogeneous list with a class attached.
*)