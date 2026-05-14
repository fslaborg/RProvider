module GraphicsTests

open Expecto
open RProvider

[<Tests>]
let graphics =
    testList "Making graphics" [

        testCase "Can plot to in-memory svg" <| fun _ ->
            let mkSvg () = R.plot [ 1. .. 10. ]
            let txt = Graphics.svg 100 100 mkSvg
            Expect.stringStarts txt "<?xml" "Was not XML returned"

    ]