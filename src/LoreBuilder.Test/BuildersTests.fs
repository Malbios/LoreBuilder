namespace LoreBuilder.Test

open Xunit
open Faqt
open Faqt.Operators
open LoreBuilder.Model
open LoreBuilder.Builders.Cards

[<Trait("Category", "Standard")>]
module ``Builders Tests`` =

    type ``card and cues builders``() =

        [<Fact>]
        let ``builds a card with the given type and cues on both sides`` () =
            // Act
            let result =
                CardBuilder(faction) {
                    primary (cues { bottom "City"; left "College"; top "Union"; right "Battalion" })
                    secondary (cues { bottom "Ashes" })
                }

            // Assert
            %result.Type.Should().Be(CardType.Faction)
            %result.PrimarySide.Bottom.Should().Be(Some(Cue.Simple "City"))
            %result.PrimarySide.Left.Should().Be(Some(Cue.Simple "College"))
            %result.PrimarySide.Top.Should().Be(Some(Cue.Simple "Union"))
            %result.PrimarySide.Right.Should().Be(Some(Cue.Simple "Battalion"))
            %result.SecondarySide.Bottom.Should().Be(Some(Cue.Simple "Ashes"))
            %result.SecondarySide.Left.Should().Be(None)

        [<Fact>]
        let ``cue positions default to None when not set`` () =
            // Act
            let result = cues { bottom "only this one" }

            // Assert
            %result.Bottom.Should().Be(Some(Cue.Simple "only this one"))
            %result.Left.Should().Be(None)
            %result.Top.Should().Be(None)
            %result.Right.Should().Be(None)

        [<Fact>]
        let ``a cue position accepts either a raw string or an already-built Cue`` () =
            // Act
            let fromString = cues { bottom "plain text" }
            let fromCue = cues { bottom (icon "axe") }

            // Assert
            %fromString.Bottom.Should().Be(Some(Cue.Simple "plain text"))
            %fromCue.Bottom.Should().Be(Some(Cue.Icon "axe.svg"))

    type ``complex cue builders``() =

        [<Fact>]
        let ``sets the header from the builder's kind and the text from the text operation`` () =
            // Act
            let result = background { text "co-founded by rivals" }

            // Assert
            match result with
            | Cue.Complex complexCue ->
                %complexCue.Header.Should().Be(Some "Background")
                %complexCue.Text.Should().Be("co-founded by rivals")
                %complexCue.Expansions.Should().Be(None)
            | other -> failwith $"expected Cue.Complex, got {other}"

        [<Fact>]
        let ``expansion sets a single required type`` () =
            // Act
            let result = agenda { text "undo an event"; expansion event }

            // Assert
            match result with
            | Cue.Complex complexCue -> %complexCue.Expansions.Should().Be(Some(Logical.One CardType.Event))
            | other -> failwith $"expected Cue.Complex, got {other}"

        [<Fact>]
        let ``expansions_any sets an Any list`` () =
            // Act
            let result = traitCue { text "corrupted"; expansions_any [ location; figure ] }

            // Assert
            match result with
            | Cue.Complex complexCue ->
                %complexCue.Expansions.Should().Be(Some(Logical.Any [ CardType.Location; CardType.Figure ]))
            | other -> failwith $"expected Cue.Complex, got {other}"

        [<Fact>]
        let ``expansions_all sets an All list`` () =
            // Act
            let result = background { text "co-founded"; expansions_all [ figure; figure ] }

            // Assert
            match result with
            | Cue.Complex complexCue ->
                %complexCue.Expansions.Should().Be(Some(Logical.All [ CardType.Figure; CardType.Figure ]))
            | other -> failwith $"expected Cue.Complex, got {other}"

    type ``icon helper``() =

        [<Fact>]
        let ``appends the svg extension`` () =
            %(icon "cow").Should().Be(Cue.Icon "cow.svg")
