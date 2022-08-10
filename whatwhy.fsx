(**
// can't yet format YamlFrontmatter (["category: Documentation"; "categoryindex: 1"; "index: 3"], Some { StartLine = 1 StartColumn = 0 EndLine = 4 EndColumn = 8 }) to pynb markdown

# Whats and Whys

## What is R?

[R](http://www.r-project.org/) is an Open Source package for statistical computing.  There are a wide range of community-developed packages available that are very useful in the statistical computing/econometrics space.

R is an interpreted, dynamically typed language that is typically used from its GUI or command line interactive environment.  But R is also embeddable using the R.DLL.

## What is F#?

[F#](http://msdn.microsoft.com/en-us/vstudio/hh388569) is a mixed-paradigm language that supports functional, object-oriented and imperative programming, with the emphasis on functional.  F# runs on the .NET runtime and is a compiled, statically typed language with a strong type system and type inference.  F# is typically used in the scientific/numerical computing space, though is quite widely applicable.

## Why use R with F#?

While there are a number of math/statistical packages available for the .NET platform, none of the approach the power of the packages that are available for R.  R also includes versatile packages for visualization which are hard to match on .NET.

## What is a Type Provider?

F# 3.0 supports a new feature called [Type Providers](http://msdn.microsoft.com/en-us/library/hh156509.aspx) which allow a set of types and members to be determined at compile time (or in the IDE) based on statically known parameters and (optionally) access to some external resource.  The primary purpose of Type Providers is to support strongly-typed access to external data sources, without the additional step of code generation, which adds friction to the development process and is sometimes impractical due to the size of the type space.  Type Providers can also be used to interoperate with another language or runtime environment, by introspecting on constructs available in that environment during compile time and making equivalent constructs available to F#.

## Why not just use R directly?

In some cases, this will make a lot of sense, but there are a number of reasons why it might not:

1. F# is particularly well-suited for the retrieval and manipulation/cleansing of data, which we will subsequently want to use in statistical models.
2. We can combine functionality in .NET libraries (or otherwise callable from F#) with R functionality in a low-friction way.
3. F# is well-suited to building scalable production applications, and using the R type provider allows us to use R functionality from those applications?

*)

