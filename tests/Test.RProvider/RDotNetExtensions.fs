module Test.RDotNetExtensions

open RProvider
open RProvider.datasets
open RProvider.methods
open Xunit
open FsCheck.Xunit
open RProvider.Operators

[<Fact>]
let ``Can get member of an S3 object when it exists`` () =
    let col = R.mtcars.Member("mpg")
    Assert.True(col.IsVector())

[<Fact>]
let ``Can access S3 member using dynamic operator`` () =
    let col = R.mtcars?mpg
    Assert.True(col.IsVector())

[<Fact>]
let ``Throws when S3 member does not exist`` () =
    Assert.Throws<System.ArgumentOutOfRangeException>(fun () -> R.mtcars?somerandomvectorname |> ignore)

[<Fact>]
let ``Can get slot of an S4 object using dynamic operator`` () =
    R.setClass <| namedParams [
        "Class", box "Person"
        "slots", box (R.c (namedParams [ "name", "string"; "age", "numeric"]))
    ]
    let person = R.``new`` <| namedParams ["Class", "Person"]
    Assert.True(person?age.AsNumeric().Length = 0)
