(**
# Diagnostics and debugging

The R type provider uses a common logging system across all of its components.
The design-time, runtime, and server instances all log to a common log file.

The logfile is disabled by default. It is enabled by setting the environment
variable `RPROVIDER_LOG` to `true` (/ `on` / `1`).

If RProvider is not working correctly, the log file may give you some hints on what is wrong and provide details
that you can send when [submitting an issue](https://github.com/fslaborg/RProvider/issues).

## Log location

The rprovider.log file will be created and appended
to depending on the OS it is run on. On macOS, the log will also display in the Console app that is an included macOS utility.

* macOS: ~/Library/Logs/\RProvider/rprovider.log

* Windows: %LOCALAPPDATA%\RProvider\rprovider.log

* Linux: ~/.local/state/RProvider/rprovider.log

## To enable logging

Logging is controlled by a single environment variable:

```bash
RPROVIDER_LOG=true
```

### Windows

To set on Windows:
1. Open System Properties → Advanced → Environment Variables
2. Under User variables, click New…
3. Add:

```
Name:  RPROVIDER_LOG
Value: true
```
1. Restart Visual Studio / Code / your IDE so that it picks up the new environment variable.

### macOS

On macOS, apps do not inherit Terminal environment variables. You must therefore set the environment variable and launch your IDE from the same terminal session. For example:

```bash
export RPROVIDER_LOG=true
open -n /Applications/Visual\ Studio\ Code.app
```

### Linux

Similar to macOS, to set temporarily and launch VS Code:

```bash
export RPROVIDER_LOG=true
code .
```

*)