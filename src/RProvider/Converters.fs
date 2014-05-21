namespace RProvider.Internal.Converters

open RDotNet
open RProvider
open RInterop
open System.ComponentModel.Composition
open System.Linq

/// Contains higher-level converters
/// [omit]
module Factor = 
    let getLevels sexp = 
        let rvalStr = RInterop.serializeRValue (RValue.Function(["x"], false))
        let symexpr = RInterop.call "base" "levels" rvalStr [| sexp |] [| |]
        symexpr.AsCharacter().ToArray()

    let tryConvert (sexp:SymbolicExpression) = 
        match sexp, sexp.Type, sexp.Class with
        | UntypedVector(nv), Internals.SymbolicExpressionType.IntegerVector, [| "factor" |] ->                
                Some( let levels = getLevels sexp
                      nv.AsInteger() |> Seq.map (fun i -> levels.[i-1]) 
                                     |> Seq.toArray )
        | _ -> None 

    [<Export(typeof<IConvertFromR<string[]>>)>]
    [<Export(typeof<IDefaultConvertFromR>)>]
    type FactorVectorConverter() = 
        interface IConvertFromR<string[]> with
            member this.Convert(sexp: SymbolicExpression) = tryConvert sexp

        // Implementing this interface indicates that if we can convert
        // the value, we are the default converter to use
        interface IDefaultConvertFromR with
            member this.Convert(sexp: SymbolicExpression) = 
                tryConvert sexp |> Option.map box
    
    [<Export(typeof<IConvertFromR<string>>)>]
    type DataFrameConverter() = 
        interface IConvertFromR<string> with
            member this.Convert(sexp: SymbolicExpression) =
                match sexp with
                | IntegerVector(nv) when sexp.Class = [| "factor" |] && nv.Length = 1 ->                    
                        Some <| getLevels(sexp).[nv.[0]]
                | _ -> None 
