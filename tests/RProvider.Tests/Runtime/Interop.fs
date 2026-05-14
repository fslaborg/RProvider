module InteropTests

open Expecto
open RProvider.Runtime

[<Tests>]
let interop =
    testList "R interop API surface" [

        testCase "Listing packages shows installed packages" <| fun _ ->
            let packages = RInterop.getPackages ()
            Expect.isNonEmpty packages "No packages were listed"
            Expect.contains packages "base" "Did not list 'base' in tests"
            Expect.contains packages "zoo" "Did not list 'zoo' in tests"

        testCase "Listing packages is consistent" <| fun _ ->
            let p1 = RInterop.getPackages ()
            let p2 = RInterop.getPackages ()
            Expect.equal p1 p2 "Packages changed between calls"

        testCase "Get package description works for 'base'" <| fun _ ->
            let desc = RInterop.getPackageDescription "base"
            printfn "%s" desc
            Expect.isNotEmpty desc "Base package description was empty"
            Expect.equal desc "Base R functions." "Base description was not as expected"

        testCase "Get package description is placeholder for non-existent package" <| fun _ ->
            let desc = RInterop.getPackageDescription "made-up-package"
            Expect.equal desc "[Could not get package description from R]" "Expected empty description for non-existent package"

        testCase "Function description for 'base'" <| fun _ ->
            let functions = RInterop.getFunctionDescriptions "base"
            let asFunction = functions |> Array.tryFind(fun f -> fst f = "as.function") |> Option.map snd
            Expect.isNonEmpty functions "Base package description was empty"
            Expect.equal asFunction (Some "Convert Object to Function") "as.function description was wrong"
    
        testCase "Can load the 'zoo' package" <| fun _ ->
            RInterop.loadPackage "zoo"

        testCase "Loading a package twice has no effect" <| fun _ ->
            RInterop.loadPackage "zoo"
            RInterop.loadPackage "zoo"

        testCase "Throws for loading non-existent package" <| fun _ ->
            Expect.throws (fun _ ->
                RInterop.loadPackage "made-up-package")
                "Did not throw despite package not existing"

        // TODO Strange DesignTime reference error:
        // testCase "getBindings returns known symbol from base" <| fun _ ->
        //     let bindings = RInterop.getBindings "base"
        //     let names = bindings |> Array.map fst
        //     Expect.contains names "sum" "sum not found in base bindings"

        testCase "Can call functions with unnamed arguments ('sum' from base)" <| fun _ ->
            let globEnv = RInterop.globalEnvironment()
            let result =
                RInterop.callFuncByName globEnv "base" "sum" Seq.empty [| [| 1; 2; 3 |] |]
            let value = result.FromR<int> ()
            Expect.equal value 6 "sum(1,2,3) should be 6"

        testCase "Can call functions with named arguments ('sum' from base)" <| fun _ ->
            let globEnv = RInterop.globalEnvironment()
            let args =
                [ "x", box [| 1.; 2.; 3.; nan |]
                  "na.rm", box true ] |> Map.ofList
            let result = RInterop.callFuncByName globEnv "base" "sum" args [||]
            let value = result.FromR<float> ()
            Expect.equal value 6. "sum(1,2,3) should be 6"

    ]