(**

*)
#r "nuget: RProvider,{{package-version}}"
(**
# Installing RProvider

The R type provider can be used on macOS, Windows, and Linux (for supported OS versions,
see the [.NET 10 OS support matrix](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0-supported-os.md)).

There are three **requirements** to use the R type provider:

* [dotnet](https://www.microsoft.com/net/download/core) 10.0 or greater; and

* [R](http://cran.r-project.org/) version 4.5.0 or greater.

* A correctly set `R_HOME` environment variable (see below).

*Note. If you require .NET framework / mono support, you should use RProvider 1.2 or earlier.*

## Setting the `R_HOME` environment variable

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

On Windows, R_HOME must point to the *root of the R installation*, not the bin directory.
For example, if R is installed in:

    [lang=cmd]
    C:\Program Files\R\R-4.5.0

from a command prompt use the following command to set the R_HOME permanently as a user environment variable:

    [lang=cmd]
    setx R_HOME "C:\Program Files\R\R-4.5.0"

If R_HOME is not set, RProvider will search the standard installation directory (`"C:\Program Files\R\"`)
and automatically select the newest version matching: `R-<major>.<minor>.<patch>`.

## Testing the R provider

You can now start experimenting with the R type provider using your favourite editor,
or directly from the command line using

    [lang=bash]
    dotnet fsi

The easiest way to get started is to install Visual Studio Code, making sure to also install
the Ionide-fsharp extension within the Extensions tab.

First, create a new file with the extension .fsx (e.g., test.fsx). Second, reference the
R type provider package from NuGet by adding this line to the start of your file:

    [lang=fsharp]
    #r "nuget: RProvider,3.0.0"

Third, add your code. In this code, we load RProvider, then load some R packages using
the `open` declarations.

*)
// Pretty-print in F# interactive:
fsi.AddPrinter FSIPrinters.rValue
(**
Now, a basic test to make sure it's working correctly.

```
Could not find reference ''
```

We can also do some basic plots. First, we can calculate sin
using the R 'sin' function, and then extract the results from
R to F# for plotting.

```
Could not find reference ''
```

However, it would be cleaner to keep the values in R like so,
as R.sin supports vectors:

```
Could not find reference ''
```

Next, we can plot the nile flow using a standard R example dataset.

```
Could not find reference ''
```

## Diagnostics and debugging

If you encounter any issues, please do not hesitate to submit an issue! You can do that on the
[GitHub page](https://github.com/fslaborg/RProvider/issues). Before submitting
an issue, please see the [Diagnostics and debugging page](diagnostics.html), which tells you how
to create a log file with more detailed information about the issues.

*)