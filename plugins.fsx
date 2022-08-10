(**
// can't yet format YamlFrontmatter (["category: Developer"; "categoryindex: 2"; "index: 3"], Some { StartLine = 1 StartColumn = 0 EndLine = 4 EndColumn = 8 }) to pynb markdown

# Plugins

RProvider supports plugins to support custom functionality.  It uses [MEF](http://msdn.microsoft.com/en-us/library/dd460648.aspx) to load plugins that export certain contracts.  See below for examples.

Before implementing a plugin you should consider whether your conversion is universally applicable and should be added to the core conversion logic in the provider.  If so, please log as an issue, and ideally fork the repo and submit a pull request.

## Supporting an implicit parameter conversion for a datatype 

## Supporting an explicit result conversion for a datatype

## Supporting a default result conversion
*)

