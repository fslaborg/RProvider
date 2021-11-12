(**
---
category: Documentation
categoryindex: 1
index: 1
---
*)

(*** condition: prepare ***)
#nowarn "211"
#r "../src/RProvider/bin/Release/net5.0/RDotNet.dll"
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
Requirements: Getting Started
=================================

The R type provider can be used on macOS, Windows, and Linux (for supported OS versions,
see the [.NET 5 OS support matrix](https://github.com/dotnet/core/blob/main/release-notes/5.0/5.0-supported-os.md)).

There are three **requirements** to use the R type provider:

- [dotnet SDK](https://www.microsoft.com/net/download/core) 5.0 or greater; and
- [R statistical environment](http://cran.r-project.org/).
- A correctly set `R_HOME` environment variable (see below).
  You **must** set the `R_HOME` environment variable to the R home directory, 
  which can usually be identified by running the command 'R RHOME'. 

Note. If you require .NET framework / mono support, you should use RProvider 1.2 or earlier.
Support for .NET versions below 5.0 was dropped with RProvider 2.0.

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

Third, add your code. In this code, we load RProvider, then load three R packages using
the `open` declarations (graphics, grDevices, and datasets).
*)

open RProvider
open RProvider.graphics
open RProvider.grDevices
open RProvider.datasets
(**
Now we can run some calculations and create charts. When using R on Mac, the default graphics
device (Quartz) sometimes hangs, but X11 is working without issues, so the following uses X11:
*)
// basic test if RProvider works correctly
R.mean([1;2;3;4])
// val it : RDotNet.SymbolicExpression = [1] 2.5


(***do-not-eval***)
// testing graphics
R.x11()

(***do-not-eval***)
// Calculate sin using the R 'sin' function
// (converting results to 'float') and plot it
[ for x in 0.0 .. 0.1 .. 3.14 -> 
    R.sin(x).GetValue<float>() ]
|> R.plot

// Plot the data from the standard 'Nile' data set
R.plot(R.Nile)
(**
Diagnostics and debugging
-------------------------

If you encounter any issues, please do not hesitate to submit an issue! You can do that on the
[GitHub page](https://github.com/fslaborg/RProvider/issues). Before submitting
an issue, please see the [Diagnostics and debugging page](diagnostics.html), which tells you how
to create a log file with more detailed information about the issues.
*)






