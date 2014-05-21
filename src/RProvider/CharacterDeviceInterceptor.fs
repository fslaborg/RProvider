namespace RProvider

open RDotNet.Devices
open System.Text

type internal CharacterDeviceInterceptor() = 
    inherit RDotNet.Devices.ConsoleDevice()

    let mutable sb : StringBuilder = null

    interface ICharacterDevice with
        override this.WriteConsole(output, length, outputType) = 
            if sb <> null then
                sb.Append(output) |> ignore
            else
                base.WriteConsole(output, length, outputType)

    member this.IsCapturing = sb <> null

    member this.BeginCapture() =
        sb <- new StringBuilder()

    member this.EndCapture() : string =
        let res = sb.ToString()
        sb <- null
        res
