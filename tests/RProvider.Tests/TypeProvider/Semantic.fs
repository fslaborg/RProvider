module SemanticTests

open Expecto
open RProvider
open RProvider.datasets

[<Tests>]
let interop =
    testList "Semantic types - lists" [

        testCase "Numeric lists can get list items" <| fun _ ->
            let l = R.c [1; 2; 3]
            Expect.equal l.Type Runtime.RTypes.VectorType "Was not a vector"
            Expect.isSome l.TryAsTyped "Could not get semantic view of a vector"

        testCase "Can get typed view of a data frame" <| fun _ ->
            Expect.equal R.mtcars.Type Runtime.RTypes.DataFrameType "mtcars was not a data frame"
            Expect.isSome R.mtcars.TryAsTyped "Could not get semantic view of a data frame"

            let df = R.mtcars.AsDataFrame()
            Expect.contains df.Names (Some "mpg") "Did not infer columns correctly"

            let mpg = df.Column "mpg"
            Expect.isTrue mpg.IsNumericColumn "mpg was not numeric"
    ]