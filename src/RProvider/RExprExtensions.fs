namespace RProvider

open RProvider.Abstractions
open RProvider.Runtime

/// Functions for working with R expressions.
[<RequireQualifiedAccess>]
module RExpr =

    let private slotTypesToNA (slots: Map<string, string option>) =
        slots |> Map.map(fun k v -> v |> Option.defaultValue "NA")

    /// <summary> For an S4 object, get a dictionary containing first the
    /// slot name and second the slot's R type. If the expression
    /// is not an S4 object, returns `None`.</summary>
    /// <param name="expr">An R symbolic expression</param>
    /// <returns>A diictionary with key = slot name, and value = R type</returns>
    let trySlots: RExpr -> Map<string, string> option = RExprWrapper.toRBridge >> SymbolicExpression.trySlots >> Option.map slotTypesToNA

    /// <summary> For an S4 object, get a dictionary containing first the
    /// slot name and second the slot's R type.</summary>
    /// <param name="expr">An R symbolic expression</param>
    /// <returns>A diictionary with key = slot name, and value = R type</returns>
    let slots: RExpr -> Map<string, string> = RExprWrapper.toRBridge >> SymbolicExpression.slots >> slotTypesToNA

    /// <summary>Gets the value of a slot as a SymbolicExpression</summary>
    /// <param name="name">Slot name to retrieve</param>
    /// <param name="expr">An R symbolic expression</param>
    /// <returns>Some symbolic expression if the expression was an S4
    /// object and had the slot, or None otherwise.</returns>
    let trySlot name expr = expr |> RExprWrapper.toRBridge |> SymbolicExpression.trySlot name |> Option.map RExprWrapper.toRProvider

    /// <summary>Gets the value of a slot as a SymbolicExpression</summary>
    /// <param name="name">Slot name to retrieve</param>
    /// <param name="expr">An R symbolic expression</param>
    /// <returns>A symbolic expression containing the slot value</returns>
    let slot name expr = expr |> RExprWrapper.toRBridge |> SymbolicExpression.slot name |> RExprWrapper.toRProvider

    /// <summary>Get the data from a column in an R dataframe
    /// by its name.</summary>
    /// <param name="name">The column name</param>
    /// <param name="expr">An R symbolic expression</param>
    /// <returns>A vector containing the data</returns>
    let column (name: string) expr =
        expr |> RExprWrapper.toRBridge |> SymbolicExpression.column name |> RExprWrapper.toRProvider

    /// Get the R classes associated with an R expression.
    let classes = RExprWrapper.toRBridge >> SymbolicExpression.rClass

    /// Pass a value from R memory space into .NET, represented
    /// as a .NET primitive or the closest approximation of the
    /// relevant R primitive.
    let tryGetValue<'a> = RExprWrapper.toRBridge >> SymbolicExpression.tryGetValue<'a>

    let getValue<'a> = RExprWrapper.toRBridge >> SymbolicExpression.getValue<'a>

    let tryGetTyped: RExpr -> RTypes.RSemantic<1> option =
        RExprWrapper.toRBridge >> SymbolicExpression.tryGetTyped

    let getTyped: RExpr -> RTypes.RSemantic<1> = RExprWrapper.toRBridge >> SymbolicExpression.getTyped

    let getType: RExpr -> RTypes.RSemanticType = RExprWrapper.toRBridge >> SymbolicExpression.semanticType

    let listItem name expr =
        expr |> RExprWrapper.toRBridge |> SymbolicExpression.listItem name |> RExprWrapper.toRProvider

    let getMember name expr =
        expr |> RExprWrapper.toRBridge |> SymbolicExpression.getMember name |> RExprWrapper.toRProvider

    let typedVectorByName (name: string) expr =
        expr |> RExprWrapper.toRBridge |> SymbolicExpression.typedVectorByName name

    let typedVectorByIndex (index: int) expr =
        expr |> RExprWrapper.toRBridge |> SymbolicExpression.typedVectorByIndex index

    let head: RExpr -> Runtime.RTypes.RScalar<1> = RExprWrapper.toRBridge >> SymbolicExpression.head
    let tryHead: RExpr -> Runtime.RTypes.RScalar<1> option = RExprWrapper.toRBridge >> SymbolicExpression.tryHead

    let printToString = RExprWrapper.toRBridge >> Runtime.Printing.printUsingTempFile


/// Public API for accessing RExpr, including converting to
/// R semantic types, .NET types, and extracting key metadata.
/// [omit]
[<AutoOpen>]
module RExprExtensions =

    type RExpr with

        member this.Class: string [] = RExpr.classes this
        member this.Type : RTypes.RSemanticType = RExpr.getType this

        member this.TryFromR<'a>() = RExpr.tryGetValue<'a> this

        /// Extract the value from R memory space into .NET, with
        /// type 'a.
        member this.FromR<'a>() = RExpr.getValue<'a> this

        /// Get the member symbolic expression of given name.
        member this.Member(name: string) = RExpr.getMember name this

        /// Get the value from the typed vector by name.
        member this.ValueOf(name: string) : RTypes.RScalar<'u> =
            RExpr.typedVectorByName name this

        /// Represents the R value in an appropriate semantic
        /// R type for further data exploration and analysis, without
        /// extraction from R memory.
        member this.TryAsTyped = RExpr.tryGetTyped this
        member this.AsTyped = RExpr.getTyped this
        member this.TryAsDataFrame = RTypes.DataFrame.tryFromExpression (RExprWrapper.toRBridge this)
        member this.TryAsVector = RTypes.GenericVector.tryFromExpression (RExprWrapper.toRBridge this)
        member this.TryAsScalar = RTypes.GenericScalar.tryFromExpression (RExprWrapper.toRBridge this)
        member this.TryAsFactor = RTypes.Factor.tryFromExpression (RExprWrapper.toRBridge this)
        member this.TryAsList = RTypes.HeterogeneousList.tryFromExpression (RExprWrapper.toRBridge this)

        member this.AsDataFrame () =
            this.TryAsDataFrame
            |> Option.defaultWith(fun _ -> failwithf "The RExpr was not a data frame. It was a %A." this.Type)

        member this.AsVector () =
            this.TryAsVector
            |> Option.defaultWith(fun _ -> failwithf "The RExpr was not a vector. It was a %A." this.Type)

        member this.AsScalar () =
            this.TryAsScalar
            |> Option.defaultWith(fun _ -> failwithf "The RExpr was not a scalar. It was a %A." this.Type)

        member this.AsFactor () =
            this.TryAsFactor
            |> Option.defaultWith(fun _ -> failwithf "The RExpr was not a factor. It was a %A." this.Type)

        member this.AsList () =
            this.TryAsList
            |> Option.defaultWith(fun _ -> failwithf "The RExpr was not a list. It was a %A." this.Type)

        /// Get the value from an indexed vector by index.
        member this.ValueAt(index: int) : Runtime.RTypes.RScalar<'u> =
            RExpr.typedVectorByIndex index this

        /// Get the first value of a vector.
        member this.Head<'a>() = RExpr.head this

        /// Try and get the first value of a vector, returning
        /// `None` if the `RExpr` is not a vector
        /// or an empty vector.
        member this.TryHead<'a>() = RExpr.tryHead this

        /// The contents of R's print function as a string.
        member this.Print() = RExpr.printToString this
