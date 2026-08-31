namespace LoreBuilder.Test

open Xunit
open Faqt
open Faqt.Operators
open LoreBuilder
open LoreBuilder.Model

[<Trait("Category", "Standard")>]
module ``Utils Tests`` =

    type ``randomModifierCard``() =

        // Only one Modifier card exists in Data/Modifiers.fs today, so this is deterministic for
        // now - this assertion will still hold once more are added, it just won't be the only one
        // possible any more.
        [<Fact>]
        let ``always returns a card of type Modifier`` () =
            // Act
            let card = Utils.randomModifierCard ()

            // Assert
            %card.Type.Should().Be(CardType.Modifier)
