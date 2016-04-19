(*** hide ***)
// Include the right directories so that the documentation tool tips work
#nowarn "211" // Ignore warning that a search path does not exist on #I
#I "../../bin/"

(** 
Using R provider on Mac and Linux
=================================

The most recent versions of the R type provider can be used on Mac and Linux using
Mono. However, there is a bit of setup that you need to go through first. This page
describes the necessary steps for using R provider on Mac using Xamarin Studio, but
it should be easily adaptable for other configuration. If no, please [edit this 
file](https://github.com/BlueMountainCapital/FSharpRProvider/blob/master/docs/content/mac-and-linux.fsx)
to add more details!

In summary, you need the following:

 - Prerequisite: install R from [R-project.org](https://www.r-project.org/)
 - Install 64 bit version of Mono, because the default installation is
   32 bit and R provider only works with 64 bit version.
 - Tell Xamarin Studio to run 64 bit version of the F# compiler and F# Interactive.
 - Create a file `~/.rprovider.conf` with configuration that tells R provider
   where to find 64 bit version of mono and where to find the R installation.

Installing 64 bit Mono
----------------------
Mono currently offers a preview of a [Universal package](http://www.mono-project.com/download/#download-mac) 
for installing of 64 bit version of Mono. Download the *Mono Universal Installer*
and follow the installation instructions. After installation, you can run the 64 bit version of
Mono using the command
	
	[lang=bash]
	mono64

If you want to build 64 bit Mono manually, there are instructions at the bottom of this page.

Running F# in 64 bit
--------------------------------
The standard launcher for F# compiler under Mono is `fsharpc`, and the launcher for F# Interactive
is `fsharpi`. On a Mac they are installed under `/usr/local/bin/`, and they point to the Mono
installation folder (something like `/Library/Frameworks/Mono.framework/Versions/4.2.1/bin/`).

Create copies of the two files by running the following commands: 

	[lang=bash]
	sudo cp /usr/local/bin/fsharpi /usr/local/bin/fsharpi64
	sudo cp /usr/local/bin/fsharpc /usr/local/bin/fsharpc64

We'll use them to create the 64 bit launch scripts (if you don't have
admin privileges, copy the files to some other location). 
Now open the `fsharpi64` file and edit the last line to use `mono64` instead of `mono`. 
The result should look like the following:

	[lang=bash]
	$EXEC /Library/Frameworks/Mono.framework/Versions/Current/bin/mono64 \
		$DEBUG $MONO_OPTIONS \
		/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.5/fsi.exe \
		--exename:$(basename "$0") "$@"
 
*Optional*: you can also change the folder name pointing to a specific version of Mono
to the `Current` version. This should keep the launch script working even if you update Mono. 
The code above uses the `Current` version of Mono.

Now you can test the installation by running `fsharpi64` in the Terminal and typing in

	[lang=text]
	System.IntPtr.Size;;

If the result is 8, then all is working correctly and you are running the 64 bit version.
If the result is 4, then something went wrong and you are still using the 32 bit version
of Mono.

Now repeat the same steps with `fsharpc64` launcher file to change `mono` to `mono64`. 
The last line in the launch script should look like the following:

	[lang=bash]
	$EXEC /Library/Frameworks/Mono.framework/Versions/Current/bin/mono64 \ 
		$DEBUG $MONO_OPTIONS \
		/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.5/fsc.exe \
		--exename:$(basename "$0") "$@"

If you are using Xamarin Studio, you can now tell it to use `fsharpi64` instead of `fsharpi`,
and `fsharpc64` instead of `fsharpc`. To do this, go to `Preferences > Other > F# Settings`. 

* Change the default F# interactive path to the path `/usr/local/bin/fsharpi64`.
* Change the default F# compiler path to `/usr/local/bin/fsharpc64`. 

Now restart Xamarin Studio for the changes to take place. 
You can again test which version of F# Interactive you're running 
by entering `System.IntPtr.Size` 
(if the result is 4, you're running 32 bit; if the result is 8, you're on a 64 bit).

Configuring R provider
----------------------

Finally, you need to tell the R provider where to find the 64 bit installation of Mono (the
R provider starts a background process to communicate with R using the 64 bit version). To 
do that, we need to create a file `~/.rprovider.conf` (that is, in your home folder) containing 
the location of `mono64` and location of your R installation in the `MONO64` and `R_HOME` variables.  

    [lang=text]
    echo -e "MONO64=`which mono64`\nR_HOME=`R --print-home`" > ~/.rprovider.conf

And this is all you should need! The command assumes that you have both `mono64` and
`R` in your `PATH`. Check the `~/.rprovider.conf` file
if the locations of `mono64` and R were generated correctly. The
`R_HOME` variable should point to the folder holding your R installation. 

If calling `R --print-home` did not work, you'll need to edit the environment variable
`R_HOME` and point it to the R home folder on your system. On Mac, this is something like 
`/Library/Frameworks/R.framework/Resources` (check that `$R_HOME/lib/libR.dylib` exists).

Testing the R provider
----------------------

Now you can open Xamarin Studio and start playing with the R type provider. The easiest way
to do that is to create a new F# Tutorial, add a new file (say `Test.fsx`) and reference
R provider using NuGet (right click on the project `Add > Add Packages...` and search
for "rprovider").

Now, type the following in the script file (with the correct R provider version):
*)
#nowarn "211"
#I "packages/RProvider.1.1.19"
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
// basic test if RProvider works correctly
R.mean([1;2;3;4])
// val it : RDotNet.SymbolicExpression = [1] 2.5

// testing graphics
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

Note: Manual installation 64 bit version of Mono
------------------------------------------
The Mono project currently offers a [preview version](http://www.mono-project.com/download/#download-mac) 
of an installer for 64 bit version of Mono. If you however need to install Mono manually, 
the following section describes the necessary steps.

This page is based on [this write-up on running R.NET on 
Mac](http://rawgit.com/evelinag/Projects/master/RDotNetOnMac/output/RDotNetOnMac.html) by 
[Evelina Gabasova](http://evelinag.com/). The page has some more details and hints that
you may need if the simplified version below does not work for you.

### Installing 64 bit version of Mono

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

Now we can run Mono in 64-bit explicitly using `/usr/local/mono64/bin/mono`. 
*)






