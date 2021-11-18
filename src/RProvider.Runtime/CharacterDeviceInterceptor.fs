namespace RProvider

open RDotNet.Devices
open System.Text

type internal CharacterDeviceInterceptor() =
    inherit ConsoleDevice()

    let mutable sb: StringBuilder = null

    interface ICharacterDevice with
        override _.WriteConsole(output, length, outputType) =
            if not <| isNull sb then sb.Append(output) |> ignore else base.WriteConsole(output, length, outputType)

    member _.IsCapturing = not <| isNull sb

    member _.BeginCapture() = sb <- StringBuilder()

    member _.EndCapture() : string =
        let res = sb.ToString()
        sb <- null
        res
