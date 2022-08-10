(**
// can't yet format YamlFrontmatter (["category: Documentation"; "categoryindex: 1"; "index: 3"], Some { StartLine = 2 StartColumn = 0 EndLine = 5 EndColumn = 8 }) to pynb markdown

*)
#r "nuget: RProvider,{{package-version}}"
(**
# Passing Data Between F# and R

## Passing Parameters

### Parameter Passing Conventions

R supports various kinds of parameters, which we try to map onto equivalent F# parameter types:

 * All R formal parameters have names, and you can always pass their values either by name or positionally.  If you pass by name, you can skip arguments in your actual argument list.  We simply map these onto F# arguments, which you can also pass by name or positionally.

 * In R, essentially all arguments are optional (even if no default value is specified in the function argument list).  It's up to the receiving function to determine whether to error if the value is missing.   So we make all arguments optional.

 * R functions support ... (varargs/paramarray).  We map this onto a .NET ParamArray, which allows an arbitrary number of arguments to be passed.  However, there are a couple of kinks with this:

    * R allows named arguments to appear _after_ the ... argument, whereas .NET requires the ParamArray argument to be at the end.  Some R functions use this convention because their primary arguments are passed in the ... argument and the named arguments will sometimes be used to modify the behavior of the function.  From the RProvider you will to supply values for the positional arguments before you can pass to the ... argument.  If you don't want to supply a value to one of these arguments, you can explicitly pass System.Reflection.Missing.

    * Parameters passed to the R ... argument can also be passed using a name.  Those names are accessible to the calling function.  Example are list and dataframe construction (R.list, and R.data_frame).  To pass arguments this way, you can use the overload of each function that takes an IDictionary<string, obj>, either directly, or using the namedParams function.  For example:

        R.data_frame(namedParams [ "A", [|1;2;3|]; "B", [|4;5;6|] ])

### Parameter Types

Since all arguments to functions are of type obj, it is not necessarily obvious what you can pass.  Ultimately, you will need to know what the underlying function is expecting, but here is a table to help you.  When reading this, remember that for most types, R supports only vector types.  There are no scalar string, int, bool etc. types.

<table class="table table-bordered table-striped">
<tr><th>R Type</th><th>F#/.NET Type</th></tr>
<tr><td>character</td><td>string or string[]</td></tr>
<tr><td>complex</td><td>System.Numerics.Complex or Complex[]</td></tr>
<tr><td>integer</td><td>int or int[]</td></tr>
<tr><td>logical</td><td>bool or bool[]</td></tr>
<tr><td>numeric</td><td>double or double[]</td></tr>
<tr><td>list</td><td>Call R.list, passing the values as separate arguments</td><tr>
<tr><td>dataframe</td><td>Call R.data_frame, passing column vectors in a dictionary</td><tr>
</table>

**NB**: For any input, you can also pass a SymbolicExpression instance you received as the result of calling another R function.  Doing so it a very efficient way of passing data from one function to the next, since there is no marshalling between .NET and R types in that case.

### Creating and passing an R function
R has some high-level functions (e.g. sapply) that require a function parameter. Although F# has first-class support of functional programming and provides better functionality and syntax for apply-like operations, which often makes it sub-optimal to call apply-like high-level functions in R, the need for parallel computing in R, which is not yet directly supported by F# parallelism to R functions, requires users to pass a function as parameter. Here is an example way to create and pass an R function:

*)
let fun1 = R.eval(R.parse(text="function(i) {mean(rnorm(i))}"))
let nums = R.sapply(R.c(1,2,3),fun1)
(**
The same usage also applies to parallel apply functions in parallel package.

## Accessing results

Functions exposed by the RProvider return an instance of `RDotNet.SymbolicExpression`.  This keeps all return data inside R data structures, so does not impose any data marshalling overhead.  If you want to pass the value in as an argument to another R function, you can simply do so.

In order to access the result in .NET code, you have three routes:

### Convert the data into a specified .NET type via GetValue<type>()

RProvider adds a generic `GetValue<'T>` extension method to `SymbolicExpression`.  This supports conversions from certain R values to specific .NET types.  Here are the currently supported conversions:

<table class="table table-bordered table-striped">
<tr><th>R Type</th><th>Requested F#/.NET Type</th></tr>
<tr><td>character (when vector is length 1)</td><td>string</td></tr>
<tr><td>character</td><td>string[]</td></tr>
<tr><td>complex (when vector is length 1)</td><td>Complex</td></tr>
<tr><td>complex</td><td>Complex[]</td></tr>
<tr><td>integer (when vector is length 1)</td><td>int</td></tr>
<tr><td>integer</td><td>int[]</td></tr>
<tr><td>logical (when vector is length 1)</td><td>bool</td></tr>
<tr><td>logical</td><td>bool[]</td></tr>
<tr><td>numeric (when vector is length 1)</td><td>double</td></tr>
<tr><td>numeric</td><td>double[]</td></tr>
</table>

Custom conversions can be supported through [plugins](plugins.html).

### Convert the data into the default .NET type the .Value property

We also expose an extension property called Value that performs a _default_ conversion of a SymbolicExpresion to a .NET type.  These are the current conversions:

<table class="table table-bordered table-striped">
<tr><th>R Type</th><th>F#/.NET Type</th></tr>
<tr><td>character</td><td>string[]</td></tr>
<tr><td>complex</td><td>Complex[]</td></tr>
<tr><td>integer</td><td>int[]</td></tr>
<tr><td>logical</td><td>bool[]</td></tr>
<tr><td>numeric</td><td>double[]</td></tr>
</table>

Again, custom conversions can be supported through [plugins](plugins.html).

### Explicitly access the data in the SymbolicExpression

If there are no supported conversions, you can access the data through the RDotNet object model.  RDotNet exposes properties, members and extension members (available only if you open the RDotNet namespace) that allow you to access the underlying data directly.  So, for example:

*)
let res = R.sum([|1;2;3;4|])
if res.Type = RDotNet.Internals.SymbolicExpressionType.IntegerVector then res.AsInteger().[0]
else failwithf "Expecting a Numeric but got a %A" res.Type
(**
To make this easier, we have defined some active patterns, under the RProvider.Helpers namespace, which is auto-opened when you open the RProvider namespace.  These combine the type tests and conversion.  An equivalent example:

*)
match R.sum([|1;2;3;4|]) with 
| IntegerVector(iv) -> iv.[0]
| _                 -> failwithf "Expecting a Numeric but got a %A" res.Type
(**
## What if I commonly need an argument or result conversion that RProvider does not support?

If you believe the argument conversion is universally appropriate and should be available to everybody, please fork the repo and submit a pull request.

RProvider also supports custom conversions to/from your own data types using [plugins](plugins.html).

*)

