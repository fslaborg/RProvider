F# R Type Provider
=======

The F# Type Provider is a mechanism that enables smooth interoperability
between F# and [R](http://www.r-project.org/). 
The Type Provider discovers R packages that are available 
in your R installation and makes them available as .NET namespaces 
underneath the parent namespace `RProvider`. 

The Type Provider makes it possible to use 
all of R capabilities, from the F# interactive environment. 
It enables on-the-fly charting and data analysis using R packages, 
with the added benefit of IntelliSense over R, 
and compile-time type-checking that the R functions you are using exist. 
It allows you to leverage all of .NET libraries,
as well as F# unique capabilities to access and manipulate data 
from a wide variety of sources via Type Providers.

### A Quick Demo

<div style="text-align:center;">
<iframe style="margin-left:auto;margin-right:auto;" width="420" height="315" src="http://www.youtube.com/embed/_BOST3W88-Y" frameborder="0" allowfullscreen></iframe>
</div>

## What are R and F#?

[F#](http://msdn.microsoft.com/en-us/vstudio/hh388569) is a mixed-paradigm language 
that supports functional, object-oriented and imperative programming, 
with the emphasis on functional. F# runs on the .NET runtime and is a compiled, 
statically typed language with a strong type system and type inference. 
F# is a general purpose programming language, 
and is particularly well-suited for scientific/numerical computing.

[R](http://www.r-project.org/) is an Open Source language for statistical computing. 
R offers a wide range of high-quality, community-developed packages, 
covering virtually every area of statistics, econometrics or machine learning. 
It is also famous for its charting capabilities, making it a great tool 
to produce publication-quality graphics. 
R is an interpreted, dynamically typed language that is typically used 
from its GUI, [RStudio](http://www.rstudio.com/), or command line interactive environment.

## Using the R Type Provider

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      Install using the <a href="https://nuget.org/packages/RProvider/">NuGet package</a>.
      Run the following command in the <a href="http://docs.nuget.org/docs/start-here/using-the-package-manager-console">Package Manager Console</a>:
      <pre>PM> Install-Package RProvider</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>

### Pre-requisites

The R Provider requires an installation of R for Windows, downloadable from
[here](http://cran.cnr.berkeley.edu/bin/windows/base/).  RProvider uses the R registry key
`SOFTWARE\R-core` to locate the R binary directory, in order to load `R.dll`.  It will also
locate `R.dll` if it is on the path.  If run from a 32-bit process, RProvider will use
the 32-bit `R.dll`, and if run from a 64-bit process, it will load the 64-bit version.

If you are using R 2.15 or later, you should not try to load the RProvider inside a script
that is passed to FSI via the `--use` flag.  It seems that something about the way R
initializes causes it to hang in that context.  Works fine if you load later.

For compilation you will need VS2012 / F# 3.0 or later.  For runtime you'll need .NET 4.5.

Contributing and copyright
--------------------------

<img src="http://www.bluemountaincapital.com/media/logo.gif" style="float:right;margin:10px;margin-top:0px;" />

The project has been developed by [BlueMountain Capital](https://www.bluemountaincapital.com/)
and contributors. It is available under the BSD license. The library uses
[RDotNet](http://rdotnet.codeplex.com/) which is also covered by the BSD license.
For more information see the [License file][license] in the GitHub repository.

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork
the project and submit pull requests.
Many thanks to Mathias Brandewinder for producing the [FAKE](https://github.com/fsharp/FAKE) script
to build the NuGet package, and to Steffen Forkman for writing [FAKE](https://github.com/fsharp/FAKE).

[gh]: https://github.com/blueMountainCapital/FSharpRProvider
[issues]: https://github.com/blueMountainCapital/FSharpRProvider/issues
[license]: https://github.com/blueMountainCapital/FSharpRProvider/blob/master/LICENSE.md
