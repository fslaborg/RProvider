![logo](https://www.bluemountaincapital.com/media/logo.gif)
F# R Provider
=======
An F# type provider for interoperating with [R](http://www.r-project.org/).

What does it do?
================
The R Provider discovers R packages that are available in your R installation and makes them available as .NET namespaces underneath the parent namespace RProvider.  For example, the stats package is available as RProvider.stats.  If you open the namespaces you want to use, functions and values will be available as R.name.  For example:



License
=======
FSharpRProvider is covered by the BSD license.

The library uses [RDotNet](http://rdotnet.codeplex.com/) which is covered by the LGPL.  Please see LICENSE.md for details.  You can access the source to RDotNet from the [RDotNet CodePlex site](http://rdotnet.codeplex.com/).  As per the terms of LGPL, should you be unable to obtain the source we would be happy to provide a copy.

Pre-requisites
==============
The R Provider requires an installation of R for Windows, downloadable from [here](http://cran.cnr.berkeley.edu/bin/windows/base/).  RProvider uses the R registry key SOFTWARE\R-core to locate the R binary directory, in order to load R.dll.  It will also locate R.dll if it is on the path.  If run from a 32-bit process, RProvider will use the 32-bit R.DLL, and if run from a 64-bit process, it will load the 64-bit version.

 



