namespace RProvider


open System
open System.Collections.Generic
open System.Reflection
open System.IO
open System.Diagnostics
open System.Threading
open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices
open RProvider
open RProvider.Internal
open RInterop
open RInteropInternal
open RInteropClient
open Microsoft.Win32
open System.IO

module internal RTypeBuilder =
    
    /// Assuming initialization worked correctly, generate the types using R engine
    let generateTypes ns asm = withServer <| fun server ->
      [ // Expose all available packages as namespaces
        Logging.logf "generateTypes: getting packages"
        for package in server.GetPackages() do
            let pns = ns + "." + package
            let pty = ProvidedTypeDefinition(asm, pns, "R", Some(typeof<obj>))    

            pty.AddXmlDocDelayed <| fun () -> withServer <| fun serverDelayed -> serverDelayed.GetPackageDescription package
            pty.AddMembersDelayed( fun () -> 
              withServer <| fun serverDelayed ->
              [ serverDelayed.LoadPackage package
                let bindings = serverDelayed.GetBindings package

                // We get the function descriptions for R the first time they are needed
                let titles = lazy withServer (fun s -> s.GetFunctionDescriptions package)

                for name, rval in Map.toSeq bindings do
                    let memberName = makeSafeName name

                    // Serialize RValue to a string, so that we can include it in the 
                    // compiled quotation (and do not have to get the info again at runtime)
                    let serializedRVal = RInterop.serializeRValue rval

                    match rval with
                    | RValue.Function(paramList, hasVarArgs) ->
                        let paramList = [ for p in paramList -> 
                                                ProvidedParameter(makeSafeName p,  typeof<obj>, optionalValue=null)

                                          if hasVarArgs then
                                            yield ProvidedParameter("paramArray", typeof<obj[]>, optionalValue=null, IsParamArray=true)
                                        ]
                        
                        let paramCount = paramList.Length
                        
                        let pm = ProvidedMethod(
                                      methodName = memberName,
                                      parameters = paramList,
                                      returnType = typeof<RDotNet.SymbolicExpression>,
                                      IsStaticMethod = true,
                                      InvokeCode = fun args -> if args.Length <> paramCount then
                                                                 failwithf "Expected %d arguments and received %d" paramCount args.Length
                                                               if hasVarArgs then
                                                                 let namedArgs = 
                                                                     Array.sub (Array.ofList args) 0 (paramCount-1)
                                                                     |> List.ofArray
                                                                 let namedArgs = Quotations.Expr.NewArray(typeof<obj>, namedArgs)
                                                                 let varArgs = args.[paramCount-1]
                                                                 <@@ RInterop.call package name serializedRVal %%namedArgs %%varArgs @@>                                                 
                                                               else
                                                                 let namedArgs = Quotations.Expr.NewArray(typeof<obj>, args)                                            
                                                                 <@@ RInterop.call package name serializedRVal %%namedArgs [||] @@> )

                        pm.AddXmlDocDelayed (fun () -> match titles.Value.TryFind name with 
                                                        | Some docs -> docs 
                                                        | None -> "No documentation available")                                    
                        
                        yield pm :> MemberInfo

                        // Yield an additional overload that takes a Dictionary<string, object>
                        // This variant is more flexible for constructing lists, data frames etc.
                        let pdm = ProvidedMethod(
                                      methodName = memberName,
                                      parameters = [ ProvidedParameter("paramsByName",  typeof<IDictionary<string,obj>>) ],
                                      returnType = typeof<RDotNet.SymbolicExpression>,
                                      IsStaticMethod = true,
                                      InvokeCode = fun args -> if args.Length <> 1 then
                                                                 failwithf "Expected 1 argument and received %d" args.Length
                                                               let argsByName = args.[0]
                                                               <@@  let vals = %%argsByName: IDictionary<string,obj>
                                                                    let valSeq = vals :> seq<KeyValuePair<string, obj>>
                                                                    RInterop.callFunc package name valSeq null @@> )
                        yield pdm :> MemberInfo                                    
                    | RValue.Value ->
                        yield ProvidedProperty(
                                propertyName = memberName,
                                propertyType = typeof<RDotNet.SymbolicExpression>,
                                IsStatic = true,
                                GetterCode = fun _ -> <@@ RInterop.call package name serializedRVal [| |] [| |] @@>) :> MemberInfo  ] )
                      
            yield pns, [ pty ] ]
    
    /// Check if R is installed - if no, generate type with properties displaying
    /// the error message, otherwise go ahead and use 'generateTypes'!
    let initAndGenerate providerAssembly = 
       [  // Get the assembly and namespace used to house the provided types
          Logging.logf "initAndGenerate: starting"
          let ns = "RProvider"

          match tryGetInitializationError() with
          | Some error ->
              // add an error static property (shown when typing `R.`)
              let pty = ProvidedTypeDefinition(providerAssembly, ns, "R", Some(typeof<obj>))
              let prop = ProvidedProperty("<Error>", typeof<string>, IsStatic = true, GetterCode = fun _ -> <@@ error @@>)
              prop.AddXmlDoc error
              pty.AddMember prop
              yield ns, [ pty ]
              // add an error namespace (shown when typing `open RProvider.`)
              yield ns + ".Error: " + error, [ pty ]
          | _ -> 
              yield! generateTypes ns providerAssembly
          Logging.logf "initAndGenerate: finished" ]