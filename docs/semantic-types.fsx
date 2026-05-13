(** 
---
title: R Semantic Types
category: Guides
categoryindex: 4
index: 1
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

open RProvider
open RProvider.Operators

open RProvider.``base``
open RProvider.datasets
open RProvider.stats

(**
## Numeric

In R, as in F#, there is a distinction between integer and real (floating-point) numeric values.

R represents floating point numbers through the 'real' data type. Here, we include semantic types for R's real numbers that include full arithmetic support. Similarly, the integer semantic type includes arithmetic support. However, R automatically casts integers to real numbers during any mathematical operations, even if only integers are involved. Our semantic types therefore follow the same casting rules.

### Scalars

To create an integer from an F# value:
*)

open FSharp.Data.UnitSystems.SI.UnitSymbols

let x = Runtime.RTypes.Real.Scalar.fromFloat 2.1<m> |> Option.get
let y = Runtime.RTypes.Real.Scalar.fromFloat 54.9<s> |> Option.get

let z = x / y


(**
### Vectors

*)

(**
## Heterogeneous Lists (HLists)

In R, lists may contain values of disparate types. In F#, lists must be homogeneous.

RProvider includes a semantic type for R's heterogeneous lists.

*)

R.mtcars?mpg

(**

...

## Factors

...

## Data Frames

...
*)
