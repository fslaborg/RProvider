(*** hide ***)
// Include the right directories so that the documentation tool tips work
#nowarn "211" // Ignore warning that a search path does not exist on #I
#I "../../bin/"

(** 
Using R provider on Mac and Linux
=================================

The most recent version of the R type provider can be used on Mac and Linux using
Mono. However, there is a bit of setup that you need to go through first. This document
describes the necessary steps for using R provider on Mac using Xamarin Studio, but
it should be easily adaptable for other configuration. If no, please [edit this 
file](https://github.com/BlueMountainCapital/FSharpRProvider/blob/master/docs/content/mac-and-linux.fsx)
to add more details!

In summary, you need the following:

 - Download and build 64 bit version of Mono, because the default installation is
   32 bit and R provider only works with 64 bit version.
 - Tell Xamarin Studio to run 64 bit version of F# Interactive (the main IDE
   will still run as 32 bit, which is fine).
 - Create a file `~/.rprovider.conf` with configuration that tells R provider
   where to find 64 bit version of mono.

This page is based on [this excellent write-up on running R.NET on 
Mac](http://rawgit.com/evelinag/Projects/master/RDotNetOnMac/output/RDotNetOnMac.html) by 
[Evelina Gabasova](http://evelinag.com/). The page has some more details and hints that
you may need if the simplified version below does not work for you.

Installing 64 bit version of Mono
---------------------------------

First of all, you need 64 bit version of Mono. To do this, you'll need command line tools
`autoconf`, `automake` and `libtool`. Probably the easiest way to do this is to install
[Homebrew](http://brew.sh/) (but feel free to use your favorite tool) and then run:

    [lang=text]
    brew install automake
    brew install autoconf
    brew install libtool

Now you can use `git` to get the latest version of Mono from GitHub and install it. The 
following installs it into `/usr/local/mono64` (feel free to change this too):

    [lang=text]
    export MONO_PREFIX=/usr/local/mono64
    git clone https://github.com/mono/mono.git
    cd mono
    ./autogen.sh --prefix=$MONO_PREFIX --disable-nls  
    make
    make install

You might get a timeout from `git` when running the `autogen`-command, if you are behind
a firewall. To fix this just run the following, which will force `git` to clone using
"https://" instead of "git://":

    [lang=text]
    git config --global url."https://".insteadOf git://

Now we can run Mono in 64-bit explicitly using `/usr/local/mono64/bin/mono`. Next, we need 
to create a launcher script that will start F# Interactive in 64 bit. 

Running F# Interactive in 64 bit
--------------------------------

The F# Interactive launcher that comes
with standard installation of Mono is `fsharpi`. On Mac, you can find it (depending on your
Mono version) in a folder like `/Library/Frameworks/Mono.framework/Versions/3.4.0/bin/`.

Create a copy of this file called, for example, `fsharpi64` (in the same folder) and change
the bit that points to the Mono installation, so that it points to the newly installed 64 bit
version. Also, change `fsi.exe` to `fsiAnyCpu.exe` at the end. The whole thing should be on
a single line, but the following shows it with newlines for better readability:

    [lang=bash]
    $EXEC /usr/local/mono64/bin/mono  
        $DEBUG $MONO_OPTIONS 
        /Library/Frameworks/Mono.framework/Versions/3.4.0/lib/mono/4.0/fsiAnyCpu.exe 
        --exename:$(basename $0) "$@"

When you start Xamarin Studio, you can now tell it to use `fsharpi64` instead of `fsharpi`.
To do this, go to `Preferences > Other > F# Settings > F# interactive`. Once you change this,
you can test which version of F# Interactive you're running by entering `System.IntPtr.Size` 
(if the result is 4, you're running 32 bit; if the result is 8, you're on a 64 bit).

Configuring R provider
----------------------

Finally, you need to tell the R provider where to find the 64 bit installation on Mono (the
R provider starts a background process to communicate with R using the 64 bit version). To 
do that, create a file `~/.rprovider.conf` (that is, in your home folder) containing a single
line `MONO64=/usr/local/mono64/bin/mono`.

You can create the file using the following command:

    [lang=text]
    echo MONO64=$MONO_PREFIX/bin/mono > ~/.rprovider.conf

And this is all you should need! One more thing to check is to make sure that R is in your
PATH (and the R provider will be able to find it). To do that, open Terminal and type
`R --print-home`. This command should print the home folder and is used by the R type
provider. 

If calling `R --print-home` does not work, you'll need to create an environment variable
`R_HOME` and point it to the R home folder. On Mac, this is something like 
`/Library/Frameworks/R.framework/Resources` (check that `$R_HOME/lib/libR.dylib` exists).

Testing the R provider
----------------------

Now you can open Xamarin studio and start playing with the R type provider. The easiest way
to do that is to create a new F# Tutorial, add a new file (say `Test.fsx`) and reference
R provider using NuGet (right click on the project `Add > Add Packages...` and search
for "rprovider").

Now, type the following in the script file (with the correct R provider version):
*)
#nowarn "211"
#I "packages/RProvider.1.1.4"
#load "RProvider.fsx"

open RProvider
open RProvider.graphics
open RProvider.grDevices
open RProvider.datasets
(**
The `#load` command loads the R provider. The first line disables warnings about unnecessary 
folder references (a few are generated by the loader script). Finally, the `open` declarations
open a number of standard R packages.

Now we can run some calculations and create charts. When using R on Mac, the default graphics
device (Quartz) sometimes hangs, but X11 is working without issues, so the following uses X11:
*)
R.x11()

// Calculate sin using the R 'sin' function
// (converting results to 'float') and plot it
[ for x in 0.0 .. 0.1 .. 3.14 -> 
    R.sin(x).GetValue<float>() ]
|> R.plot

// Plot the data from the standard 'Nile' data set
R.plot(R.Nile)
(**
Diagnostics and debugging
-------------------------

If you encounter any issues, please do not hesitate to submit an issue! You can do that on the
[GitHub page](https://github.com/BlueMountainCapital/FSharpRProvider/issues). Before submitting
an issue, please see the [Diagnostics and debugging page](diagnostics.html), which tells you how
to create a log file with more detailed information about the issues.
*)






