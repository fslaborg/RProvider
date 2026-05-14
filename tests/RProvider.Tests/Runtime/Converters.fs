module ConverterTests

open System
open Expecto
open FsCheck
open RProvider.Runtime

let testVector (xs: 'scalar option []) (t: RBridge.SymbolicExpression.SexpType) clsName =
    let sexp = SymbolicExpression.ofObj xs
    Expect.equal (SymbolicExpression.getExprType sexp) t "Type was mutated"
    Expect.equal sexp.Class clsName "R class was not properly set"

    // Vectors round-trip as vectors
    Expect.equal (Some xs) (sexp.TryFromR<'scalar option array>()) "Round-trip returned different value"

    // If a single-element vector, can return as a scalar
    let scalarOpt = sexp.TryFromR<'scalar option>()
    if xs.Length = 1
    then Expect.equal (Some xs.[0]) scalarOpt ""
    else Expect.equal None scalarOpt "Non-scalar vector should not be read as scalar option"

let testScalar (x: 'scalar) (typeof: RBridge.SymbolicExpression.SexpType) clsName =
    let sexp = SymbolicExpression.ofObj x
    Expect.equal (SymbolicExpression.getExprType sexp) typeof "Type was mutated"
    Expect.equal sexp.Class clsName "R class was not properly set"

    // Scalars round-trip to scalar when scalar-type is requested explicitly (scalar option)
    Expect.equal (Some (Some x)) (sexp.TryFromR<'scalar option>()) ""

    // Scalars round-trip as scalar non-option when there are no NA values
    Expect.equal (Some x) (sexp.TryFromR<'scalar>()) "Did not round-trip as true scalars when no NA"

    // Scalars round-trip as vectors of scalar option
    Expect.equal[| Some x |] (sexp.FromR<'scalar option []>()) ""
    Expect.equal[| Some x |] (unbox<'scalar option []> <| sexp.FromR()) "Default obj conversion should be to option array"


/// R stores date-times in seconds, so we cannot
/// expect round-tripping if fractional seconds are passed through.
let normaliseToSeconds (dt: DateTime) =
    let epoch = DateTime(1970,1,1,0,0,0,DateTimeKind.Utc)
    let seconds = (dt.ToUniversalTime() - epoch).TotalSeconds |> int64
    epoch.AddSeconds(float seconds)

let nullToNone (v:string) =
    if isNull v then None else Some (v.Replace("\u0000", ""))

[<Tests>]
let roundTrips =
    testList "Round-trip tests" [

        testProperty "Datetime vector round-trip tests" <| fun (NonEmptyArray (xs: DateTime option [])) ->
            testVector (xs |> Array.map(Option.map normaliseToSeconds)) RBridge.SymbolicExpression.SexpType.RealVector [| "POSIXct"; "POSIXt" |]

        testProperty "Datetime scalar round-trip tests" <| fun (xs: DateTime) ->
            let xs = normaliseToSeconds xs
            testScalar xs RBridge.SymbolicExpression.SexpType.RealVector [| "POSIXct"; "POSIXt" |]

        testProperty "Date vector round-trip tests" <| fun (NonEmptyArray (xs: DateTime option [])) ->
            let xs = xs |> Array.map (Option.map DateOnly.FromDateTime)
            testVector xs RBridge.SymbolicExpression.SexpType.RealVector [| "Date" |]

        testProperty "Date scalar round-trip tests" <| fun (xs: DateTime) ->
            let xs = xs |> DateOnly.FromDateTime
            testScalar xs RBridge.SymbolicExpression.SexpType.RealVector [| "Date" |]

        testProperty "Int vector round-trip tests" <| fun (xs: int option []) ->
            testVector xs RBridge.SymbolicExpression.SexpType.IntegerVector [||]

        testProperty "Int scalar round-trip tests" <| fun (x: int) ->
            testScalar x RBridge.SymbolicExpression.SexpType.IntegerVector [||]

        testProperty "Double vector round-trip tests" <| fun (x: NormalFloat option []) ->
            testVector (x |> Array.map (fun x -> x |> Option.map(fun x -> x.Get))) RBridge.SymbolicExpression.SexpType.RealVector [||]

        testProperty "Double scalar round-trip tests" <| fun (NormalFloat x) ->
            testScalar x RBridge.SymbolicExpression.SexpType.RealVector [||]

        testProperty "Bool vector round-trip tests" <| fun (x: bool option []) ->
            testVector x RBridge.SymbolicExpression.SexpType.LogicalVector [||]

        testProperty "Bool scalar round-trip tests" <| fun (x: bool) ->
            testScalar x RBridge.SymbolicExpression.SexpType.LogicalVector [||]

        testProperty "Complex vector round-trip tests" <| fun (x: (float * float) []) ->
            let xs =
                [| for (r, i) in x do
                    if not (Double.IsNaN(r) || Double.IsNaN(i)) then yield Some (RBridge.Extensions.RComplex.Create (r, i)) |]
            testVector xs RBridge.SymbolicExpression.SexpType.ComplexVector [||]

        testProperty "Complex scalar round-trip tests" <| fun (r: float) (i:float) ->
            if not (Double.IsNaN(r) || Double.IsNaN(i)) then
                let x = RBridge.Extensions.RComplex.Create(r, i)
                testScalar x RBridge.SymbolicExpression.SexpType.ComplexVector [||]

        testProperty "String arrays round-trip" <| fun (NonEmptyArray strings) ->
            let sexp = SymbolicExpression.ofObj strings
            let strings = strings |> Array.map nullToNone
            Expect.equal strings (unbox<string option[]> <| sexp.FromR<obj>()) ""
            Expect.equal strings (sexp.FromR<string option []>()) ""

    ]
