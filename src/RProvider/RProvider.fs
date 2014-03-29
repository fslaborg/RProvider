namespace RProvider

open System
open System.Collections.Generic
open System.Reflection
open System.IO
open System.Diagnostics
open System.Threading
open Samples.FSharp.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices
open RDotNet
open RInterop
open RInterop.RInteropInternal
open RInterop.Logging
open RServerInfo
open Microsoft.Win32
open System.IO

[<TypeProvider>]
type public RProvider(cfg:TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces()

    // Set the R 'R_CStackLimit' variable to -1 when initializing the R engine
    // (the engine is initialized lazily, so the initialization always happens
    // after the static constructor is called - by doing this in the static constructor
    // we make sure that this is *not* set in the normal execution)
    //static do RInit.DisableStackChecking <- true
    let server = GetServer();

    /// Assuming initialization worked correctly, generate the types using R engine
    let generateTypes ns asm =
        // Expose all available packages as namespaces
        logf "generateTypes: getting packages"
        for package in server.GetPackages() do
            let pns = ns + "." + package
            let pty = ProvidedTypeDefinition(asm, pns, "R", Some(typeof<obj>))    

            pty.AddXmlDocDelayed <| fun () -> server.GetPackageDescription package
            pty.AddMembersDelayed( fun () -> 
              [ server.LoadPackage package
                let bindings = server.GetBindings package

                // We get the function descriptions for R the first time they are needed
                let titles = lazy server.GetFunctionDescriptions package

                for name, rval in Map.toSeq bindings do
                    let memberName = server.MakeSafeName name

                    // Serialize RValue to a string, so that we can include it in the 
                    // compiled quotation (and do not have to get the info again at runtime)
                    let serializedRVal = server.SerializeRValue rval

                    match rval with
                    | RInteropInternal.RValue.Function(paramList, hasVarArgs) ->
                        let paramList = [ for p in paramList -> 
                                                ProvidedParameter(makeSafeName p,  typeof<obj>, optionalValue=null)

                                          if hasVarArgs then
                                            yield ProvidedParameter("paramArray", typeof<obj[]>, optionalValue=null, isParamArray=true)
                                        ]
                        
                        let paramCount = paramList.Length
                        
                        let pm = ProvidedMethod(
                                      methodName = memberName,
                                      parameters = paramList,
                                      returnType = typeof<SymbolicExpression>,
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
                                      returnType = typeof<SymbolicExpression>,
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
                                propertyType = typeof<SymbolicExpression>,
                                IsStatic = true,
                                GetterCode = fun _ -> <@@ RInterop.call package name serializedRVal [| |] [| |] @@>) :> MemberInfo  ] )
                      
            this.AddNamespace(pns, [ pty ])
    
    /// Check if R is installed - if no, generate type with properties displaying
    /// the error message, otherwise go ahead and use 'generateTypes'!
    let initAndGenerate () =

        // Get the assembly and namespace used to house the provided types
        Logging.logf "initAndGenerate: starting"
        let asm = System.Reflection.Assembly.GetExecutingAssembly()
        let ns = "RProvider"

        //match RInit.initResult.Value with
        match GetServer().RInitValue with
        | Some error ->
            // add an error static property (shown when typing `R.`)
            let pty = ProvidedTypeDefinition(asm, ns, "R", Some(typeof<obj>))
            let prop = ProvidedProperty("<Error>", typeof<string>, IsStatic = true, GetterCode = fun _ -> <@@ error @@>)
            prop.AddXmlDoc error
            pty.AddMember prop
            this.AddNamespace(ns, [ pty ])
            // add an error namespace (shown when typing `open RProvider.`)
            this.AddNamespace(ns + ".Error: " + error, [ pty ])
        | _ -> 
            generateTypes ns asm        
        Logging.logf "initAndGenerate: finished"


    // Generate all the types and log potential errors
    do  try initAndGenerate() 
        with e ->
          Logging.logf "RProvider constructor failed: %O" e
          reraise()

[<TypeProviderAssembly>]
do()