namespace RProvider.Runtime

open RBridge
open RBridge.Extensions
open RBridge.Extensions.ActivePatterns
open RProvider.Runtime

/// Adds functions to the Symbolic Expression module,
/// which cover semantic operations and have the R engine
/// singleton injected in.
[<RequireQualifiedAccess>]
module SymbolicExpression =

    /// Send a .NET value to R memory, returning it's reference
    /// as an R symbolic expression.
    let ofObj item = Convert.toR Singletons.engine.Value item

    /// Get the type of the expression as an R sexp type
    let getExprType sexp = SymbolicExpression.getType Singletons.engine.Value sexp

    /// <summary> For an S4 object, get a dictionary containing first the
    /// slot name and second the slot's R type. If the expression
    /// is not an S4 object, returns `None`.</summary>
    /// <param name="expr">An R symbolic expression</param>
    /// <returns>A diictionary with key = slot name, and value = R type</returns>
    let trySlots (expr: SymbolicExpression) =
        match expr with
        | S4Object Singletons.engine.Value s4Class -> S4.tryGetSlotTypes Singletons.engine.Value expr
        | _ -> None

    /// <summary> For an S4 object, get a dictionary containing first the
    /// slot name and second the slot's R type.</summary>
    /// <param name="expr">An R symbolic expression</param>
    /// <returns>A diictionary with key = slot name, and value = R type</returns>
    let slots (expr: SymbolicExpression) =
        match trySlots expr with
        | Some slots -> slots
        | None -> invalidOp "Can only get slots for an S4 object (R type)"

    /// <summary>Gets the value of a slot as a SymbolicExpression</summary>
    /// <param name="name">Slot name to retrieve</param>
    /// <param name="expr">An R symbolic expression</param>
    /// <returns>Some symbolic expression if the expression was an S4
    /// object and had the slot, or None otherwise.</returns>
    let trySlot name (expr: SymbolicExpression) =
        RProvider.Common.LogFile.logf "Type in slot %A" (SymbolicExpression.getType Singletons.engine.Value expr)
        match expr with
        | S4Object Singletons.engine.Value s4Class -> S4.tryGetSlot Singletons.engine.Value expr name
        | _ ->
            RProvider.Common.LogFile.logf "Help"
            None

    /// <summary>Gets the value of a slot as a SymbolicExpression</summary>
    /// <param name="name">Slot name to retrieve</param>
    /// <param name="expr">An R symbolic expression</param>
    /// <returns>A symbolic expression containing the slot value</returns>
    let slot name (expr: SymbolicExpression) =
        match trySlot name expr with
        | Some slot -> slot
        | None -> invalidOp "Can only get slot for an S4 object (R type)"

    /// <summary>Get the data from a column in an R dataframe
    /// by its name.</summary>
    /// <param name="name">The column name</param>
    /// <param name="expr">An R symbolic expression</param>
    /// <returns>A vector containing the data</returns>
    let column (name: string) (expr: SymbolicExpression) : SymbolicExpression =
        match expr with
        | DataFrame Singletons.engine.Value df ->
            RTypes.DataFrame.tryFromExpression df
            |> Option.defaultWith (fun _ -> failwith "The expression was not a valid R data frame")
            |> RTypes.DataFrame.getColumn name
            |> RTypes.DataFrame.Column.getSexp
        | _ -> invalidOp "The expression is not an R data frame."

    /// Get the R classes associated with an R expression.
    let rClass sexp =
        match SymbolicExpression.tryGetAttribute sexp "class" Singletons.engine.Value with
        | None -> [||]
        | Some attrs ->
            match attrs with
            | CharacterVector Singletons.engine.Value v -> v |> Extract.extractStringArray Singletons.engine.Value |> Array.map (Option.defaultValue "NA")
            | _ -> [||]

    /// Pass a value from R memory space into .NET, represented
    /// as a .NET primitive or the closest approximation of the
    /// relevant R primitive.
    let tryGetValue<'a> sexp = Convert.tryFromRStructural<'a> Singletons.engine.Value sexp

    let getValue<'a> sexp =
        match tryGetValue<'a> sexp with
        | Some r -> r
        | None -> failwithf "Could not convert R expression to .NET type %s." typeof<'a>.Name

    let semanticType = RTypes.classify Singletons.engine.Value
    
    let tryGetTyped sexp = Convert.tryAsRTyped Singletons.engine.Value sexp

    let getTyped sexp =
        match tryGetTyped sexp with
        | Some r -> r
        | None -> failwith "Could not convert R expression to a semantic type."

    let listItem name sexp = SymbolicExpression.getListItemByName Singletons.engine.Value name sexp

    let getMember name sexp =
        match sexp with
        | S4Object Singletons.engine.Value s4Class ->
            match SymbolicExpression.tryGetAttribute sexp name Singletons.engine.Value with
            | Some e -> e
            | None -> failwithf "Member not defined: %s" name
        | List Singletons.engine.Value l -> listItem name l
        | _ ->
            let t = SymbolicExpression.getType Singletons.engine.Value sexp
            invalidOp <| sprintf "Unsupported operation on R object (R type: %A)" t

    let typedVectorByName (name: string) sexp =
        match RTypes.GenericVector.tryFromExpression sexp with
        | Some v ->
            match v with
            | RTypes.RVector.NumericV v -> v.[name] |> RTypes.NumericS
            | _ -> failwith "not implemented (typed v by name)"
        | None -> invalidOp "Expression was not a vector"

    let typedVectorByIndex (index: int) sexp =
        match RTypes.GenericVector.tryFromExpression sexp with
        | Some v ->
            match v with
            | RTypes.RVector.NumericV v -> v.[index] |> RTypes.NumericS
            | _ -> failwith "not implemented (typed v by index)"
        | None -> invalidOp "Expression was not a vector"

    let head sexp =
        if SymbolicExpression.isVector Singletons.engine.Value sexp then
            typedVectorByIndex 0 sexp
        else
            failwith "Symbolic expression was not a vector"

    let tryHead sexp =
        if SymbolicExpression.isVector Singletons.engine.Value sexp then typedVectorByIndex 0 sexp |> Some else None


