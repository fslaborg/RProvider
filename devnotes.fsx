(**
# Developer notes

Binding to R's native C API is not entirely stable and so we do not want to crash the
F# compiler (or Visual Studio / Ionide / FSAC) when something goes wrong with R. For this
reason, we run the type discovery in a separate process and communicate
with it via .NET pipes.

## Project structure

To account for the single-threaded nature of R, we use the following structure.

<div style="margin:40px">
<img src="misc/diagram.png" />
</div>
The individual assemblies are as follows:

* `RProvider.Abstractions`. Contains user-facing root erasable types that
are referenced by both the design-time and runtime parts of the type provider.
  

* `RProvider.dll` - this is runtime assembly. Contains runtime
functionality (such as initialization of R, interop
with R and converters that convert values between F# and R). It also contains
helpers (logging, etc.). This is the assembly that the user of R provider will
reference. It contains the `TypeProviderAssembly` attribute pointing to
the assembly with the actual type provider code.
  

* `RProvider.DesignTime.dll` - this is where the type provider code lives.
This generates types by calling the `RProvider.Server` executable to do the type discovery.
  

* `RProvider.Server` - this is started as a stand-alone process that 
performs type and package discovery in R. It is called by the DesignTime
component and restarted automatically. This also needs to setup the
`AssemblyResolve` event handler. It is compiled as platform-
and architecture-specific executables, so an exe on windows for example.

*)