namespace RProvider

open System
open System.IO
open System.Reflection
open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices
open RProvider
open RProvider.Internal.Configuration
open RProvider.Internal

[<TypeProvider>]
type public RProvider(cfg: TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces(cfg)

    let useReflectionOnly = false //true

    let runtimeAssembly =
        if useReflectionOnly then
            let coreAssembly = typeof<obj>.Assembly
            let resolver = PathAssemblyResolver([ cfg.RuntimeAssembly; coreAssembly.Location ])
            use mlc = new MetadataLoadContext(resolver, coreAssemblyName = coreAssembly.GetName().Name)
            Logging.logf "Loading runtime assembly %O" mlc
            mlc.LoadFromAssemblyPath cfg.RuntimeAssembly
        else
            Assembly.LoadFrom cfg.RuntimeAssembly

    static do
        // When RProvider is installed via NuGet, the RDotNet assembly and plugins
        // will appear typically in "../../*/lib/net40". To support this, we look at
        // RProvider.dll.config which has this pattern in custom key "ProbingLocations".
        // Here, we resolve assemblies by looking into the specified search paths.
        AppDomain.CurrentDomain.add_AssemblyResolve
            (fun source args ->
                Logging.logf "Assembly resolve: %O" args.Name
                resolveReferencedAssembly args.Name)

    // Generate all the types and log potential errors
    let buildTypes () =
        try
            Logging.logf "Starting build types."

            for ns, types in RTypeBuilder.initAndGenerate (runtimeAssembly) do
                this.AddNamespace(ns, types)

            Logging.logf "RProvider constructor succeeded"
        with
        | e ->
            Logging.logf "RProvider constructor failed: %O" e
            reraise ()

    do buildTypes ()
