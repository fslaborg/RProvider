/// [omit]
module RProvider.Internal.Configuration

open System
open System.IO
open System.Reflection
open System.Collections.Generic
open System.Xml

/// Returns the Assembly object of RProvider.Runtime.dll (this needs to
/// work when called from RProvider.DesignTime.dll and also RProvider.Server.exe/dll)
let getRProviderRuntimeAssembly () =
    AppDomain.CurrentDomain.GetAssemblies() |> Seq.find (fun a -> a.FullName.StartsWith("RProvider.Runtime,"))

/// Finds directories relative to 'dirs' using the specified 'patterns'.
/// Patterns is a string, such as "..\foo\*\bar" split by '\'. Standard
/// .NET libraries do not support "*", so we have to do it ourselves..
let rec searchDirectories patterns dirs =
    match patterns with
    | [] -> dirs
    | "*" :: patterns -> dirs |> List.collect (Directory.GetDirectories >> List.ofSeq) |> searchDirectories patterns
    | name :: patterns -> dirs |> List.map (fun d -> Path.Combine(d, name)) |> searchDirectories patterns

/// Returns the real assembly location - when shadow copying is enabled, this
/// returns the original assembly location (which may contain other files we need)
let getAssemblyLocation (assem: Assembly) =
    if AppDomain.CurrentDomain.ShadowCopyFiles then (Uri(assem.Location)).LocalPath else assem.Location

/// Returns the real config file location even when shadow copying is enabled.
/// To account for single-file server executables, we use AppContext.BaseDirectory
/// and navigate up two directories to get to the original RProvider.Runtime.dll
/// location where the config file is (from server/{platform}/)
let getConfigFileLocation (assem: Assembly) =
    if String.IsNullOrEmpty assem.Location then
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"../../", assem.GetName().Name + ".dll.config"))
    else
        getAssemblyLocation assem + ".config"

/// Load a .config XML file and obtain the value of the AppSetting
/// called 'ProbingLocations'.
let probingLocationsFromXmlConfig file =
    if not <| File.Exists file then
        Error "Configuration file missing"
    else
        let doc = XmlDocument()
        doc.LoadXml(File.ReadAllText file)
        let appSettings = doc.SelectSingleNode "//appSettings"
        let setting = appSettings.SelectSingleNode "//add[@key='ProbingLocations']"

        if isNull setting then
            Error "Appsetting not set: ProbingLocations"
        else
            let locations = setting.Attributes.GetNamedItem("value")
            if isNull locations then Error "Appsetting not set: ProbingLocations" else locations.Value |> Ok

/// Reads the 'RProvider.dll.config' file and gets the 'ProbingLocations'
/// parameter from the configuration file. Resolves the directories and returns
/// them as a list.
let getProbingLocations () =
    try
        let configLocation = getRProviderRuntimeAssembly () |> getConfigFileLocation
        Logging.logf "RProvider configuration file is %s" configLocation

        if String.IsNullOrEmpty configLocation then
            []
        else
            Logging.logf "Attempting to load config file '%s'" configLocation
            let pattern = probingLocationsFromXmlConfig configLocation

            match pattern with
            | Ok pattern ->
                [ let pattern = pattern.Split(';', ',') |> List.ofSeq

                  for pat in pattern do
                      let roots = [ Path.GetDirectoryName(configLocation) ]

                      for dir in roots |> searchDirectories (List.ofSeq (pat.Split('/', '\\'))) do
                          if Directory.Exists(dir) then yield dir ]
            | Error _ -> []
    with
    | :? KeyNotFoundException -> []

/// Given an assembly name, try to find it in either assemblies
/// loaded in the current AppDomain, or in one of the specified
/// probing directories.
let resolveReferencedAssembly (asmName: string) =

    // Do not interfere with loading FSharp.Core resources, see #97
    // This also breaks for "mscorlib.resources" and so it might be good idea to skip all
    // resources (both short format "foo.resources" and long format "foo.resources, Version=4.0.0.0...")
    if asmName.EndsWith ".resources" || asmName.Contains ".resources," then
        (* Do not log when we skip, because that would cause recursive lookup for mscorlib.resources *) null
    else
        Logging.logf "Attempting resolution for '%s'" asmName

        // First, try to find the assembly in the currently loaded assemblies
        let fullName = AssemblyName(asmName)

        let loadedAsm =
            AppDomain.CurrentDomain.GetAssemblies()
            |> Seq.tryFind (fun a -> AssemblyName.ReferenceMatchesDefinition(fullName, a.GetName()))

        match loadedAsm with
        | Some asm -> asm
        | None ->

            // Otherwise, search the probing locations for a DLL file
            let libraryName =
                let idx = asmName.IndexOf(',')
                if idx > 0 then asmName.Substring(0, idx) else asmName

            let locations = getProbingLocations ()
            Logging.logf "Probing locations: %s" (String.concat ";" locations)

            let asm =
                locations
                |> Seq.tryPick
                    (fun dir ->
                        let library = Path.Combine(dir, libraryName + ".dll")

                        if File.Exists(library) then
                            Logging.logf "Found assembly, checking version! (%s)" library
                            // We do a ReflectionOnlyLoad / GetAssemblyName so that we can check the version
                            let refAssem = AssemblyName.GetAssemblyName(library)
                            // If it matches, we load the actual assembly
                            if refAssem.FullName = asmName then
                                Logging.logf "...version matches, returning!"
                                Some(Assembly.LoadFrom(library))
                            else
                                Logging.logf "...version mismatch, skipping"
                                None
                        else
                            None)

            if asm = None then Logging.logf "Assembly not found!"
            defaultArg asm null

let isUnixOrMac () =
    let platform = Environment.OSVersion.Platform
    // The guide at www.mono-project.com/FAQ:_Technical says to also check for the
    // value 128, but that is only relevant to old versions of Mono without F# support
    platform = PlatformID.MacOSX || platform = PlatformID.Unix

/// On Mac (and Linux), we use ~/.rprovider.conf in user's home folder for
/// various configuration (64-bit mono and R location if we cannot determine it)
let getRProviderConfValue key =
    Logging.logf "getRProviderConfValue '%s'" key

    if isUnixOrMac () then
        let home = Environment.GetEnvironmentVariable("HOME")

        try
            Logging.logf "getRProviderConfValue - Home: '%s'" home
            let config = home + "/.rprovider.conf"

            File.ReadLines(config)
            |> Seq.tryPick
                (fun line ->
                    match line.Split('=') with
                    | [| key'; value |] when key' = key -> Some value
                    | _ -> None)
        with
        | _ -> None
    else
        None
