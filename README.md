# F# R Provider

[![Discord](https://img.shields.io/discord/836161044501889064?color=purple&label=Join%20our%20Discord%21&logo=discord&logoColor=white)](https://discord.gg/VUpfpzfBmd)

<img align="right" src="https://github.com/fslaborg/RProvider/raw/master/docs/img/logo.png" alt="RProvider" />

An F# type provider for interoperating with [R](http://www.r-project.org/). For more information, see [detailed documentation with tutorials, examples and more](https://fslab.org/RProvider//). The following tutorials are a good place to start:

 - [Quickstart: Using Statistical Packages](https://fslab.org/RProvider//Statistics-QuickStart.html)
 - [Quickstart: Creating Charts](https://fslab.org/RProvider//Charts-QuickStart.html)
 - [Tutorial: Analysing Stock Prices](https://fslab.org/RProvider//tutorial.html)

The R Provider discovers R packages that are available in your R installation and makes them available as .NET namespaces underneath the parent namespace RProvider.  For example, the stats package is available as RProvider.stats.  If you open the namespaces you want to use, functions and values will be available as R.name.

---

## Builds

GitHub Actions |
:---: |
[![Github Actions](https://github.com/fslaborg/RProvider/actions/workflows/push.yml/badge.svg?branch=master)](https://github.com/fslaborg/RProvider/actions/workflows/push.yml) |

## NuGet 

Package | Stable | Prerelease
--- | --- | ---
RProvider | [![NuGet Badge](https://buildstats.info/nuget/RProvider)](https://www.nuget.org/packages/RProvider/) | [![NuGet Badge](https://buildstats.info/nuget/RProvider?includePreReleases=true)](https://www.nuget.org/packages/RProvider/)

---

### Requirements

Make sure the following **requirements** are installed on your system:

- [dotnet SDK](https://www.microsoft.com/net/download/core) 5.0 or greater; and
- [R statistical language](http://cran.r-project.org/). _Note: on Windows, there is currently a bug in R preventing us from supporting R versions greater than 4.0.2._
- R_HOME environment variable set to the R home directory. This can usually be identified by running the command 'R RHOME'.

_Note: for .NET framework support, you should use the legacy RProvider 1.2 or earlier; we are no longer supporting these versions._

### What does it do?

The R Provider discovers R packages that are available in your R installation and makes them available as .NET namespaces underneath the parent namespace RProvider.  For example, the stats package is available as RProvider.stats.  If you open the namespaces you want to use, functions and values will be available as R.name.  For example, consider this F# interactive script:

```fsharp
#r "nuget:RProvider"

open RProvider
open RProvider.``base``

let v = R.c(1,2,3)
```

This creates an R numeric vector containing 1,2,3, and names it v.  Note that we had to open the base namespace, since the function 'c' is part of that namespace.  You should also open namespace RProvider, because it contains some helper functions. As type providers are used by Visual Studio and other IDEs, you will get intellisense for R functions. You will also get compile-time type-checking that the function exists.

Note that you can set the version of RProvider to use (for reproducability) by changing the #r line to:

```fsharp
#r "nuget:RProvider,2.0.3" //replace 2.0.3 with desired version
```

### How to use

RProvider is distributed as a [NuGet package](https://nuget.org/packages/RProvider/), which can be used from an F# script or F# projects. See our [documentation](https://fslab.org/RProvider//) for more detailed information and tutorials.

If you are using R 2.15 or later, you should not try to load the RProvider inside a script that is passed to FSI via the --use flag.  It seems that something about the way R initializes causes it to hang in that context.  Works fine if you load later.

### Developing

Install the requirements listed in the above section. To build and test:

1. Restore dotnet tools: ```dotnet tool restore```
2. Run FAKE: ```dotnet fake build -t All```

To debug, enable logging by setting the RPROVIDER_LOG environment value to an existing text file. 

### License
RProvider is covered by the BSD license.

The library uses [RDotNet](https://github.com/rdotnet/rdotnet) which is also covered by the BSD license.

### Maintainers
* [AndrewIOM](https://github.com/AndrewIOM)
* [dsyme](https://github.com/dsyme)
* [tpetricek](https://github.com/tpetricek)
