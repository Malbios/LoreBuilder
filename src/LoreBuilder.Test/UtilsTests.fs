namespace LoreBuilder.Test

open Xunit
open Faqt
open Faqt.Operators
open LoreBuilder
open LoreBuilder.Model

[<Trait("Category", "Standard")>]
module ``Utils Tests`` =

    type ``randomModifierCard``() =

        // Card data now comes from CardData's own runtime-loaded pool (see CardData.fs), not a
        // compiled Data/Modifiers.fs module - seed it directly rather than depending on a real
        // HttpClient fetch, which isn't available in a unit test.
        [<Fact>]
        let ``always returns a card of type Modifier`` () =
            // Arrange
            CardData.seedForTests [ [ { Card.empty with Type = CardType.Modifier } ] ]

            // Act
            let card = Utils.randomModifierCard ()

            // Assert
            %card.Type.Should().Be(CardType.Modifier)
