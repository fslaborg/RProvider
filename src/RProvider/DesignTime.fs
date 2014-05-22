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
      
        // Generate all the types and log potential errors
        let buildTypes () =
            try 
              for ns, types in RTypeBuilder.initAndGenerate(runtimeAssembly) do
                this.AddNamespace(ns, types)
            with e ->
              Logging.logf "RProvider constructor failed: %O" e
              reraise()
        do buildTypes ()
