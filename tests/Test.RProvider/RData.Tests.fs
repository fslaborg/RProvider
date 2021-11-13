#if INTERACTIVE
#I "../../bin"
#r "RDotNet.dll"
#r "RProvider.dll"
#r "RProvider.Runtime.dll"
#r "../../packages/xunit/lib/net20/xunit.dll"
#else
module Test.RData
#endif

open Xunit
open System
open RProvider

type Sample = RData<"data/sample.rdata">

[<Fact>]
let ``Can read sample RData file`` () =
    let sample = Sample()
    let sum = sample.volcanoList |> Array.sum
    Assert.Equal<_>(sum, 690907.0)
    Assert.Equal<_>(int sample.volcanoMean.[0], 130)

[<Fact>]
let ``Can save RData file and read it from a temp path`` () =
    let volcanoList = [| 3.0; 1.0 |]
    let volcanoMean = [| 2.0 |]

    let temp = IO.Path.GetTempFileName() + ".rdata"
    R.assign ("volcanoList", volcanoList) |> ignore
    R.assign ("volcanoMean", volcanoMean) |> ignore
    R.save (list = [ "volcanoList"; "volcanoMean" ], file = temp) |> ignore

    let sample = Sample(temp)
    Assert.Equal<_>(sample.volcanoList |> Array.sum, 4.0)
    Assert.Equal<_>(int sample.volcanoMean.[0], 2)
