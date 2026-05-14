(** 
---
category: Core Concepts
categoryindex: 3
index: 4
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
Operators
===============

RProvider includes the `RProvider.Operators` module, which contains custom operators that can make working with R easier. Make sure to open it alongside your packages:
*)

open RProvider
open RProvider.Operators

open RProvider.``base``
open RProvider.datasets
open RProvider.stats

(**
# Accessing members / slots

You can use the dynamic (`?`) operator to access:

* Slots in S4 objects
* Members of list types

### List: accessing named columns in a dataframe.

*)

R.mtcars?mpg

(**
### S4 object: access a slot 
*)

R.eval "setClass('testclass', representation(foo='character', bar='integer'))"

let test = R.eval "new('testclass', foo='s4', bar=1:4)"

test?foo
