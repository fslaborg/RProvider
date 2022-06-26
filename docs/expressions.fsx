(** 
---
category: Documentation
categoryindex: 1
index: 4
---
*)

(*** condition: prepare ***)
#nowarn "211"
#r "../src/RProvider/bin/Release/net5.0/RDotNet.dll"
#r "../src/RProvider/bin/Release/net5.0/RDotNet.FSharp.dll"
#r "../src/RProvider/bin/Release/net5.0/RProvider.Runtime.dll"
#r "../src/RProvider/bin/Release/net5.0/RProvider.DesignTime.dll"
#r "../src/RProvider/bin/Release/net5.0/RProvider.dll"
(*** condition: fsx ***)
#if FSX
#r "nuget: RProvider,{{package-version}}"
#endif // FSX
(*** condition: ipynb ***)
#if IPYNB
#r "nuget: RProvider,{{package-version}}"
#endif // IPYNB

(**
Working with R expressions
===============

RProvider represents R objects and values through a `SymbolicExpression` type (derived from R.NET). RProvider includes a `SymbolicExpression` module that allows you to work with R expressions in a more idiomatic way using forward pipes (`|>`). First, open RProvider, its custom operators and any packages you need:
*)

open RProvider
open RProvider.Operators

open RProvider.``base``
open RProvider.datasets
open RProvider.stats

(**

# S4 classes

For this example, let's set up an S4 class and object from scratch:
*)

let x = R.rnorm(100)
x.Engine.Evaluate("setClass('testclass', representation(foo='character', bar='integer'))")
let s4 = x.Engine.Evaluate("new('testclass', foo='s4', bar=1:4)")

(**
You can find out if there are slots using the `slots` and `trySlots` functions:
*)

s4 |> SymbolicExpression.slots
s4 |> SymbolicExpression.trySlots
R.mtcars |> SymbolicExpression.trySlots

(**
You can access slot values similarly with the `slot` and `trySlot` functions:
*)

s4 |> SymbolicExpression.slot "foo"
s4 |> SymbolicExpression.trySlot "foo"
s4 |> SymbolicExpression.trySlot "doesntexist"
