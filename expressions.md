# Working with R expressions

RProvider represents R objects and values through an `RExpr` type. RProvider includes an `RExpr` module that allows you to work with R expressions in a more idiomatic way using forward pipes (`|>`). First, open RProvider, its custom operators and any packages you need:

```fsharp
open RProvider
open RProvider.Operators

open RProvider.datasets
open RProvider.stats
```

Next, let's use a sample R dataframe to demonstrate core R expression functions.

```fsharp
let df = R.penguins
```

Note: in most cases, there are equivalent functions available using either the module (`RExpr.*`) or using dot notation as we preference below.

### Inpecting a value

You may print the expression identically to using R print() using the .Print function or `RExpr.printToString`.

```fsharp
df.Print()
```

When using F# interactive (fsi), you may register a pretty-printer to print R values automatically into the terminal using `fsi.AddPrinter FSIPrinters.rValue`.

We can also inspect the 'semantic type' - the standard R shape - of the R expression.
The possible shapes are listed in the `Runtime.RTypes.RSemanticType` discriminated union.

```fsharp
df.Type
```

```
DataFrameType
```

Similarly, you may retrieve the classes of an R object using the classes property.

```fsharp
df.Class
```

```
[|"data.frame"|]
```

#### Printing R values to the console (F# interactive)

Add this line to your script to tell F# interactive how to print out
the values of R objects:

```fsharp
fsi.AddPrinter FSIPrinters.rValue

```

### Extracting values (R -&gt; F#)

We can extract a value from R memory space into to F# primitive values in two ways: type-specific
or default type. For more information, see [passing data](passing-data.html).

For example, let's extract the bill length column from the penguin dataset using the `RExpr` directly (without using the semantic types layer). To access the column, we can use the `?` operator; see [operators](operators.html) for details.

```fsharp
let billLength = df?bill_len
let billVal = df?bill_len |> RExpr.getValue<float[]>
```

#### Extracting using the semantic R types layer

It is also possible to extract values through semantic types. Each semantic type has its own extraction functions specific to the type. For more information, see [semantic R types](semantic-types.html).

Here, we can show how to extract a factor column using a type-safe pattern matching approach.

```fsharp
let speciesFactorSafe =
    match df?species.TryAsTyped with
    | Some t ->
        match t with
        | Runtime.RTypes.FactorInR f -> f.AsStringVector.Value
        | _ -> [||]
    | _ -> [||]
```

We can also do the same thing without try methods if we are certain of the type:

```fsharp
let speciesFactor = df?species.AsFactor().AsStringVector.Value
speciesFactor
```

```
[|"Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie";
  "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie";
  "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie";
  "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie";
  "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie";
  "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie";
  "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie";
  "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie";
  "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie";
  "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie";
  "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie";
  "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie"; "Adelie";
  "Adelie"; "Adelie"; "Adelie"; "Adelie"; ...|]
```

*Note: Older RProvider versions contained a plugin system to register custom converters between .NET types and R types. This has been removed. The new preferred approach is to convert explicitly from semantic type wrappers. For example, an F# data frame library like Deedle could implement a custom conversion function from RProvider's `DataFrame` type.*

### Viewing as an R semantic type

A set of functions enable viewing an `RExpr` as RProvider's R semantic type wrappers.

### Quick access to common properties

The `RExpr` type has members for quick access into common required fields or properties of R objects. These are mirrored in the `RExpr`, which are easier to call with forward pipes for example.

#### Vectors

You can quickly access a single value within a vector using `.ValueAt` and the index.

```fsharp
let v =  [ 0.1 .. 3.5 ] |> R.c

let y : Runtime.RTypes.RScalar<1> = v.ValueAt 0
y.AsReal().FromR.Value

let z : Runtime.RTypes.RScalar<1> = v |> RExpr.typedVectorByIndex 0
z.AsReal().FromR.Value
```

#### Lists and list-based objects

S3 objects in R are basic R objects, but with a class attribute attached.
More often than not, they are lists.

There are multiple quick-access methods to list items:

* Using the ? operator from `RProvider.Operators`.

* Using the `RExpr.listItem` function.

```fsharp
let x = 10.
let summary = R.binom_test(x, 100., 0.5) // alternative = true

summary?``p.value``.FromR<float>()
summary?statistic.FromR<float>()

RExpr.listItem "p.value" summary
```

#### S4 objects (slots)

For this example, let's set up an S4 class and object from scratch:

```fsharp
R.parse(text = "setClass('testclass', representation(foo='character', bar='integer'))") |> R.eval
let s4 = R.parse(text = "new('testclass', foo='s4', bar=1:4)") |> R.eval
s4.Print()
```

```
"An object of class "testclass"
Slot "foo":
[1] "s4"

Slot "bar":
[1] 1 2 3 4

"
```

You can find out if there are slots using the `slots` and `trySlots` functions:

```fsharp
s4 |> RExpr.slots
R.mtcars |> RExpr.trySlots
```

You can access slot values similarly with the `slot` and `trySlot` functions:

```fsharp
s4 |> RExpr.slot "foo"
s4 |> RExpr.trySlot "foo"
s4 |> RExpr.trySlot "doesntexist"
```

## Parallel processing

R itself is not thread-safe. Most R parallel processing libraries focus on running multiple R processes and coordinating work and values between them.

When using RProvider, you may use R from one or many threads. However, the underlying R engine being used is a single R instance. Internally, *RBridge* uses a concurrent queue to process incoming work. You will only gain the perception of multi-threading but none of the speed advantage when consuming R functions.

```fsharp
[| 1 .. 10 |]
|> Array.Parallel.map(fun i -> (R.sqrt i).AsScalar().AsReal().FromR.Value)
```

```
[|Some 1.0; Some 1.414213562; Some 1.732050808; Some 2.0; Some 2.236067977;
  Some 2.449489743; Some 2.645751311; Some 2.828427125; Some 3.0;
  Some 3.16227766|]
```

```fsharp
[| 1 .. 10 |]
|> Array.map(fun i -> (R.sqrt i).AsScalar().AsReal().FromR.Value)
```

```
[|Some 1.0; Some 1.414213562; Some 1.732050808; Some 2.0; Some 2.236067977;
  Some 2.449489743; Some 2.645751311; Some 2.828427125; Some 3.0;
  Some 3.16227766|]
```

As can be seen, the results are identical.

**Important**. Although *pure* functions return identical results, impure functions will not. The internal R instance is sharing it's environment, so you may cross contaminate.
