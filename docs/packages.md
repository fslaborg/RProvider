---
title: R Packages and Environments
category: Guides
categoryindex: 4
index: 3
---

# R Packages and Environments

RProvider currently accesses the globally installed R, as specified by the `R_HOME` environment variable. A near-term aim is for the provider to access a local `renv`-based package environment, but this requires further development.

## Accessing packages

RProvider discovers the packages installed in your R installation and their functions and lazy data available as packages under the RProvider root namespace.

Packages are *not* loaded when their namesapce is opened. If your package requires loading for side-effects, load it using `R.library("somelib")`.

## Installing packages

Currently you need to load up a real R session, then install the package via `install.packages`. You will then need to restart your IDE because the set of installed packages is discovered when RProvider first loads.

#### Q. I have a package installed and it is not showing up
The most likely cause is that RProvider is using a different R installation from the one you updated. See [installation](installation.html) for more information.

## Function and Package names

There are a couple of mismatches between allowed identifiers between R and F#:

### Dots in names

It is pretty common in R to use a dot character in a name, because the character has no special meaning.  We remap dots to underscore, and underscore to a double-underscore.  So for example, data.frame() becomes R.data_frame().

### Names that are reserved F# keywords

Some package and function names are reserved words in F#.  For these, you will need to quote them using double-backquotes.  Typically, the IDE will do this for you.  A good example is the base package, which will require an open statement where "base" is double-back-quoted.
