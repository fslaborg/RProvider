namespace RProvider

open System
open System.IO
open System.Reflection
open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices
open RProvider
open RProvider.Internal.Configuration
open RProvider.Internal
open Microsoft.FSharp.Quotations
  
[<TypeProvider>]
type public RDataProvider(cfg:TypeProviderConfig) as this =
  inherit TypeProviderForNamespaces()

  // NOTE: No need to register 'AssemblyResolve' event handler
  // here, because this is already done in static constructor of RProvider.

  /// Helper for writing InvokeCode
  let (|Singleton|) = function [v] -> v | _ -> failwith "Expected one argument."

  /// Given a file name, generate static type inherited from REnv
  let generateTypes asm typeName (args:obj[]) =

    // Load the environment and generate the type
    let fileName = args.[0] :?> string
    let longFileName = 
      if Path.IsPathRooted(fileName) then fileName
      else Path.Combine(cfg.ResolutionFolder, fileName)
    let resTy = ProvidedTypeDefinition(asm, "RProvider", typeName, Some typeof<REnv>)
    let isHosted = cfg.IsHostedExecution
    let defaultResolutionFolder = cfg.ResolutionFolder

    // Provide default ctor and ctor taking another file as an argument
    let createREnvExpr fileName =
      <@@ let longFileName =
            if Path.IsPathRooted(%fileName) then %fileName
            elif isHosted then Path.Combine(defaultResolutionFolder, %fileName)
            else Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, %fileName) 
          REnv(longFileName) @@>

    let ctor = ProvidedConstructor([])
    ctor.InvokeCode <- fun _ -> createREnvExpr <@ fileName @>
    resTy.AddMember(ctor)

    let ctor = ProvidedConstructor([ProvidedParameter("fileName", typeof<string>)])
    ctor.InvokeCode <- fun (Singleton fn) -> createREnvExpr (Expr.Cast fn)
    resTy.AddMember(ctor)

    // For each key in the environment, provide a property..
    for name, typ in RInteropClient.GetServer().GetRDataSymbols(longFileName) do
      match typ with 
      | Some typ ->
          // If there is a default convertor for the type, then generate
          // property of the statically known type (e.g. Frame<string, string>)
          // (otherwise, `Value` will throw)
          ProvidedProperty(name, typ, GetterCode = fun (Singleton self) -> 
              Expr.Coerce(<@@ ((%%self):REnv).Get(name).Value @@>, typ))
          |> resTy.AddMember
      | None ->
          // Generate property of type 'SymbolicExpression'
          ProvidedProperty(name, typeof<RDotNet.SymbolicExpression>, GetterCode = fun (Singleton self) ->  
              <@@ ((%%self):REnv).Get(name) @@>)
          |> resTy.AddMember

    resTy

  // Register the main (parameterized) type with F# compiler
  // Provide tye 'RProvider.RData<FileName>' type
  let asm = Assembly.ReflectionOnlyLoadFrom cfg.RuntimeAssembly
  let rdata = ProvidedTypeDefinition(asm, "RProvider", "RData", Some(typeof<obj>))
  let parameter = ProvidedStaticParameter("FileName", typeof<string>)
  do rdata.DefineStaticParameters([parameter], generateTypes asm)
  do this.AddNamespace("RProvider", [ rdata ])
