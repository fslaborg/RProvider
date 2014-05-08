namespace RProvider

open System
open System.IO
open System.Reflection
open Samples.FSharp.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices
open RProvider
open RProvider.Configuration
open RProvider.Internal

module DesignTime =

    [<TypeProvider>]
    type public RProvider(cfg:TypeProviderConfig) as this =
        inherit TypeProviderForNamespaces()

        let useReflectionOnly = true

        let runtimeAssembly =
            if useReflectionOnly then Assembly.ReflectionOnlyLoadFrom cfg.RuntimeAssembly
            else Assembly.LoadFrom cfg.RuntimeAssembly

        static do 
          // When RProvider is installed via NuGet, the RDotNet assembly and plugins
          // will appear typically in "../../*/lib/net40". To support this, we look at
          // RProvider.dll.config which has this pattern in custom key "ProbingLocations".
          // Here, we resolve assemblies by looking into the specified search paths.
          AppDomain.CurrentDomain.add_AssemblyResolve(fun source args ->
            resolveReferencedAssembly args.Name)
      
          // Set the R 'R_CStackLimit' variable to -1 when initializing the R engine
          // (the engine is initialized lazily, so the initialization always happens
          // after the static constructor is called - by doing this in the static constructor
          // we make sure that this is *not* set in the normal execution)
          RInit.DisableStackChecking <- true

        // Generate all the types and log potential errors
        let buildTypes () =
            try 
              for ns, types in RTypeBuilder.initAndGenerate(runtimeAssembly) do
                this.AddNamespace(ns, types)
            with e ->
              Logging.logf "RProvider constructor failed: %O" e
              reraise()
        do buildTypes ()
