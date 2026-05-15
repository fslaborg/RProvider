(**

*)
#r "nuget: RProvider,{{package-version}}"
(**
# Operators

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