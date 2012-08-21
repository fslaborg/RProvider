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

[<Property>]
let ``Date arrays round-trip``(dates: DateTime[]) =        
    let ds = toR(dates)        
    
    test <@ dates = unbox ds.Value @>
    test <@ dates = ds.GetValue<DateTime[]>() @>

[<Property>]
let ``Date arrays have class``(dates: DateTime[]) =    
    test <@ toR(dates).Class = [| "Date" |] @>  

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
