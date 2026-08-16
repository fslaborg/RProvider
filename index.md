# F# R Type Provider

**A typed interop layer that embeds R within F#.**

The F# Type Provider enables interoperability between F# and [R](http://www.r-project.org/) through strongly-typed representations of F# types. The Type Provider discovers R packages that are available  in an R installation and makes them available as namespaces in F#.

From F#, you can call any R function, run statistical analyses, generate plots, and work with R objects interactively. This lets you combine R’s extensive statistical and visualisation ecosystem with F#'s succinct and expressive type system — including type providers, units of measure, and functional data pipelines.

The below example shows a simple base R example within F#:

```fsharp
open RProvider
open RProvider.stats

let x = [ 1. .. 5. ] |> R.c
let y = [ 1.2; 2.1; 2.9; 4.5; 4.8 ] |> R.c

// Run a correlation test in R
let result = R.cor_test(x, y)

// Extract results into F#
let pValue = result |> RExpr.listItem "p.value" |> RExpr.getValue<float>
let statistic = result |> RExpr.listItem "statistic" |> RExpr.getValue<float>
let estimate = result |> RExpr.listItem "estimate" |> RExpr.getValue<float>

printfn "Correlation estimate: %g\nTest statistic: %g\np-value: %g" estimate statistic pValue
```

```
Correlation estimate: 0.984939
Test statistic: 9.86672
p-value: 0.00221371
```

The above example is run through F# interactive (`dotnet fsi`).

## Using the R Type Provider

**Prerequisites**. R 4.5.0 or higher, .NET 10 or higher, and the R_HOME environment variable set. [More info](requirements.html).

In an F# script:

```fsharp
#r "nuget:RProvider"

open RProvider

```

To add to a .NET project, from the terminal:

```fsharp
dotnet add package RProvider

```

## What are R and F#?

[F#](http://fsharp.org) is a multi-paradigm language
that supports functional, object and imperative programming,
with the emphasis on functional-first programming. F# runs on the .NET runtime and is a compiled,
statically typed language with a strong type system and type inference.
F# is a general purpose programming language, and is particularly well-suited for scientific/numerical computing.

[R](http://www.r-project.org/) is a domain-specific language for statistical computing.
R has a rich ecosystem of community-developed packages across scientific disciplines.
R has many packages for publication-quality graphics, such as ggplot.
R is an interpreted, dynamically typed language for data exploration that is typically used
R-specific IDEs like [RStudio](http://www.rstudio.com/).

## Contributing and copyright

The project was originally developed by [BlueMountain Capital](https://www.bluemountaincapital.com/), and has since been developed and open-source contributors.

The project is hosted on [GitHub](https://github.com/fslaborg/RProvider) where you can [report issues](https://github.com/fslaborg/RProvider/issues), fork the project and submit pull requests.
