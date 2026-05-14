#r "nuget: RProvider, 0.0.1-local"

open RProvider
open RProvider.terra
open RProvider.broom

/// Metres above sea level
[<Measure>] type masl

fsi.AddPrinter FSIPrinters.rValue

/// Load Arctic DEM for Bear Island.
let loadDem () =
    let dem = R.rast "/Users/andrewmartin/Tresors/Research Projects/Bear Island/bear-island-arcticdem.tif"
    let coords = R.crds dem
    let mat = R.as_matrix dem
    let matF =
        mat.FromR<float[,]>()
        |> Array2D.map ((*) 1.<masl>)

    // let coordsF = coords.GetValue<float[,]>()
    // coordsF
    // |> Array2D.map
    matF

    mat.Print()


    dem |> RExpr.slots



open RProvider.stats

let x =
    [| 1 .. 10 |]
    |> Array.Parallel.map(fun i ->
        (R.sin i).FromR<float>()
        )

let y =
    [| 1 .. 10 |]
    |> Array.map(fun i ->
        (R.sin i).FromR<float>()
        )


// this works
let x2 = [| 0..10 |] |> Array.map (fun i -> R.sin(i).Print())

// this fails
let y2 = [| 0..10 |] |> Array.Parallel.map (fun i -> R.sin(i).Print() )

R.t_test()