(**
// can't yet format YamlFrontmatter (["category: Developer"; "categoryindex: 2"; "index: 3"], Some { StartLine = 1 StartColumn = 0 EndLine = 4 EndColumn = 8 }) to pynb markdown

Diagnostics and debugging
=========================

The R type provider has an extensive logging to help developers diagnose
potential issues. If you encounter any issues with the R type provider, this
page gives you all the information you need to create a log file with detailed
trace of what is going one. This may give you some hints on what is wrong & a
detailed report that you can send when [submitting an
issue](https://github.com/fslaborg/RProvider/issues).

**TL;DR** The logging is enabled by setting an environment variable
`RPROVIDER_LOG` to a file name where the log should be saved. The file does
not have to exist, but the folder where it is located has to. **You should use
an absolute (full) path, as otherwise the server will create a seperate log in the
nuget package directory.**

Enabling logging on Windows
---------------------------

On Windows, you can set environment variables by going to system properties
(this varies depending on the OS version, but generally right click on
"My Computer" and select a link or button saying something like "Change settings").

This should open a new dialog, where you can go to "Advanced", and click on the
"Environment Variables" button. Here, you can add the variable as either per-user
or per-system and save it. For example, create a folder `C:\Temp` and set
`RPROVIDER_LOG` to `C:\Temp\rlog.txt`. After you restart Visual Studio, the
R provider will start logging.

Enabling logging on Mac/Linux
-----------------------------

If you're using Xamarin Studio on Mac, then the easiest option is to set the
variable from Terminal and then start Xamarin Studio from terminal. Note that
if you set the environment variable from terminal, but launch Xamarin Studio
from Dock or in some other way, it will not see the variable!

The following should do the trick (assuming the folder `/Users/tomasp/Temp` exists):

    [lang=text]
    export RPROVIDER_LOG=/Users/tomasp/Temp/rlog.txt
    open -n /Applications/Xamarin\ Studio.app/

This will set the variable and start a new instance of Xamarin Studio in the current
context. Once it appears, reporduce the operation that causes the error, close
Xamarin Studio and look at the log file.

Enabling logging in a custom build
----------------------------------

If you're building R provider from source, you can also enable logging by changing
the `loggingEnabled` constant in the source code (and change `logFile` if you want
to override the default location). See the [right place for this on
GitHub](https://github.com/fslaborg/RProvider/blob/master/src/RProvider/Logging.fs#L13).

*)

