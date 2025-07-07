namespace FunSharp.Common

open System.Text.Json
open System.Text.Json.Serialization

module JsonSerializer =

    let configure (jsonSerializerOptions: JsonSerializerOptions) =
        jsonSerializerOptions.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
        jsonSerializerOptions.WriteIndented <- true
        jsonSerializerOptions.PropertyNameCaseInsensitive <- true

        let jsonFsharpOptions =
            JsonFSharpOptions
                .Default()
                .WithUnionExternalTag()
                .WithUnionTagCaseInsensitive()
                .WithUnionTagNamingPolicy(JsonNamingPolicy.CamelCase)
                .WithUnionFieldNamingPolicy(JsonNamingPolicy.CamelCase)
                .WithSkippableOptionFields(SkippableOptionFields.Always)

        jsonFsharpOptions.AddToJsonSerializerOptions(jsonSerializerOptions)
