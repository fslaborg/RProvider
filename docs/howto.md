---
category: Documentation
categoryindex: 1
index: 3
---

# How to

## Printing R values to the console (F# interactive)

Add this line to your script to tell F# interactive how to print out
the values of R objects:

    [lang=fsharp]
    fsi.AddPrinter FSIPrinters.rValue

## Packages

### How do I Load a Package?

RProvider discovers the packages installed in your R installation and makes them available as packages under the RProvider root namespace.  The actual package is lazily loaded the first time you access it.  

### How do I install a new Package?

Currently you need to load up a real R session, then install the package via install.packages, or the Packages/Install Packages... menu.  You will then need to restart Visual Studio because the set of installed packages is cached inside the RProvider.

#### I have a package installed and it is not showing up
The most likely cause is that RProvider is using a different R installation from the one you updated.  When you install R, you get the option to update the registry key `HKEY_LOCAL_MACHINE\SOFTWARE\R-core` to point to the version you are installing.  This is what RProvider uses.  If you are running in a 32-bit process, RProvider uses `HKEY_LOCAL_MACHINE\SOFTWARE\R-core\R\InstallPath` to determine the path.  For 64-bit, it reads `HKEY_LOCAL_MACHINE\SOFTWARE\R-core\R64\InstallPath`.  When you install a package in a given version of R, it should be available in both the 32-bit and 64-bit versions.

## Function and Package names
There are a couple of mismatches between allowed identifiers between R and F#:
### Dots in names
It is pretty common in R to use a dot character in a name, because the character has no special meaning.  We remap dots to underscore, and underscore to a double-underscore.  So for example, data.frame() becomes R.data_frame().

### Names that are reserved F# keywords
Some package and function names are reserved words in F#.  For these, you will need to quote them using double-backquotes.  Typically, the IDE will do this for you.  A good example is the base package, which will require an open statement where "base" is double-back-quoted.
