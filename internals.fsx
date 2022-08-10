(**
// can't yet format YamlFrontmatter (["category: Developer"; "categoryindex: 2"; "index: 3"], Some { StartLine = 1 StartColumn = 0 EndLine = 4 EndColumn = 8 }) to pynb markdown

# How does it work?

## How do we call into R?
The RProvider loads the R.DLL (which contains the core of R) into the calling process, via [RDotNet](http://rdotnet.codeplex.com/).  This will happen in several places:

* In the IDE, to provide IntelliSense for packages/functions/parameters.
* In the F# compiler, to generate code that calls the R functionality you are calling.
* In your resulting binary, to execute the generated code.
* In F# Interactive, to do all of the above interactively.

## How does RDotNet help?
RDotNet allows R functionality to be called from .NET, and exposes an object model for representing R values (based on the type RDotNet.SymbolExpression).  Using RDotNet, one executes R code by passing strings of R code into an Evaluate method.  From the RProvider, we introspect on available R packages and functions and expose them as members of provided types.  You can then call them just like regular .NET functions, with IntelliSense and compile-time checking.  RDotNet.SymbolicExpression provides a nice OO model of the R native SEXP type, so we simply expose results using that type.  To make it more friendly from F#, we extend it with some extension members and active patterns.

## How do we expose R packages?
RProvider determines the set of installed packages in your R installation and exposes them as namespaces under the root RProvider namespace.  This allows you to 'open' the namespaces you want to use as if they were regular .NET namespaces.

Under the namespace for a given package, RProvider exposes a single static type called "R", which contains static methods mirroring each of the functions and values that exist in the package.  This means that the members under "R." will be the union of available functions in all of the package namespaces you have opened.

## Is it Statically Typed?
Kind of.  It is statically type checked to the extent that it can be given the type information available from R.  In practice, this means that the F# compiler and IDE statically checks that the function you are calling exists in the package you are calling.  In some cases, we can also determine that you are not passing too many arguments to the function, though common use of ... (aka varargs/params) in R functions defeats that in many cases.

R is dynamically typed, so we cannot determine what the types of function arguments are supposed to be.  So all arguments are of type obj.  R functions can also be written such that they will work even if arguments that do not have default values are omitted, so we expose each argument as optional.  And for R functions that accept a ... argument, we expose a paramarray argument that allows any number of additional arguments to be passed.  In that case, you can basically pass any number of arguments to the function.
*)

