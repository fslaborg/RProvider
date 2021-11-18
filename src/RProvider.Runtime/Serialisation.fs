/// [omit]
module RProvider.Runtime.Serialisation

open System.Text
open Newtonsoft.Json
open PipeMethodCalls

let private settings = JsonSerializerSettings(NullValueHandling = NullValueHandling.Ignore)

/// We are using Newtonsoft rather than System.Text.Json because
/// the latter does not support System.Type.
type NewtonsoftJsonPipeSerializer() =
    interface IPipeSerializer with
        member this.Deserialize(data, ``type``) =
            JsonConvert.DeserializeObject(Encoding.UTF8.GetString(data), ``type``, settings)

        member this.Serialize(o) =
            let json = JsonConvert.SerializeObject(o, settings)
            Encoding.UTF8.GetBytes(json)
