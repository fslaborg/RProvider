namespace RProvider.Server

open System
open RProvider.Common
open RProvider.Runtime
open RProvider.Runtime.RInterop

/// Event loop (see below) can either perform some work item or stop
type internal EventLoopMessage =
    | Run of (unit -> unit)
    | Stop

/// The REngine can only be safely accessed from a single thread
/// and on Mac, this has to be the main thread of the application.
///
/// So, this application implements a simple event loop (running
/// once everything is setup) that picks REngine operations from a
/// concurrent queue (the RInteropServer instance initialized via
/// .NET remoting sends things here) and processes them.
module internal EventLoop =
    let queue = new System.Collections.Concurrent.BlockingCollection<_>()

    /// Start the event loop - this should be called
    let startEventLoop () =
        LogFile.logf "server event loop: starting"

        try
            let mutable running = true

            while running do
                match queue.Take() with
                | Run f ->
                    LogFile.logf "server event loop: got work item"
                    f ()
                | Stop ->
                    LogFile.logf "server event loop: got stop command"
                    running <- false
        with
        | e -> LogFile.logf "server event loop: failed with %A" e

    /// Run a server command (that accesses REngine) safely in the event loop.
    /// This sends a command to the event loop & propagates exceptions
    let runServerCommandSafe f =
        LogFile.logf "Adding work item to queue"
        use evt = new System.Threading.AutoResetEvent(false)
        LogFile.logf "Debug 1"
        let result = ref (Choice1Of3())
        LogFile.logf "Debug 2"

        // Add function with exception handling to the queue & wait for result
        queue.Add(
            Run
                (fun () ->
                    LogFile.logf "In queue"

                    try
                        try
                            result.Value <- Choice2Of3(f ())
                        with
                        | ex ->
                            let ex = if ex.GetType().IsSerializable then ex else Exception(ex.Message)
                            result.Value <- Choice3Of3(ex)
                    finally
                        evt.Set() |> ignore)
        )

        evt.WaitOne() |> ignore

        match result.Value with
        | Choice1Of3 () -> failwith "logic error: Item in the queue was not processed"
        | Choice2Of3 res -> res
        | Choice3Of3 ex ->
            LogFile.logf "There was an exception in the loop"
            raise ex


/// Server object that is exposed via remoting and is called by the editor
/// to get information about R (packages, functions, RData files etc.)
type RInteropServer() =
    interface IRInteropServer with

        member __.InitializationErrorMessage() =
            // No need for event loop here, because this is initialized
            // when the event loop starts (so initResult has value now)
            match Singletons.rLocation.Value with
            | None -> "Error: could not locate an R install"
            | Some _ -> ""

        member __.GetPackages() = EventLoop.runServerCommandSafe getPackages

        member __.LoadPackage package = EventLoop.runServerCommandSafe <| fun () -> loadPackage package

        member __.GetBindings package = EventLoop.runServerCommandSafe <| fun () -> getBindings package

        member __.GetFunctionDescriptions(package: string) =
            EventLoop.runServerCommandSafe <| fun () -> getFunctionDescriptions package

        member __.GetPackageDescription package =
            EventLoop.runServerCommandSafe <| fun () -> getPackageDescription package

        member __.GetRDataSymbols file =
            EventLoop.runServerCommandSafe
            <| fun () ->
                file
                |> REnv.loadRDataFile
                |> REnv.getDataSymbols