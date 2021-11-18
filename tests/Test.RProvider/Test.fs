#if INTERACTIVE
#I "../../bin"
#I "../../packages/xunit/lib/net20/"
#r "RDotNet.dll"
#r "RProvider.dll"
#r "RProvider.Runtime.dll"
#r "../../packages/FsCheck/lib/net40-Client/FsCheck.dll"
#r "../../packages/FsCheck.Xunit/lib/net40-Client/FsCheck.Xunit.dll"
#r "xunit.dll"
#else
module Test.RProvider
#endif

open RDotNet
open RDotNet.Internals
open RProvider
open RProvider.RInterop
open System
open Xunit
open FsCheck.Xunit
open System.Numerics
open System.Text

[<assembly: CollectionBehavior(DisableTestParallelization = true)>]
do ()

// Generic function to test that a value round-trips
// when SEXP is asked for the value by-type
let testRoundTrip (x: 'a) (typeof: SymbolicExpressionType) (clsName: Option<string>) =
    let sexp = toR (x)
    Assert.Equal<'a>(x, sexp.GetValue<'a>())
    Assert.Equal(sexp.Type, typeof)
    Assert.Equal<string []>(sexp.Class, Option.toArray clsName)

// Generic function to test that a value round-trips
// when SEXP is asked for the value by-type, and
// as the default .NET representation
let testRoundTripAndDefault (x: 'a) (typeof: SymbolicExpressionType) (clsName: Option<string>) =
    testRoundTrip x typeof clsName
    let sexp = toR (x)
    Assert.Equal<'a>(x, unbox<'a> sexp.Value)

let testVector (xs: 'scalarType []) (typeof: SymbolicExpressionType) (clsName: Option<string>) =
    // Test arrays and lists round-trip
    testRoundTrip (Array.toList xs) typeof clsName
    // Array is the default return type from .Value
    testRoundTripAndDefault xs typeof clsName
    // Can only round-trip a vector as a scalar if it is of length 1
    if xs.Length <> 1 then
        ignore <| Assert.Throws<InvalidOperationException>(fun () -> toR(xs).GetValue<'scalarType>() |> ignore)


let testScalar (x: 'scalarType) (typeof: SymbolicExpressionType) (clsName: Option<string>) =
    // Scalars round-trip to scalar when scalar-type is requested explicitly
    testRoundTrip x typeof clsName

    // Scalars round-trip as vectors
    let sexp = toR (x)
    Assert.Equal<'scalarType []>([| x |], unbox (sexp.Value))
    Assert.Equal<'scalarType []>([| x |], sexp.GetValue<'scalarType []>())

[<Property>]
let ``Date vector round-trip tests`` (xs: DateTime []) =
    testVector xs SymbolicExpressionType.NumericVector (Some "Date")

[<Property>]
let ``Date scalar round-trip tests`` (x: DateTime) = testScalar x SymbolicExpressionType.NumericVector (Some "Date")

[<Property>]
let ``Int vector round-trip tests`` (xs: int []) = testVector xs SymbolicExpressionType.IntegerVector None

[<Property>]
let ``Int scalar round-trip tests`` (x: int) = testScalar x SymbolicExpressionType.IntegerVector None

[<Property>]
let ``Double vector round-trip tests`` (xs: double []) = testVector xs SymbolicExpressionType.NumericVector None

[<Property>]
let ``Double scalar round-trip tests`` (x: double) = testScalar x SymbolicExpressionType.NumericVector None

[<Property>]
let ``Bool vector round-trip tests`` (xs: bool []) = testVector xs SymbolicExpressionType.LogicalVector None

[<Property>]
let ``Bool scalar round-trip tests`` (x: bool) = testScalar x SymbolicExpressionType.LogicalVector None

[<Property>]
let ``Complex vector round-trip tests`` (ris: (double * double) []) =
    let xs =
        [| for (r, i) in ris do
               if not (Double.IsNaN(r) || Double.IsNaN(i)) then yield Complex(r, i) |]

    testVector xs SymbolicExpressionType.ComplexVector None

[<Property>]
let ``Complex scalar round-trip tests`` (r: double) (i: double) =
    if not (Double.IsNaN(r) || Double.IsNaN(i)) then
        let x = Complex(r, i)
        testScalar x SymbolicExpressionType.ComplexVector None

[<Fact>]
let ``Printing of data frame returns string with frame data`` () =
    let df = namedParams [ "Test", box [| 1; 42; 2 |] ] |> R.data_frame
    Assert.Contains("42", df.Print())

[<Property>]
let ``Serialization of R values works`` (isValue: bool) (args: string []) (hasVar: bool) =
    let args = List.ofSeq args

    if args |> Seq.forall (fun a -> not (isNull a) && not (a.Contains(";"))) then
        let rvalue = if isValue then RValue.Value else RValue.Function(args, hasVar)
        let actual = deserializeRValue (serializeRValue (rvalue))
        Assert.Equal(rvalue, actual)

//[<Property>]
// Has various issues - embedded nulls, etc.
let ``String arrays round-trip`` (strings: string []) =
    // We only want to test for ASCII strings
    let ascii = ASCIIEncoding()

    if Array.forall (fun (s: string) -> s = (s |> ascii.GetBytes |> ascii.GetString)) strings then
        let sexp = toR (strings)

        Assert.Equal<string []>(strings, unbox sexp.Value)
        Assert.Equal<string []>(strings, sexp.GetValue<string []>())

//[<Property>]
// Has various issues - embedded nulls, etc.
let ``Strings round-trip`` (value: string) =
    // We only want to test for ASCII strings
    //if Array.forall (fun s -> s = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(s))) strings then
    let sexp = toR (value)
    Assert.Equal<string>(value, sexp.GetValue<string>())

let roundTripAsFactor (value: string []) =
    let sexp = R.as_factor (value)
    Assert.Equal<string []>(value, sexp.GetValue<string []>())

let roundTripAsDataframe (value: string []) =
    let df = R.data_frame(namedParams [ "Column", value ]).AsDataFrame()
    Assert.Equal<string []>(value, df.[0].GetValue<string []>())

[<Fact>]
let ``String arrays round-trip via factors`` () = roundTripAsFactor [| "foo"; "bar"; "foo"; "bar" |]

[<Fact>]
let ``String arrays round-trip via DataFrame`` () = roundTripAsDataframe [| "foo"; "bar"; "foo"; "bar" |]
