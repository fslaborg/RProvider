(**
---
title: Installing
category: Getting Started
categoryindex: 2
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
Installing RProvider
=================================

The R type provider can be used on macOS, Windows, and Linux (for supported OS versions,
see the [.NET 10 OS support matrix](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0-supported-os.md)).

There are three **requirements** to use the R type provider:

- [dotnet](https://www.microsoft.com/net/download/core) 10.0 or greater; and
- [R](http://cran.r-project.org/) version 4.5.0 or greater.
- A correctly set `R_HOME` environment variable (see below).

*Note. If you require .NET framework / mono support, you should use RProvider 1.2 or earlier.*

Setting the `R_HOME` environment variable
----------------------
The R type provider requires that the R_HOME environment variable is set, so that
it can find the R installation that you wish to use.

#### macOS
In a Terminal window, execute the following command to add the R_HOME environment
variable permanently:

    [lang=bash]
    echo export R_HOME=$(R RHOME) >> ~/.zshenv

#### Linux

You can set the R_HOME environment variable in your current session
using the command:

    [lang=bash]
    export R_HOME=$(R RHOME)

#### Windows

On Windows, from a command prompt use the following command to set
the R_HOME permanently as a user environment variable, replacing C:\rpath\bin
with your R install location:

    [lang=cmd]
    setx R_HOME "C:\rpath\bin"

Testing the R provider
----------------------

You can now start experimenting with the R type provider using your favourite editor,
or directly from the command line using

    [lang=bash]
    dotnet fsi

The easiest way to get started is to install Visual Studio Code, making sure to also install
the Ionide-fsharp extension within the Extensions tab.

First, create a new file with the extension .fsx (e.g., test.fsx). Second, reference the
R type provider package from NuGet by adding this line to the start of your file:

    [lang=fsharp]
    #r "nuget: RProvider,2.0.2"

Third, add your code. In this code, we load RProvider, then load some R packages using
the `open` declarations.
*)

open RProvider
open RProvider.datasets

// basic test if RProvider works correctly
R.mean([1;2;3;4]).Print()
(*** include-it ***)


(***do-not-eval***)
// Calculate sin using the R 'sin' function
// (converting results to 'float') and plot it
[ for x in 0.0 .. 0.1 .. 3.14 -> 
    R.sin(x).FromR<float>() ]
|> R.plot

// Plot the data from the standard 'Nile' data set
(***do-not-eval***)
R.plot(R.Nile)

(**
Diagnostics and debugging
-------------------------

If you encounter any issues, please do not hesitate to submit an issue! You can do that on the
[GitHub page](https://github.com/fslaborg/RProvider/issues). Before submitting
an issue, please see the [Diagnostics and debugging page](diagnostics.html), which tells you how
to create a log file with more detailed information about the issues.
*)






