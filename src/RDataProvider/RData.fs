namespace ProviderImplementation

open Microsoft.FSharp.Core.CompilerServices
open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Quotations
open RProvider

//module Globals = 
//  let mutable initialized = false

type REnv(fileName:string) =
  let env = RInterop.callFunc "base" "new.env" [] [||] // R.new_env() 
  do RInterop.callFunc "base" "load" (namedParams [ "file", box fileName; "envir", box env ]) [| |] |> ignore
  member x.Environment = env
  member x.Get(name:string) = 
    RInterop.callFunc "base" "get" (namedParams ["x", box name; "envir", box env]) [||]
  
[<TypeProvider>]
type public RDataProvider(cfg:TypeProviderConfig) as this =
  inherit TypeProviderForNamespaces()

  do 
      System.AppDomain.CurrentDomain.add_AssemblyResolve(fun _ e ->
        let asms = System.AppDomain.CurrentDomain.GetAssemblies()
        let loaded = asms |> Seq.tryFind (fun asm -> asm.FullName = e.Name)
        defaultArg loaded null )
(*
        // 

        let name = 
          let comma = e.Name.IndexOf(',')
          (if comma > 0 then e.Name.Substring(0, comma) else e.Name) + ".dll"

        let asmOpt = 
          Seq.concat
            [ cfg.ReferencedAssemblies 
              System.IO.Directory.GetFiles(System.IO.Path.GetDirectoryName(typeof<RDataProvider>.Assembly.Location), "*.dll") ]
          |> Seq.tryFind (fun asm -> asm.EndsWith(name, System.StringComparison.InvariantCultureIgnoreCase))
          |> Option.map System.Reflection.Assembly.LoadFile

        defaultArg asmOpt null )
*)
  // Boilerplate that generates root type in current assembly
  let asm = System.Reflection.Assembly.GetExecutingAssembly()
  let ns = "RProvider"
  let iniType = ProvidedTypeDefinition(asm, ns, "RData", Some(typeof<obj>))
  
  // Add static parameter that specifies the (compile-time) ini file
  let parameter = ProvidedStaticParameter("FileName", typeof<string>)
  do iniType.DefineStaticParameters([parameter], fun typeName args ->

    let fileName = args.[0] :?> string

    let env = REnv(fileName)

    // -------------------------------------------------------------

    let resTy = ProvidedTypeDefinition(asm, ns, typeName, Some typeof<REnv>)

    let ctor = ProvidedConstructor([])
    ctor.InvokeCode <- fun _ -> <@@ REnv(fileName) @@>
    resTy.AddMember(ctor)

    let ctor = ProvidedConstructor([ProvidedParameter("fileName", typeof<string>)])
    ctor.InvokeCode <- fun [fn] -> <@@ REnv(%%fn) @@>
    resTy.AddMember(ctor)

    let ls = RInterop.callFunc "base" "ls" (namedParams ["envir", box env.Environment]) [||]
    for f in ls.GetValue<string[]>()  do
      let v = env.Get(f)
      try 
        let typ = v.Value.GetType()
        ProvidedProperty(f, typ, GetterCode = fun [self] -> Expr.Coerce(<@@ ((%%self):REnv).Get(f).Value @@>, typ))
        |> resTy.AddMember
      with _ -> 
        ProvidedProperty(f, typeof<RDotNet.SymbolicExpression>, GetterCode = fun [self] ->  <@@ ((%%self):REnv).Get(f) @@>)
        |> resTy.AddMember

    resTy) 

  // Register the main (parameterized) type with F# compiler
  do this.AddNamespace(ns, [ iniType ])

[<assembly:TypeProviderAssembly>]
do()