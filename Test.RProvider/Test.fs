module Test.RProvider

open RProvider
open RProvider.RInterop
open RProvider.``base``
open System
open Xunit
open FsCheck
open Swensen.Unquote.Assertions

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
    if dates.Length > 1 then
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
