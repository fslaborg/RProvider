F# R Provider
=======

<img src="https://www.bluemountaincapital.com/media/logo.gif" style="float:right" />

An F# type provider for interoperating with [R](http://www.r-project.org/).

### What does it do?

The R Provider discovers R packages that are available in your R installation and makes them
available as .NET namespaces underneath the parent namespace `RProvider`.  For example, the
stats package is available as `RProvider.stats`.  If you open the namespaces you want to use,
functions and values will be available as R.name.  For example, consider this F# interactive script:

    open RProvider
    open RProvider.``base``

    let v = R.c(1,2,3)

This creates an R numeric vector containing 1,2,3, and names it `v`.  Note that we had to
open the `base` namespace, since the function `c` is part of that namespace.  You should also
open namespace `RProvider`, because it contains some helper functions.

And because type providers are used by the Visual Studio IDE, you will get intellisense for R
functions.  You will also get compile-time type-checking that the function exists.

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

How to use it
-------------

 * There is a lot of info on how to use the provider on our 
   [how to page](howto.html).

 * The [tutorial page](tutorial.html) demonstrates how to use the R provider to 
   analyze financial data using R libraries from F#.

 * For information on what the RProvider is and why you might want 
   to use it, take a look at the [Whats and Whys page](whatwhy.html).

 * For some details of how the RProvider works, check out [How does it work?](internals.html).


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

License
-------

FSharpRProvider is covered by the BSD license. The library uses 
[RDotNet](http://rdotnet.codeplex.com/) which is also covered by the BSD license.

Acknowledgements
----------------

Many thanks to Mathias Brandewinder for producing the [FAKE](https://github.com/fsharp/FAKE) script 
to build the NuGet package, and to Steffen Forkman for writing [FAKE](https://github.com/fsharp/FAKE).











