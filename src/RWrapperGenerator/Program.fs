open RProvider.RInterop
open RProvider.RInteropInternal
open System.IO
open System
open System.Collections.Generic
open System.Globalization

let preamble =
    @"
using System;
using RDotNet;
using RProvider;

namespace RWrappers {
"

// Attempt to encode C# identifier rules from http://msdn.microsoft.com/en-us/library/aa664670(v=VS.71).aspx
let isValidIdentifier (identifier: string) =
    let isLetterCharacter c =
        match Char.GetUnicodeCategory(c) with
        | UnicodeCategory.LowercaseLetter
        | UnicodeCategory.UppercaseLetter
        | UnicodeCategory.LetterNumber
        | UnicodeCategory.ModifierLetter
        | UnicodeCategory.OtherLetter
        | UnicodeCategory.TitlecaseLetter -> true
        | _ -> false

    let isValidStartCharacter c = isLetterCharacter c || c = '_'

    let isValidPartCharacter c =
        match Char.GetUnicodeCategory(c) with
        | UnicodeCategory.DecimalDigitNumber
        | UnicodeCategory.ConnectorPunctuation
        | UnicodeCategory.SpacingCombiningMark
        | UnicodeCategory.Format -> true
        | _ when isLetterCharacter c -> true
        | _ -> false

    let identifier = identifier.Replace("_", "__").Replace(".", "_")

    isValidStartCharacter identifier.[0]
    && Array.TrueForAll(identifier.ToCharArray(), Predicate<char> isValidPartCharacter)

// C# Keywords from http://msdn.microsoft.com/en-us/library/aa664671(v=vs.71)
let isKeyword =
    function
    | "abstract"
    | "as"
    | "base"
    | "bool"
    | "break"
    | "byte"
    | "case"
    | "catch"
    | "char"
    | "checked"
    | "class"
    | "const"
    | "continue"
    | "decimal"
    | "default"
    | "delegate"
    | "do"
    | "double"
    | "else"
    | "enum"
    | "event"
    | "explicit"
    | "extern"
    | "false"
    | "finally"
    | "fixed"
    | "float"
    | "for"
    | "foreach"
    | "goto"
    | "if"
    | "implicit"
    | "in"
    | "int"
    | "interface"
    | "internal"
    | "is"
    | "lock"
    | "long"
    | "namespace"
    | "new"
    | "null"
    | "object"
    | "operator"
    | "out"
    | "override"
    | "params"
    | "private"
    | "protected"
    | "public"
    | "readonly"
    | "ref"
    | "return"
    | "sbyte"
    | "sealed"
    | "short"
    | "sizeof"
    | "stackalloc"
    | "static"
    | "string"
    | "struct"
    | "switch"
    | "this"
    | "throw"
    | "true"
    | "try"
    | "typeof"
    | "uint"
    | "ulong"
    | "unchecked"
    | "unsafe"
    | "ushort"
    | "using"
    | "virtual"
    | "void"
    | "volatile"
    | "while" -> true
    | _ -> false

let rec safeName name =
    if isKeyword name then
        // This prefixes allows us to use keywords as identifiers
        "@" + name
    else
        // Dots are so common that we replace with underscore, and we replace underscore with double-underscore.
        name.Replace("_", "__").Replace(".", "_")

let internal generateProperty (writer: TextWriter) (packageName: string) (name: string) =
    fprintfn
        writer
        "\t\tpublic static SymbolicExpression %s { get { return RInterop.call(\"%s\", \"%s\", emptyArr, emptyArr); } }\n"
        (safeName name)
        packageName
        name

let internal generateFunction
    (writer: TextWriter)
    (packageName: string)
    (name: string)
    (args: RParameter list)
    (hasVarArgs: bool)
    =
    let argArr =
        [| for arg in args -> sprintf "object %s = null" (safeName arg)
           if hasVarArgs then yield "params object[] paramArray" |]

    fprintfn writer "\t\tpublic static SymbolicExpression %s(%s) {" (safeName name) (String.Join(", ", argArr))
    let argsPass = String.Join(", ", args |> List.map safeName |> List.toArray)
    fprintfn writer "\t\t\tvar namedArgs = new object[] { %s };" argsPass

    fprintfn
        writer
        "\t\t\treturn RInterop.call(\"%s\", \"%s\", namedArgs, %s);"
        packageName
        name
        (if hasVarArgs then "paramArray" else "emptyArr")

    fprintfn writer "\t\t}\n"

let generatePackage (writer: TextWriter) (exposedNames: HashSet<string>) (packageName: string) =
    //fprintfn writer "\tpublic class %s {" (safeName packageName)
    //fprintfn writer "\t\tprivate static object[] emptyArr = new object[0];"

    loadPackage packageName

    for name, rval in getBindings packageName do
        let name = if exposedNames.Contains(name) then packageName + "." + name else name

        match deserializeRValue rval with
        | RValue.Value -> generateProperty writer packageName name
        | RValue.Function (args, hasVarArgs) ->
            if isValidIdentifier name then generateFunction writer packageName name args hasVarArgs

        ignore <| exposedNames.Add(name)

//fprintfn writer "}"

let parseArgs (argv: string []) =
    Map.ofSeq [ for arg in argv ->
                    let idx = arg.IndexOf("=")

                    if idx < 0 then
                        let k = if arg.[0] = '/' then arg.Substring(1) else arg
                        k, k
                    else
                        let k = if arg.[0] = '/' then arg.Substring(1, idx - 1) else arg.Substring(0, idx)
                        let v = arg.Substring(idx + 1)
                        k, v ]

[<EntryPoint>]
let main argv =
    let args = parseArgs argv

    let outFile, packages =
        match (args.TryFind "outFile"), (args.TryFind "packages") with
        | Some outFile, Some packages -> outFile, packages
        | _ ->
            failwithf
                "Usage:\n\nRWrapperGenerator outFile=<outfilename> packages=<PackageNames>\nwhere PackageNames is a comma-separated list of R package names"

    use writer = new StreamWriter(outFile)

    fprintfn writer "%s" preamble

    let exposedNames = new HashSet<string>()

    fprintfn writer "\tpublic class R {"
    fprintfn writer "\t\tprivate static object[] emptyArr = new object[0];\n"

    Seq.iter (generatePackage writer exposedNames) (packages.Split([| ',' |], StringSplitOptions.RemoveEmptyEntries))

    fprintfn writer "\t}"
    fprintfn writer "}"

    0
