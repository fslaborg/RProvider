namespace RProvider.Server

open Microsoft.FSharp.Core.CompilerServices
open Microsoft.Win32
open RDotNet
open RProvider
open RProvider.RInterop
open RProvider.Internal
open System

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
        Logging.logf "server event loop: starting"

        try
            let initResultValue = RInit.rHomePath.Force()
            let mutable running = true

            while running do
                match queue.Take() with
                | Run f ->
                    Logging.logf "server event loop: got work item"
                    f ()
                | Stop ->
                    Logging.logf "server event loop: got stop command"
                    running <- false
        with
        | e -> Logging.logf "server event loop: failed with %A" e

    /// Run a server command (that accesses REngine) safely in the event loop.
    /// This sends a command to the event loop & propagates exceptions
    let runServerCommandSafe f =
        Logging.logf "Adding work item to queue"
        use evt = new System.Threading.AutoResetEvent(false)
        Logging.logf "Debug 1"
        let result = ref (Choice1Of3())
        Logging.logf "Debug 2"

        // Add function with exception handling to the queue & wait for result
        queue.Add(
            Run
                (fun () ->
                    Logging.logf "In queue"

                    try
                        try
                            result := Choice2Of3(f ())
                        with
                        | ex ->
                            let ex = if ex.GetType().IsSerializable then ex else Exception(ex.Message)
                            result := Choice3Of3(ex)
                    finally
                        evt.Set() |> ignore)
        )

        evt.WaitOne() |> ignore

        match result.Value with
        | Choice1Of3 () -> failwith "logic error: Item in the queue was not processed"
        | Choice2Of3 res -> res
        | Choice3Of3 ex ->
            Logging.logf "There was an exception in the loop"
            raise ex


/// Server object that is exposed via remoting and is called by the editor
/// to get information about R (packages, functions, RData files etc.)
type RInteropServer() =
    //inherit MarshalByRefObject()
    interface IRInteropServer with

        member x.InitializationErrorMessage() =
            // No need for event loop here, because this is initialized
            // when the event loop starts (so initResult has value now)
            match RInit.rHomePath.Value with
            | RInit.RInitError error -> error
            | _ -> null

        member x.GetPackages() = EventLoop.runServerCommandSafe getPackages

        member x.LoadPackage(package) = EventLoop.runServerCommandSafe <| fun () -> loadPackage package

        member x.GetBindings(package) = EventLoop.runServerCommandSafe <| fun () -> getBindings package

        member x.GetFunctionDescriptions(package: string) =
            EventLoop.runServerCommandSafe <| fun () -> getFunctionDescriptions package

        member x.GetPackageDescription(package) =
            EventLoop.runServerCommandSafe <| fun () -> getPackageDescription package

        member x.GetRDataSymbols(file) =
            EventLoop.runServerCommandSafe
            <| fun () ->
                let env = REnv(file)

                [| for k in env.Keys ->
                       Logging.logf "GetRDataSymbols: key={%O}" k
                       let v = env.Get(k)
                       Logging.logf "GetRDataSymbols: value=%O" v.Value

                       let typ =
                           try
                               v.Value.GetType()
                           with
                           | _ -> null

                       k, typ |]
