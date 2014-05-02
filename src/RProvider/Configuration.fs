module RProvider.Configuration

open System
open System.IO
open System.Reflection
open System.Configuration
open System.Collections.Generic

/// Returns the Assembly object of RProvider.dll (this needs to
/// work when called from RProvider.dll and also RProvider.Runtime.dll)
let getRProviderAssembly() =
  AppDomain.CurrentDomain.GetAssemblies()
  |> Seq.find (fun a -> a.FullName.StartsWith("RProvider.Runtime,"))

/// Finds directories relative to 'dirs' using the specified 'patterns'.
/// Patterns is a string, such as "..\foo\*\bar" split by '\'. Standard
/// .NET libraries do not support "*", so we have to do it ourselves..
let rec searchDirectories patterns dirs = 
  match patterns with 
  | [] -> dirs
  | "*"::patterns ->
      dirs |> List.collect (Directory.GetDirectories >> List.ofSeq)
      |> searchDirectories patterns
  | name::patterns -> 
      dirs |> List.map (fun d -> Path.Combine(d, name))
      |> searchDirectories patterns

/// Reads the 'RProvider.dll.config' file and gets the 'ProbingLocations' 
/// parameter from the configuration file. Resolves the directories and returns
/// them as a list.
let getProbingLocations() = 
  try
    let root = getRProviderAssembly().Location
    let config = System.Configuration.ConfigurationManager.OpenExeConfiguration(root)
    let pattern = config.AppSettings.Settings.["ProbingLocations"]
    if pattern <> null then
      [ let pattern = pattern.Value.Split(';', ',') |> List.ofSeq
        for pat in pattern do 
          let roots = [ Path.GetDirectoryName(root) ]
          for dir in roots |> searchDirectories (List.ofSeq (pat.Split('/','\\'))) do
            if Directory.Exists(dir) then yield dir ]
    else []
  with :? ConfigurationErrorsException | :? KeyNotFoundException -> []

