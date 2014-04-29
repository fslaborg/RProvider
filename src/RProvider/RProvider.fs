namespace RProvider

open System
open System.IO
open System.Reflection
open Samples.FSharp.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices
open RProvider
open RProvider.Internal.Configuration
open RProvider.Internal

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

[<TypeProvider>]
type public RProviderRemote(cfg:TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces()

    let useReflectionOnly = true

    let runtimeAssembly =
        if useReflectionOnly then Assembly.ReflectionOnlyLoadFrom cfg.RuntimeAssembly
        else Assembly.LoadFrom cfg.RuntimeAssembly

    let ns = "RProvider"
    let baseType = typeof<obj>
    let staticParams =
        [   ProvidedStaticParameter("host", typeof<string>)
            ProvidedStaticParameter("port", typeof<int>)
            ProvidedStaticParameter("blocking", typeof<bool>)
        ]

    let remoteRType = ProvidedTypeDefinition(runtimeAssembly, ns, "RemoteR", Some baseType)

    do remoteRType.DefineStaticParameters(
        parameters=staticParams,
        instantiationFunction=(fun typeName parameterValues ->
            let host = parameterValues.[0] :?> string
            let port = parameterValues.[1] :?> int
            let blocking = parameterValues.[2] :?> bool
            let remoteConfig = new SessionConfig(host, port, blocking)
            let sessionType =
                ProvidedTypeDefinition(
                    runtimeAssembly,
                    ns,
                    typeName,
                    baseType = Some baseType
                    )
            sessionType.AddXmlDoc <| sprintf
                "A strongly typed interface to the R session hosted at %s, port %d through svSocket"
                remoteConfig.hostName
                remoteConfig.port
            RemoteRTypeBuilder.generateTypes remoteConfig sessionType

            let ctor =
                ProvidedConstructor(
                    parameters = [],
                    InvokeCode = fun args -> <@@ RemoteSession.GetConnection(host, port, blocking) :> obj @@>
                    )
            ctor.AddXmlDoc "Initialize a connected R session hosted through svSocket."
            sessionType.AddMember ctor

            let sessionEvalToHandle = 
                ProvidedMethod(
                    methodName = "evalToHandle",
                    parameters = [ ProvidedParameter("expr",  typeof<string>) ],
                    returnType = typeof<RemoteSymbolicExpression>,
                    InvokeCode = fun args -> if args.Length <> 2 then
                                                failwithf "Expected 2 argument and received %d" args.Length
                                                <@@ ((%%args.[0]:obj) :?> RemoteSession).evalToHandle %%args.[1] @@>
                    )
            sessionType.AddMember sessionEvalToHandle

            let sessionEvalToSymbolicExpression = 
                ProvidedMethod(
                    methodName = "eval",
                    parameters = [ ProvidedParameter("expr",  typeof<string>) ],
                    returnType = typeof<RDotNet.SymbolicExpression>,
                    InvokeCode = fun args -> if args.Length <> 2 then
                                                failwithf "Expected 2 argument and received %d" args.Length
                                                <@@ ((%%args.[0]:obj) :?> RemoteSession).evalToSymbolicExpression %%args.[1] @@>
                    )
            sessionType.AddMember sessionEvalToSymbolicExpression

            let sessionAssign = 
                ProvidedMethod(
                    methodName = "assign",
                    parameters = [ ProvidedParameter("name",  typeof<string>); ProvidedParameter("value", typeof<obj>) ],
                    returnType = typeof<unit>,
                    InvokeCode = fun args -> if args.Length <> 3 then
                                                failwithf "Expected 3 argument and received %d" args.Length
                                                <@@ ((%%args.[0]:obj) :?> RemoteSession).assign %%args.[1] %%args.[2] @@>
                    )
            sessionType.AddMember sessionAssign

            let sessionGet = 
                ProvidedMethod(
                    methodName = "get",
                    parameters = [ ProvidedParameter("name",  typeof<string>) ],
                    returnType = typeof<RDotNet.SymbolicExpression>,
                    InvokeCode = fun args -> if args.Length <> 2 then
                                                failwithf "Expected 2 argument and received %d" args.Length
                                                <@@ ((%%args.[0]:obj) :?> RemoteSession).getRemoteSymbol %%args.[1] @@>
                    )
            sessionType.AddMember sessionGet

            let sessionFinalize =
                ProvidedMethod(
                    methodName = "Finalize",
                    parameters = [],
                    returnType = typeof<unit>,
                    InvokeCode = fun args -> <@@ ((%%args.[0]:obj) :?> RemoteSession).close() @@>
                    )
            sessionType.DefineMethodOverride(sessionFinalize, sessionType.GetMethod "Finalize")

            let sessionRemoteSessionProperty = 
                ProvidedProperty(
                    propertyName = "_interopSession",
                    propertyType = typeof<RemoteSession>,
                    GetterCode = fun args -> <@@ (%%args.[0]:obj) :?> RemoteSession @@>
                    )
            sessionType.AddMember sessionRemoteSessionProperty

            sessionType
        ))

    do this.AddNamespace(ns, [remoteRType])
