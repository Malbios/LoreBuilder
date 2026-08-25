namespace LoreBuilder.Test

open Xunit
open Faqt
open Faqt.Operators
open LoreBuilder.Model

[<Trait("Category", "Standard")>]
module ``Card Tests`` =

    type ``CardEdge opposite``() =

        [<Fact>]
        let ``opposite of Bottom is Top`` () =
            %(CardEdge.opposite CardEdge.Bottom).Should().Be(CardEdge.Top)

        [<Fact>]
        let ``opposite of Top is Bottom`` () =
            %(CardEdge.opposite CardEdge.Top).Should().Be(CardEdge.Bottom)

        [<Fact>]
        let ``opposite of Left is Right`` () =
            %(CardEdge.opposite CardEdge.Left).Should().Be(CardEdge.Right)

        [<Fact>]
        let ``opposite of Right is Left`` () =
            %(CardEdge.opposite CardEdge.Right).Should().Be(CardEdge.Left)

        [<Fact>]
        let ``opposite is its own inverse for every edge`` () =
            // Arrange
            let edges = [ CardEdge.Bottom; CardEdge.Left; CardEdge.Top; CardEdge.Right ]

            // Act
            let isInvolution =
                edges |> List.forall (fun edge -> CardEdge.opposite (CardEdge.opposite edge) = edge)

            // Assert
            %isInvolution.Should().BeTrue()

    type ``CardType presentation``() =

        [<Fact>]
        let ``every card type has a non-empty theme color and a fa- icon`` () =
            // Act
            let allValid =
                FunSharp.Common.Union.toList<CardType>()
                |> List.forall (fun cardType -> CardType.themeColor cardType <> "" && (CardType.icon cardType).StartsWith("fa-"))

            // Assert
            %allValid.Should().BeTrue()

        [<Fact>]
        let ``Emblem and Modifier use black icon/text colors, everything else uses white text on the theme color`` () =
            // Act
            let allCorrect =
                FunSharp.Common.Union.toList<CardType>()
                |> List.forall (fun cardType ->
                    match cardType with
                    | CardType.Emblem
                    | CardType.Modifier ->
                        CardType.iconColor cardType = "#000000" && CardType.primaryTextColor cardType = "#000000"
                    | _ ->
                        CardType.iconColor cardType = CardType.themeColor cardType
                        && CardType.primaryTextColor cardType = "#FFFFFF")

            // Assert
            %allCorrect.Should().BeTrue()

    type ``Card empty``() =

        [<Fact>]
        let ``has type Unknown and no cues on either side`` () =
            %Card.empty.Type.Should().Be(CardType.Unknown)
            %Card.empty.PrimarySide.Should().Be(Cues.empty)
            %Card.empty.SecondarySide.Should().Be(Cues.empty)

    type ``Card copy``() =

        [<Fact>]
        let ``is structurally equal to the original`` () =
            // Arrange
            let card = Card.empty

            // Act
            let copied = Card.copy card

            // Assert
            %copied.Should().Be(card)

        [<Fact>]
        let ``is a different reference than the original`` () =
            // Arrange
            let card = Card.empty

            // Act
            let copied = Card.copy card

            // Assert - the drag-drop library's CopyItem callback relies on this
            %(System.Object.ReferenceEquals(card, copied)).Should().BeFalse()
