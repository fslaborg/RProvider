module Test.RProvider

open RDotNet
open RProvider
open RProvider.RInterop
open RProvider.``base``
open System
open Xunit
open FsCheck
open Swensen.Unquote.Assertions
open System.Text

// Generic function to test that a value round-trips
// when SEXP is asked for the value by-type
let testRoundTrip (x: 'a) (clsName: string) =
    let sexp = toR(x)
    test <@ sexp.Class = [| clsName|] @>
    test <@ x = sexp.GetValue<'a>() @>

// Generic function to test that a value round-trips
// when SEXP is asked for the value by-type, and
// as the default .NET representation
let testRoundTripAndDefault (x: 'a) (clsName: string) =
    testRoundTrip x clsName    
    test <@ let sexp = toR(x)
            x = unbox sexp.Value @>    

(*
let testForType (xs: 'scalarType[]) (clsName: string) =    
    // Test arrays and lists
    testRoundTrip (Array.toList xs) clsName
    testRoundTripAndDefault xs clsName

    // Test scalars
    if xs.Length > 0 then
        let 
    
*)

[<Property>]
// Seems to be an FsCheck in xunit bug with list arguments so I use an array here    
let ``Date lists round-trip``(dates: DateTime[]) = testRoundTrip (Array.toList dates) "Date"

[<Property>]
let ``Date arrays round-trip``(dates: DateTime[]) = testRoundTripAndDefault dates "Date"
    
[<Property>]
let ``Date arrays length <> 1 don't round-trip as dates`` (dates: DateTime[]) =
    if dates.Length <> 1 then
        ignore <| Assert.Throws<InvalidOperationException>(fun () -> toR(dates).GetValue<DateTime>() |> ignore)

[<Property>]
let ``Dates round-trip as vector`` (date: DateTime) =     
    test <@ [|date|] = unbox(toR(date).Value) @>
    test <@ [|date|] = toR(date).GetValue<DateTime[]>() @>

[<Property>]
let ``Dates round-trip (explicit)`` (date: DateTime) = 
    test <@ date = toR(date).GetValue<DateTime>() @>

[<Property>]
let ``Dates have class`` (date: DateTime) = 
    test <@ toR(date).Class = [| "Date" |] @>

//[<Property>]
// Has various issues - embedded nulls, etc.
let ``String arrays round-trip``(strings: string[]) =
    // We only want to test for ASCII strings
    if Array.forall (fun s -> s = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(s))) strings then
        let sexp = toR(strings)

        test <@ strings = unbox sexp.Value @>
        test <@ strings = sexp.GetValue<string[]>() @>

//[<Property>]
// Has various issues - embedded nulls, etc.
let ``Strings round-trip``(value: string) =
    // We only want to test for ASCII strings
    //if Array.forall (fun s -> s = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(s))) strings then
        let sexp = toR(value)    
        test <@ value = sexp.GetValue<string>() @>

let roundTripAsFactor (value:string[]) = 
    let sexp = R.as_factor(value)
    test <@ value = sexp.GetValue<string[]>() @>

let roundTripAsDataframe (value: string[]) = 
    let df = R.data_frame(namedParams [ "Column", value ]).AsDataFrame()    
    test <@ value = df.[0].GetValue<string[]>() @>

[<Fact>]
let ``String arrays round-trip via factors`` () = 
    roundTripAsFactor [| "foo"; "bar"; "foo"; "bar" |]

[<Fact>]
let ``String arrays round-trip via DataFrame`` () = 
    roundTripAsDataframe [| "foo"; "bar"; "foo"; "bar" |]
