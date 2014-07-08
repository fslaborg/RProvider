namespace RProvider

open System
open System.Collections.Generic
open System.Reflection
open System.IO
open System.Diagnostics
open System.Threading
open Samples.FSharp.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices
open RProvider
open RProvider.Internal
open RInterop
open RInteropInternal
open RInteropClient
open Microsoft.Win32
open System.IO

module internal RemoteRTypeBuilder =
        
    let generateTypes remoteSession (parentType: ProvidedTypeDefinition) =
        withServer <| fun server ->
        Logging.logf "generateTypes for remote R session: getting packages"
        for package in server.GetPackages(remoteSession) do
            let pty = ProvidedTypeDefinition(package, Some(typeof<obj>), HideObjectMethods = true)
            pty.AddXmlDocDelayed <| fun () -> withServer <| fun serverDelayed -> serverDelayed.GetPackageDescription(package, remoteSession)
            pty.AddMembersDelayed(fun () ->
                withServer <| fun serverDelayed ->
                [   let bindings = serverDelayed.GetBindings(package, remoteSession)
                    let titles = lazy serverDelayed.GetFunctionDescriptions(package, remoteSession)
                    for name, rval in Map.toSeq bindings do
                        let memberName = makeSafeName name

                        // Serialize RValue to a string, so that we can include it in the 
                        // compiled quotation (and do not have to get the info again at runtime)
                        let serializedRVal = RInterop.serializeRValue rval

                        match rval with
                        | RValue.Function(paramList, hasVarArgs) ->
                            let paramList =
                                [   for p in paramList ->
                                        ProvidedParameter(makeSafeName p, typeof<obj>, optionalValue=null)

                                    if hasVarArgs then
                                        yield ProvidedParameter("paramArray", typeof<obj[]>, optionalValue=null, isParamArray=true)
                                ]
                            let paramCount = paramList.Length

                            let pm = ProvidedMethod(
                                        methodName = memberName,
                                        parameters = paramList,
                                        returnType = typeof<RemoteSymbolicExpression>,
                                        InvokeCode = fun args ->
                                            if args.Length <> paramCount+1 then // expect arg 0 is RemoteSession
                                                failwithf "Expected %d arguments and received %d" paramCount args.Length
                                            if hasVarArgs then
                                                let namedArgs =
                                                    Array.sub (Array.ofList args) 1 (paramCount-1)
                                                    |> List.ofArray
                                                let namedArgs = Quotations.Expr.NewArray(typeof<obj>, namedArgs)
                                                let varArgs = args.[paramCount]
                                                <@@ ((%%args.[0]:obj) :?> RemoteSession).call package name serializedRVal %%namedArgs %%varArgs @@>
                                            else
                                                let namedArgs =
                                                    Array.sub (Array.ofList args) 1 (args.Length-1)
                                                    |> List.ofArray
                                                let namedArgs = Quotations.Expr.NewArray(typeof<obj>, namedArgs)
                                                <@@ ((%%args.[0]:obj) :?> RemoteSession).call package name serializedRVal %%namedArgs [||] @@>
                                        )
                            pm.AddXmlDocDelayed (
                                fun () ->
                                    match titles.Value.TryFind name with
                                    | Some docs -> docs
                                    | None -> "No documentation available"
                                 )
                            yield pm :> MemberInfo

                            let pdm = ProvidedMethod(
                                        methodName = memberName,
                                        parameters = [ ProvidedParameter("paramsByName", typeof<IDictionary<string,obj>>) ],
                                        returnType = typeof<RemoteSymbolicExpression>,
                                        InvokeCode = fun args ->
                                            if args.Length <> 2 then
                                                failwithf "Expected 2 arguemnts and received %d" args.Length
                                            let argsByName = args.[1]
                                            <@@ let vals = %%argsByName: IDictionary<string,obj>
                                                let valSeq = vals :> seq<KeyValuePair<string,obj>>
                                                ((%%args.[0]:obj) :?> RemoteSession).callFunc package name valSeq null @@>
                                        )
                            yield pdm :> MemberInfo
                        
                        | _ -> 
                            let serializedRVal = RInterop.serializeRValue RValue.Value
                            yield ProvidedProperty(
                                    propertyName = memberName,
                                    propertyType = typeof<RemoteSymbolicExpression>,
                                    GetterCode = fun args -> <@@ ((%%args.[0]:obj) :?> RemoteSession).call package name serializedRVal [||] [||] @@>
                                    ) :> MemberInfo
                ]
                )
            
            parentType.AddMember pty
            let ptyName = pty.Name
            let prop =
                ProvidedProperty(
                    propertyName = pty.Name,
                    propertyType = pty,
                    GetterCode = fun args -> <@@ %%args.[0] @@>
                    )
            parentType.AddMember prop