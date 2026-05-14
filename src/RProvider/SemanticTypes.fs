namespace RProvider.Runtime

open RBridge
open RBridge.Extensions

/// Convert between user-facing RExpr and the internal
/// RBridge symbolic expression type.
module internal RExprWrapper =

    open RProvider.Abstractions

    let toRBridge (ex: RExpr) : RBridge.SymbolicExpression = { ptr = (RExpr.unwrap ex).ptr }

    let toRProvider (ex: RBridge.SymbolicExpression) : RExpr = RExpr.wrap { ptr = ex.ptr }

/// Types representing common R types, and
/// functions for working with them.
module RTypes =

    /// Represents user-facing types of expressions in R.
    /// Expressions are labelled with these types, for example
    /// in FSI output.
    type RSemanticType =
        | ScalarType
        | VectorType
        | ListType
        | FactorType
        | MatrixType
        | ArrayType
        | DataFrameType
        | FunctionType
        | EnvironmentType
        | S3ObjectType
        | S4ObjectType
        | R6ObjectType

    let private scalarOrVector (engine:RInterop.RInstance) sexp =
        match engine.invokeInt(fun e -> SymbolicExpression.length e sexp) with
        | 1 -> ScalarType
        | _ -> VectorType

    /// Classify a symbolic expression into one of the semantic
    /// types provided by RProvider.
    let internal classify engine sexp =
        match sexp with
        | ActivePatterns.S4Object engine _ -> S4ObjectType
        | ActivePatterns.DataFrame engine _ -> DataFrameType
        | ActivePatterns.Factor engine _ -> FactorType
        | _ when Extract.getDimension engine sexp = 2 -> MatrixType
        | _ when Extract.getDimension engine sexp > 2 -> ArrayType
        | ActivePatterns.S3Object engine _ -> S3ObjectType
        | ActivePatterns.RealVector engine _
        | ActivePatterns.ComplexVector engine _
        | ActivePatterns.IntegerVector engine _
        | ActivePatterns.LogicalVector engine _
        | ActivePatterns.CharacterVector engine _
        | ActivePatterns.RawVector engine _ -> scalarOrVector engine sexp
        | ActivePatterns.List engine _ -> ListType
        | ActivePatterns.Function engine _ -> FunctionType
        | ActivePatterns.Environment engine _ -> EnvironmentType
        | _ ->
            RProvider.Common.LogFile.logf "No typed conversion was possible for sexp: %A" (SymbolicExpression.getType engine sexp)
            failwith "Could not classify expression as a semantic type."


    /// Functions for accessing R functions within typed
    /// R wrappers.
    module private R =

        let passThrough _ (v: obj) = v :?> SymbolicExpression

        let baseOp (fn: string) (a: SymbolicExpression) tryMake =
            let rEnv = Environment.globalEnv Singletons.engine.Value
            let sexp = Call.callFuncByName passThrough rEnv "base" fn Seq.empty [| a |]

            match sexp with
            | Ok sexp -> tryMake sexp |> Option.get
            | Error e -> failwith e

        let baseOp2 (fn: string) (a: SymbolicExpression) (b: SymbolicExpression) tryMake =
            let rEnv = Environment.globalEnv Singletons.engine.Value
            let sexp = Call.callFuncByName passThrough rEnv "base" fn Seq.empty [| a; b |]

            match sexp with
            | Ok sexp -> tryMake sexp |> Option.get
            | Error e -> failwith e

        let baseOpArray (fn: string) (a: SymbolicExpression array) tryMake =
            let rEnv = Environment.globalEnv Singletons.engine.Value
            let sexp = Call.callFuncByName passThrough rEnv "base" fn Seq.empty (a |> Array.map(fun a -> a :> obj))

            match sexp with
            | Ok sexp -> tryMake sexp |> Option.get
            | Error e -> failwith e


    /// A basic representation of a vector that does not
    /// support numeric operations.
    module VectorBase =

        type RVectorBase<'T> =
            internal { Sexp: SymbolicExpression }

            /// If a vector is a named vector, returns a list of the names
            /// associated with the vector. Otherwise, returns a blank array.
            member this.Names = Vector.tryNames Singletons.engine.Value this.Sexp |> Option.defaultValue [||]

            /// Get a scalar item by R index (1..n based, not zero based).
            member this.Item(i: int, mk) : 'T =
                let idxSexp = Create.intVector Singletons.engine.Value [| Some <| i + 1 |]
                R.baseOp2 "[[" this.Sexp idxSexp mk

            /// If a vector is named, get the named item from the vector.
            member this.Item(name: string, mk) : 'T =
                let idx = this.Names |> Array.findIndex ((=) name)
                this.[idx, mk]

            member this.AsRExpr = this.Sexp |> RExprWrapper.toRProvider

        /// A vector of any length may become a generic vector.
        let tryFromExpression sexp =
            match classify Singletons.engine.Value sexp with
            | VectorType
            | ScalarType -> { Sexp = sexp } |> Some
            | _ -> None


    /// A basic representation of a scalar value in R, which
    /// does not support numeric operators.
    module ScalarBase =

        type RScalarBase<'T> =
            internal { Sexp: SymbolicExpression }

            member this.AsRExpr = this.Sexp |> RExprWrapper.toRProvider

        /// Creates a scalar from a size-1 vector, or returns None.
        let tryFromExpression sexp =
            match classify Singletons.engine.Value sexp with
            | ScalarType -> { Sexp = sexp } |> Some
            | _ -> None

        /// Enforces that a scalar is a vector of a single element,
        /// to be used before any operation.
        let internal enforceShape num =
            if Singletons.engine.Value.invokeInt(fun e -> SymbolicExpression.length e num) = 1 then
                num
            else
                failwith "A scalar R value was mutated and is no longer scalar."


    /// Types for expressing real numbers that are within
    /// an R environment. Operations are zero-copy within R.
    module Real =

        module Scalar =

            /// A scalar value currently residing in R's memory space.
            type RRealScalar<[<Measure>] 'u> = internal { Sexp: SymbolicExpression }

            let tryFromExpression (sexp: SymbolicExpression) =
                match sexp with
                | ActivePatterns.RealVector Singletons.engine.Value _ ->
                    if Singletons.engine.Value.invokeInt(fun e -> SymbolicExpression.length e sexp) = 1 then Some { Sexp = sexp } else None
                | _ -> None

            let extractScalarFloat (scalar: RRealScalar<'u>) =
                ScalarBase.enforceShape scalar.Sexp
                |> Extract.extractFloatArray Singletons.engine.Value
                |> Array.head
                |> Option.map ((*) (LanguagePrimitives.FloatWithMeasure<'u> 1.))

            let fromFloat (value: float<'u>) : RRealScalar<'u> option =
                let value = value / LanguagePrimitives.FloatWithMeasure<'u> 1
                Create.realVector Singletons.engine.Value [| Some value |] |> tryFromExpression

            type RRealScalar<'u> with
                static member Add (a: RRealScalar<'u>) (b: RRealScalar<'u>) : RRealScalar<'u> =
                    R.baseOp2 "+" a.Sexp b.Sexp tryFromExpression

                static member Sub (a: RRealScalar<'u>) (b: RRealScalar<'u>) : RRealScalar<'u> =
                    R.baseOp2 "-" a.Sexp b.Sexp tryFromExpression

                static member Mul (a: RRealScalar<'u>) (b: RRealScalar<'v>) : RRealScalar<'u 'v> =
                    R.baseOp2 "*" a.Sexp b.Sexp tryFromExpression

                static member Div (a: RRealScalar<'u>) (b: RRealScalar<'v>) : RRealScalar<'u / 'v> =
                    R.baseOp2 "/" a.Sexp b.Sexp tryFromExpression

                static member Log a = R.baseOp "log" a.Sexp tryFromExpression
                static member Exp a = R.baseOp "exp" a.Sexp tryFromExpression

                static member Scale (a: RRealScalar<'u>) (s: RRealScalar<1>) : RRealScalar<'u> =
                    R.baseOp2 "*" a.Sexp s.Sexp tryFromExpression

                static member ToFloat a = extractScalarFloat a
                static member FromFloat f = fromFloat f

                static member (+)(a, b) = RRealScalar.Add a b
                static member (-)(a, b) = RRealScalar.Sub a b
                static member (*)(a, b) = RRealScalar.Mul a b
                static member (/)(a, b) = RRealScalar.Div a b

                member this.AsRExpr = this.Sexp |> RExprWrapper.toRProvider
                member this.FromR = lazy(this |> extractScalarFloat)

        module Vector =

            type RRealVector<[<Measure>] 'u> = { Inner: VectorBase.RVectorBase<Scalar.RRealScalar<'u>> }

            let tryFromExpression sexp =
                match sexp with
                | ActivePatterns.RealVector Singletons.engine.Value _ -> Some { Inner = { Sexp = sexp } }
                | _ -> None
    
            /// Send an F# sequence of floats to R.
            let fromFloats (items: float<'u> seq) : RRealVector<'u> =
                Create.realVector Singletons.engine.Value (items |> Seq.map ((*) (LanguagePrimitives.FloatWithMeasure<1/'u> 1.) >> Some))
                |> tryFromExpression
                |> Option.get

            /// Send an F# sequence of floats to R, where in F# the floats
            /// are option-typed with None representing NA values.
            let fromFloatsWithNA items =
                Create.realVector Singletons.engine.Value (items |> Seq.map (Option.map <| (*) (LanguagePrimitives.FloatWithMeasure<1/'u> 1.)))
                |> tryFromExpression
                |> Option.get

            /// Concatenate a sequence of R scalar values into a single
            /// R vector.
            let fromScalars (items: Scalar.RRealScalar<'u> seq) =
                R.baseOpArray "c" (items |> Seq.toArray |> Array.map(fun s -> s.Sexp)) tryFromExpression

            let extract (vector:RRealVector<'u>) =
                Extract.extractFloatArray Singletons.engine.Value vector.Inner.Sexp
                |> Array.map(Option.map ((*) (LanguagePrimitives.FloatWithMeasure<'u> 1.)))

            type RRealVector<'u> with

                static member Lift(_: Scalar.RRealScalar<'u>, vector: RRealVector<'u>) : RRealVector<'u> = vector
                static member Lift(vec1: RRealVector<'u>, _: RRealVector<'u>) = vec1

                static member Mean(a: RRealVector<'u>) = R.baseOp "mean" a.Inner.Sexp Scalar.tryFromExpression

                static member (+)(a: RRealVector<'u>, b: RRealVector<'u>) : RRealVector<'u> =
                    R.baseOp2 "+" a.Inner.Sexp b.Inner.Sexp tryFromExpression
                static member (-)(a: RRealVector<'u>, b: RRealVector<'u>) : RRealVector<'u> =
                    R.baseOp2 "-" a.Inner.Sexp b.Inner.Sexp tryFromExpression
                static member (*)(a: RRealVector<'u>, b: RRealVector<'v>) : RRealVector<'u 'v> =
                    R.baseOp2 "*" a.Inner.Sexp b.Inner.Sexp tryFromExpression
                static member (/)(a: RRealVector<'u>, b: RRealVector<'v>) : RRealVector<'u/'v> =
                    R.baseOp2 "/" a.Inner.Sexp b.Inner.Sexp tryFromExpression

                member this.Item(i: int) = this.Inner.[i, Scalar.tryFromExpression]
                member this.Item(name: string) = this.Inner.[name, Scalar.tryFromExpression]
                member this.Length = R.baseOp "length" this.Inner.Sexp Scalar.tryFromExpression
                /// Extract a vector from R to F#, where None is used
                /// to represent R's NA values.
                member this.FromR = lazy(extract this)

    /// Semantic types for integer vectors and scalars.
    /// Supports arithmetic using R functions.
    module Integer =

        /// Scalar operations on integers in R. For int-based
        /// arithmetic, R will always return real numbers.
        module Scalar =

            /// A scalar value currently residing in R's memory space.
            type RIntScalar<[<Measure>] 'u> = internal { Sexp: SymbolicExpression }

            let tryFromExpression (sexp: SymbolicExpression) =
                match sexp with
                | ActivePatterns.IntegerVector Singletons.engine.Value _ ->
                    if Singletons.engine.Value.invokeInt(fun e -> SymbolicExpression.length e sexp) = 1 then Some { Sexp = sexp } else None
                | _ -> None

            let extractScalar (scalar: RIntScalar<'u>) =
                ScalarBase.enforceShape scalar.Sexp
                |> Extract.extractIntArray Singletons.engine.Value
                |> Array.head
                |> Option.map ((*) (LanguagePrimitives.Int32WithMeasure<'u> 1))

            let fromInt (value: int<'u>) : RIntScalar<'u> option =
                let value = value / LanguagePrimitives.Int32WithMeasure<'u> 1
                Create.intVector Singletons.engine.Value [| Some value |] |> tryFromExpression

            let private enforce a = ScalarBase.enforceShape a

            type RIntScalar<'u> with
                static member Add (a: RIntScalar<'u>) (b: RIntScalar<'u>) : Real.Scalar.RRealScalar<'u> =
                    R.baseOp2 "+" (enforce a.Sexp) (enforce b.Sexp) Real.Scalar.tryFromExpression

                static member Sub (a: RIntScalar<'u>) (b: RIntScalar<'u>) : Real.Scalar.RRealScalar<'u> =
                    R.baseOp2 "-" (enforce a.Sexp) (enforce b.Sexp) Real.Scalar.tryFromExpression

                static member Mul (a: RIntScalar<'u>) (b: RIntScalar<'v>) : Real.Scalar.RRealScalar<'u 'v> =
                    R.baseOp2 "*" (enforce a.Sexp) (enforce b.Sexp) Real.Scalar.tryFromExpression

                static member Div (a: RIntScalar<'u>) (b: RIntScalar<'v>) : Real.Scalar.RRealScalar<'u / 'v> =
                    R.baseOp2 "/" (enforce a.Sexp) (enforce b.Sexp) Real.Scalar.tryFromExpression

                static member Log a = R.baseOp "log" (enforce a.Sexp) Real.Scalar.tryFromExpression
                static member Exp a = R.baseOp "exp" (enforce a.Sexp) Real.Scalar.tryFromExpression

                static member Scale (a: RIntScalar<'u>) (s: RIntScalar<1>) : RIntScalar<'u> =
                    R.baseOp2 "*" (enforce a.Sexp) (enforce s.Sexp) tryFromExpression

                static member ToFloat a = extractScalar a
                static member FromInt (f: int<'u>) = fromInt f
                static member ToReal (a: RIntScalar<'u>) : Real.Scalar.RRealScalar<'u> = R.baseOp "as.double" (enforce a.Sexp) Real.Scalar.tryFromExpression

                static member (+)(a, b) = RIntScalar.Add a b
                static member (-)(a, b) = RIntScalar.Sub a b
                static member (*)(a, b) = RIntScalar.Mul a b
                static member (/)(a, b) = RIntScalar.Div a b

                member this.AsRExpr = this.Sexp |> RExprWrapper.toRProvider
                member this.FromR = lazy(this |> extractScalar)

        module Vector =

            type RIntVector<[<Measure>] 'u> = { Inner: VectorBase.RVectorBase<Scalar.RIntScalar<'u>> }

            let tryFromExpression sexp =
                match sexp with
                | ActivePatterns.IntegerVector Singletons.engine.Value _ -> Some { Inner = { Sexp = sexp } }
                | _ -> None
    
            type RIntVector<'u> with

                static member Lift(scalar: Scalar.RIntScalar<'u>, vector: RIntVector<'u>) : RIntVector<'u> =
                    tryFromExpression scalar.Sexp
                    |> Option.defaultWith(fun _ -> failwith "Could not place scalar into a vector")

                static member Lift(vec1: RIntVector<'u>, vec2: RIntVector<'u>) = vec1

                static member Add (a: RIntVector<'u>) (b: RIntVector<'u>) : RIntVector<'u> =
                    R.baseOp2 "+" a.Inner.Sexp b.Inner.Sexp tryFromExpression

                static member Mean(a: RIntVector<'u>) = R.baseOp "mean" a.Inner.Sexp Scalar.tryFromExpression
                static member (+)(a, b) = RIntVector.Add a b
                member this.Item(i: int) = this.Inner.[i, Scalar.tryFromExpression]
                member this.Item(name: string) = this.Inner.[name, Scalar.tryFromExpression]
                member this.Length = R.baseOp "length" this.Inner.Sexp Scalar.tryFromExpression


    type RVector<[<Measure>] 'u> =
        | NumericV of Real.Vector.RRealVector<'u>
        | IntegerV of Integer.Vector.RIntVector<'u>
        | LogicalV of VectorBase.RVectorBase<bool>
        | CharacterV of VectorBase.RVectorBase<string>
        | ComplexV of VectorBase.RVectorBase<Extensions.RComplex>
        | RawV of VectorBase.RVectorBase<byte>

    with
        member this.AsReal() =
            match this with
            | NumericV s -> s
            | _ -> failwith "Expression was not a vector of real numbers"

        member internal this.Sexp =
            match this with
            | NumericV v -> v.Inner.AsRExpr |> RExprWrapper.toRBridge
            | IntegerV v -> v.Inner.AsRExpr |> RExprWrapper.toRBridge
            | LogicalV v -> v.AsRExpr |> RExprWrapper.toRBridge
            | CharacterV v -> v.AsRExpr |> RExprWrapper.toRBridge
            | ComplexV v -> v.AsRExpr |> RExprWrapper.toRBridge
            | RawV v -> v.AsRExpr |> RExprWrapper.toRBridge


    module GenericVector =

        let tryFromExpression sexp =
            match sexp with
            | ActivePatterns.RealVector Singletons.engine.Value v -> Real.Vector.tryFromExpression v |> Option.map NumericV
            | ActivePatterns.IntegerVector Singletons.engine.Value v -> Integer.Vector.tryFromExpression v |> Option.map IntegerV
            | ActivePatterns.LogicalVector Singletons.engine.Value _ ->
                VectorBase.tryFromExpression sexp |> Option.map LogicalV
            | ActivePatterns.CharacterVector Singletons.engine.Value _ ->
                VectorBase.tryFromExpression sexp |> Option.map CharacterV
            | ActivePatterns.ComplexVector Singletons.engine.Value _ ->
                VectorBase.tryFromExpression sexp |> Option.map ComplexV
            | ActivePatterns.RawVector Singletons.engine.Value _ ->
                VectorBase.tryFromExpression sexp |> Option.map RawV
            | _ -> None

    /// Represents R lists, which may contain elements of any type.
    module HeterogeneousList =

        /// An R heterogeneous list.
        type HList = private { sexp: SymbolicExpression }

        let tryFromExpression sexp =
            match classify Singletons.engine.Value sexp with
            | ListType -> Some { sexp = sexp }
            | _ -> None

        type HList with
            member internal this.Sexp = this.sexp
            member internal this.AsRExpr = this.sexp |> RExprWrapper.toRProvider
            
            member this.Item(i: int) =
                SymbolicExpression.getVectorElement Singletons.engine.Value this.sexp i
                |> RExprWrapper.toRProvider

            member this.Item(name: string) =
                SymbolicExpression.getListItemByName Singletons.engine.Value name this.sexp
                |> RExprWrapper.toRProvider

            member this.Length =
                Singletons.engine.Value.invokeInt(fun e ->  SymbolicExpression.length e  this.sexp)


    module Factor =

        type RFactor =
            internal { Sexp: SymbolicExpression }
            member this.AsRExpr = this.Sexp |> RExprWrapper.toRProvider
            member this.Levels = lazy (
                Factor.trylevels Singletons.engine.Value this.Sexp
                |> Option.map(fun levels ->
                    levels |> List.map (Option.defaultWith(fun _ -> failwithf "Levels contained NA, which is not permitted by R."))))

            member this.Indices = lazy (Extract.extractIntArray Singletons.engine.Value this.Sexp)

            member this.AsStringVector =
                lazy
                    (this.Levels.Value
                    |> Option.defaultWith (fun _ -> failwith "Could not get levels for factor")
                    |> fun levels ->
                        this.Indices.Value
                        |> Array.map (fun idx ->
                            match idx with
                            | Some idx when idx >= 1 && idx <= levels.Length ->
                                levels.[idx - 1]
                            | None -> "NA"
                            | _ -> failwith "Invalid index requested for factor."
                        ))

        let tryFromExpression expr : RFactor option =
            match expr with
            | ActivePatterns.Factor Singletons.engine.Value e -> Some { Sexp = e }
            | _ -> None


    /// Type wrapper for R data.frame instances.
    module DataFrame =

        module Column =

            type Column =
                | NumericColumn of Real.Vector.RRealVector<1>
                | IntegerColumn of RProvider.Abstractions.RExpr
                | LogicalColumn of RProvider.Abstractions.RExpr
                | FactorColumn of Factor.RFactor
                | CharacterColumn of RProvider.Abstractions.RExpr
                | ListColumn of HeterogeneousList.HList
                | MatrixColumn of RProvider.Abstractions.RExpr

            let tryFromExpression sexp =
                match classify Singletons.engine.Value sexp with
                | VectorType ->
                    RProvider.Common.LogFile.logf "Vector"
                    match sexp with
                    | ActivePatterns.RealVector Singletons.engine.Value v -> Real.Vector.tryFromExpression v |> Option.map NumericColumn
                    | ActivePatterns.IntegerVector Singletons.engine.Value v -> IntegerColumn (RExprWrapper.toRProvider sexp) |> Some 
                    | ActivePatterns.LogicalVector Singletons.engine.Value v -> LogicalColumn (RExprWrapper.toRProvider sexp) |> Some 
                    | ActivePatterns.ComplexVector Singletons.engine.Value v -> None
                    | ActivePatterns.CharacterVector Singletons.engine.Value v -> CharacterColumn (RExprWrapper.toRProvider sexp) |> Some 
                    | ActivePatterns.RawVector Singletons.engine.Value v -> None
                    | _ -> None
                | FactorType -> Factor.tryFromExpression sexp |> Option.map FactorColumn
                | MatrixType -> MatrixColumn (RExprWrapper.toRProvider sexp) |> Some
                | _ -> None
            
            let getSexp = function
                | NumericColumn c -> c.Inner.Sexp
                | MatrixColumn c
                | IntegerColumn c
                | LogicalColumn c
                | CharacterColumn c -> c |> RExprWrapper.toRBridge
                | ListColumn c -> c.Sexp
                | FactorColumn c -> c.AsRExpr |> RExprWrapper.toRBridge

        type RFrame = internal { Sexp: SymbolicExpression }

        let tryCreate (cols: (string * SymbolicExpression) array) =
            failwith "Creation of semantic types currently not implemented"

        /// Get row names of an R data frame. R stores row names in three
        /// ways: as sequential numbers, as character strings, and as
        /// a 'compact' encoding. All cases are extracted to a string array
        /// by this function.
        let rowNames df =
            match SymbolicExpression.tryGetAttribute df.Sexp "row.names" Singletons.engine.Value with
            | Some rn ->
                match rn with
                | ActivePatterns.CharacterVector Singletons.engine.Value _ ->
                    Extract.extractStringArray Singletons.engine.Value rn
                | ActivePatterns.IntegerVector Singletons.engine.Value _ ->
                    let ints = Extract.extractIntArray Singletons.engine.Value rn
                    if ints.Length = 2
                    then
                        // If the first int is NA and the second is negative, is compact encoding.
                        match ints.[0], ints.[1] with
                        | None, Some i1 ->
                            if i1 < 0
                            then
                                let n = -i1
                                [| 1 .. n |] |> Array.map Some
                            else ints
                        | _ -> ints
                    else ints
                    |> Array.map(Option.map (sprintf "%i"))
                | _ -> Array.empty
            | None -> Array.empty
            |> Array.map (Option.defaultValue "NA")

        let rowCount df =
            rowNames df |> Seq.length

        let tryFromExpression (sexp: SymbolicExpression) =
            match SymbolicExpression.getClasses Singletons.engine.Value sexp with
            | ls when ls |> List.contains (Some "data.frame") -> Some { Sexp = sexp }
            | _ -> None

        let getColumnNames frame =
            SymbolicExpression.tryGetAttribute frame.Sexp "names" Singletons.engine.Value
            |> Option.map (RBridge.Extensions.Promise.force Singletons.engine.Value)
            |> Option.map (Extract.extractStringArray Singletons.engine.Value)
            |> Option.defaultValue [||]

        let getColumn name frame =
            let names = getColumnNames frame
            let colIndex =
                names
                |> Array.tryFindIndex ((=) (Some name))
                |> Option.defaultWith (fun () ->
                    failwithf "Column '%s' not found in R data.frame" name)
            let colSexp = SymbolicExpression.getVectorElement Singletons.engine.Value frame.Sexp colIndex
            colSexp
            |> Column.tryFromExpression
            |> Option.defaultWith(fun _ -> failwithf "Column %s could not be coerced into a vector" name)


        type RFrame with
            static member GetColumnNames df = getColumnNames df
            static member GetColumn(df, name) = getColumn name df
            static member RowCount df = rowCount df

            member this.AsRExpr = this.Sexp |> RExprWrapper.toRProvider
            member this.Column name = RFrame.GetColumn(this, name)
            member this.Names = RFrame.GetColumnNames this
            member this.RowNames = rowNames this


    type RScalar<[<Measure>] 'u> =
        | NumericS of Real.Scalar.RRealScalar<'u>
        | IntegerS of Integer.Scalar.RIntScalar<'u>
        | LogicalS of ScalarBase.RScalarBase<bool>
        | CharacterS of ScalarBase.RScalarBase<string>
        | ComplexS of ScalarBase.RScalarBase<RComplex>
        | RawS of RProvider.Abstractions.RExpr

    with
        member this.AsReal() =
            match this with
            | NumericS s -> s
            | _ -> failwith "Expression was not a real number"

        member this.AsInt() =
            match this with
            | IntegerS s -> s
            | _ -> failwith "Expression was not an integer"

        member this.AsLogical() =
            match this with
            | LogicalS s -> s
            | _ -> failwith "Expression was not logical (boolean)"

        member this.AsCharacter() =
            match this with
            | CharacterS s -> s
            | _ -> failwith "Expression was not character"

        member this.AsComplex() =
            match this with
            | ComplexS s -> s
            | _ -> failwith "Expression was not complex number"

        member internal this.Sexp =
            match this with
            | NumericS s -> s.Sexp
            | IntegerS s -> s.Sexp
            | LogicalS s -> s.Sexp
            | CharacterS s -> s.Sexp
            | ComplexS s -> s.Sexp
            | RawS s -> s |> RExprWrapper.toRBridge


    module GenericScalar =

        let tryFromExpression sexp =
            match sexp with
            | ActivePatterns.RealVector Singletons.engine.Value v ->
                Real.Scalar.tryFromExpression v |> Option.map NumericS
            | ActivePatterns.IntegerVector Singletons.engine.Value v ->
                Integer.Scalar.tryFromExpression v |> Option.map IntegerS
            | ActivePatterns.LogicalVector Singletons.engine.Value _ ->
                ScalarBase.tryFromExpression sexp |> Option.map LogicalS
            | ActivePatterns.CharacterVector Singletons.engine.Value _ ->
                ScalarBase.tryFromExpression sexp |> Option.map CharacterS
            | ActivePatterns.ComplexVector Singletons.engine.Value _ ->
                ScalarBase.tryFromExpression sexp |> Option.map ComplexS
            | ActivePatterns.RawVector Singletons.engine.Value _ -> sexp |> RExprWrapper.toRProvider |> RawS |> Some
            | _ -> failwith "not implemented (Scalar)"


    /// A wrapped representation of an R value, which
    /// remains in R. It is not within .NET memory space.
    type RSemantic<[<Measure>] 'u> =
        | ScalarInR of RScalar<'u>
        | VectorInR of RVector<'u>
        | DataFrameInR of DataFrame.RFrame
        | FactorInR of Factor.RFactor
        | ListInR of HeterogeneousList.HList

    with
        member internal this.Sexp =
            match this with
            | ScalarInR s -> s.Sexp
            | VectorInR v -> v.Sexp
            | DataFrameInR df -> df.Sexp
            | FactorInR f -> f.Sexp
            | ListInR l -> l.Sexp