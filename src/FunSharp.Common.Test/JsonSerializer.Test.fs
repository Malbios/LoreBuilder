namespace FunSharp.Common.Test

open System.Text.Json
open Xunit
open FsCheck
open Faqt
open Faqt.Operators
open FunSharp.Common

[<Trait("Category", "Standard")>]
module ``JsonSerializer Tests`` =

    type ``configure JsonSerializer with default web options``() =

        let serializerOptions = JsonSerializerOptions(JsonSerializerDefaults.Web)

        do JsonSerializer.configure serializerOptions

        [<Fact>]
        member x.``property naming policy should be camel case``() =
            
            %serializerOptions.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase)

        [<Fact>]
        member x.``WriteIndented should be true``() =

            %serializerOptions.WriteIndented.Should().BeTrue()

        [<Fact>]
        member x.``PropertyNameCaseInsensitive should be true``() =

            %serializerOptions.PropertyNameCaseInsensitive.Should().BeTrue()

    type ``property-based testing sample``() =

        [<Fact>]
        let ``Reverse of reverse of a list is the original list`` () =
            
            let revRevIsOrig (xs: list<int>) = List.rev (List.rev xs) = xs
            
            Check.QuickThrowOnFailure revRevIsOrig