/// [omit]
[<AutoOpen>]
module SymbolicExpressionExtensions =

    type SymbolicExpression with

        member this.Class: string [] = SymbolicExpression.rClass this
        member this.Type = SymbolicExpression.semanticType this

        member this.TryFromR<'a>() = SymbolicExpression.tryGetValue<'a> this

        /// Extract the value from R memory space into .NET, with
        /// type 'a.
        member this.FromR<'a>() = SymbolicExpression.getValue<'a> this

        /// Get the member symbolic expression of given name.
        member this.Member(name: string) = SymbolicExpression.getMember name this

        /// Get the value from the typed vector by name.
        member this.ValueOf(name: string) : RTypes.RScalar<'u> = SymbolicExpression.typedVectorByName name this

        /// Represents the R value in an appropriate semantic
        /// R type for further data exploration and analysis, without
        /// extraction from R memory.
        member this.TryAsRTyped = SymbolicExpression.tryGetTyped this
        member this.AsTyped = SymbolicExpression.getTyped this
        member this.AsDataFrame = RTypes.DataFrame.tryFromExpression this
        member this.AsVector = RTypes.GenericVector.tryFromExpression this
        member this.AsScalar = RTypes.GenericScalar.tryFromExpression this
        member this.AsFactor = RTypes.Factor.tryFromExpression this
        member this.AsList = RTypes.HeterogeneousList.tryFromExpression this

        /// Get the value from an indexed vector by index.
        member this.ValueAt(index: int) : RTypes.RScalar<'u> = SymbolicExpression.typedVectorByIndex index this

        /// Get the first value of a vector.
        member this.Head<'a>() = SymbolicExpression.head this

        /// Try and get the first value of a vector, returning
        /// `None` if the `SymbolicExpression` is not a vector
        /// or an empty vector.
        member this.TryHead<'a>() = SymbolicExpression.tryHead this
