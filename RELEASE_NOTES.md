* 2.0.3 - Include built-in FSI printer module
* 2.0.2 - Breaking change: Replace "." with "_" in package names (#245) 
* 2.0.1 - .NET5, R 4.x, and Apple Silicon support
* 2.0.1-beta3 - Apple Silicon support
* 2.0.1-beta2 - Re-enable plugins on .NET5 release (fixes probing locations)
* 2.0.1-beta - Test package publish
* 2.0.0-beta - .NET 5 support with R 4.x compatability
* 1.1.22 - Add ProbingLocations for latest Deedle.RPlugin
* 1.1.21 - Fix ProbingLocations (#199)
* 1.1.20 - Cross-platform improvements
* 1.1.19-alpha - Fix FSharp.Core.dll included in the package
* 1.1.18-alpha - Allow specifying of R path in .rprovider.conf (fix #165)
* 1.1.17 - Fix RProvider.fsx (#166), Mac loading and update dependenices
* 1.1.16-alpha - Load correct dependencies in RProvider.fsx (fix #166)
* 1.1.15 - Disable R.NET AutoPrint (fix #161 and perhaps #160)
* 1.1.14 - Improve Linux compatibility - try searching for libR.so (#157)
* 1.1.13 - Skip assembly resolution for mscorlib.resources (avoids recursive lookup error)
* 1.1.12 - Include transitive dependency on DynamicInterop
* 1.1.11 - Update to R.NET 1.6.4 (support R version 2.14.1)
* 1.1.10 - Update NuGet package to depend on R.NET 1.6.3
* 1.1.9 - Update to R.NET 1.6.3
* 1.1.8 - Simplify load script (RProvider.fsx), improve logging
* 1.1.6 - Mono support & use and support Paket + minor improvements
* 1.1.5-alpha - Downgrade R.NET version and update load script
* 1.1.4-alpha - Fix load script in the NuGet package
* 1.1.3-alpha - Fix remaining Mono issues and add documentation
* 1.1.2-alpha - Reference ILRepack through NuGet, but disable it (breaks on Mono)
* 1.1.1-alpha - Support Paket project structure, Fix ILMerge issues
* 1.1.0-alpha - ILMerge FSharp.Core and experimental Mac support
* 1.0.17 - Fix shadow copying when referenced via a NuGet package
* 1.0.16 - Fix shadow copying (#122) and require specific R.NET version
* 1.0.15 - Fix bad upload to NuGet.org
* 1.0.14 - Fix the module clash error in FsLab (#46). Fix assembly resolution (#117). Update NuGET and automatically update FAKE(#116).
* 1.0.13 - Fix the Print extension method
* 1.0.12 - Use correct folders in NuGet package
* 1.0.11 - Bug fixes (include FSharp.Core, fix resource resolution).
* 1.0.10 - GC fixes in R.NET
* 1.0.9 - Out-of-process execution, RData type provider, bug fixes
* 1.0.8-alpha - Execute R in a separate process in the type provider
* 1.0.7-alpha - Fix handling of missing configuration key
* 1.0.6-alpha - Fixed assembly resolution when installed via NuGet
* 1.0.5 - Update load script in the NuGet package, improve cross-platform support
* 1.0.4 - Improve stability (refactor initialization), add documentation
* 1.0.3 - Fixed NuGet package
