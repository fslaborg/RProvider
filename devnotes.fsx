(**
// can't yet format YamlFrontmatter (["category: Developer"; "categoryindex: 2"; "index: 3"], Some { StartLine = 1 StartColumn = 0 EndLine = 4 EndColumn = 8 }) to pynb markdown

Developer notes
===============

There are two things that make the R type provider tricky:

 * When you use R provider via NuGet and the F# compiler references the 
   R provider, it attempts to load it from the location where NuGet puts
   it. This is typically `packages/RProvider.1.0.8/lib`. This directory
   does *not* contain `RDotNet.dll` (which is installed in `packages/R.NET.1.3.5/lib/net40`)
   and so the loading could fail.

   To avoid this, we need to make sure that the assembly that is loaded by
   the F# compiler (and Visual Studio) does not trigger loading of R.NET
   immediately - that way, we can setup `AssemblyResolve` event handler
   and load R.NET assembly from another directory.
   
 * Connecting to R is not entirely stable and so we do not want to crash the
   F# compiler (or Visual Studio) when something goes wrong with R. For this 
   reason, we run the type discovery in a separate process and communicate
   with it via .NET remoting.

Project structure
-----------------

To solve the two issues outlined above, the project structure looks like this:

<div style="margin:40px">
<img src="misc/diagram.png" />
</div>

Things to keep in mind
----------------------

Here is what you need to know about individual assemblies in the solution:

 * `RProvider.Runtime.dll` - this is the assembly that contains most of the 
   interesting runtime functionality (such as initialization of R, interop
   with R and converters that convert values between F# and R). It also contains
   helpers (logging, etc.). 
   
   This assembly references R.NET in its public assemblies and so when it
   is loaded, .NET needs to be able to load R.NET (i.e. the `AssemblyResolve`
   event handler needs to be set up).

 * `RProvider.dll` - this is the assembly that the user of R provider will 
   reference. It does not contain any useful code - it only contains 
   `TypeProviderAssembly` attribute pointing to the assembly with the actual
   type provider code. 
   
   Note that we cannot put the functionality from `RProvider.Runtime.dll` 
   here, because the code needs to be referenced by the other two assemblies
   (that are compiled before the type provider can be loaded).

 * `RProvider.DesignTime.dll` - this is where the type provider code lives.
   This sets up `AssemblyResolve` event handler and then it generates types
   (by calling the `RProvider.Server.exe` to do the type discovery).

 * `RProvider.Server.exe` - this is started as a stand-alone process that 
   performs type and package discovery in R. It is called by the DesignTime
   component and restarted automatically. This also needs to setup the 
   `AssemblyResolve` event handler.
*)

