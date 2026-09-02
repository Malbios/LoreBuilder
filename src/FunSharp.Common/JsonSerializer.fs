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
                // Without these, a fieldless union case (e.g. CardType.Faction) serializes as
                // {"faction":[]} instead of the bare "faction" a hand-written JSON file needs to
                // be able to use, and a case with named fields needs its own nested "fields"
                // wrapper instead of writing them directly - both confirmed by round-tripping the
                // real FSharp.SystemTextJson DLL, not assumed from the docs.
                .WithUnionUnwrapFieldlessTags()
                .WithUnionNamedFields()
                .WithUnionUnwrapSingleFieldCases()

        jsonFsharpOptions.AddToJsonSerializerOptions(jsonSerializerOptions)
