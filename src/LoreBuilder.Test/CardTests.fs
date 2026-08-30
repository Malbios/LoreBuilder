namespace LoreBuilder.Test

open Xunit
open Faqt
open Faqt.Operators
open LoreBuilder.Model
open LoreBuilder.Components

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

    type ``Logical accepts``() =

        [<Fact>]
        let ``One requires an exact match`` () =
            %(Logical.accepts (Logical.One CardType.Event) CardType.Event).Should().BeTrue()
            %(Logical.accepts (Logical.One CardType.Event) CardType.Figure).Should().BeFalse()

        [<Fact>]
        let ``Any accepts membership in the list`` () =
            %(Logical.accepts (Logical.Any [ CardType.Event; CardType.Figure ]) CardType.Figure).Should().BeTrue()
            %(Logical.accepts (Logical.Any [ CardType.Event; CardType.Figure ]) CardType.Location).Should().BeFalse()

        [<Fact>]
        let ``All also accepts membership in the list (single-slot limitation)`` () =
            %(Logical.accepts (Logical.All [ CardType.Event; CardType.Figure ]) CardType.Figure).Should().BeTrue()
            %(Logical.accepts (Logical.All [ CardType.Event; CardType.Figure ]) CardType.Location).Should().BeFalse()

    type ``Logical slotTypes and acceptsAt``() =

        [<Fact>]
        let ``One only has a single slot at index 0`` () =
            %(Logical.slotTypes 0 (Logical.One CardType.Event)).Should().Be(Some [ CardType.Event ])
            %(Logical.slotTypes 1 (Logical.One CardType.Event)).Should().Be(None)

        [<Fact>]
        let ``Any only has a single slot at index 0, accepting every listed type`` () =
            let any = Logical.Any [ CardType.Event; CardType.Figure ]

            %(Logical.slotTypes 0 any).Should().Be(Some [ CardType.Event; CardType.Figure ])
            %(Logical.slotTypes 1 any).Should().Be(None)
            %(Logical.acceptsAt 0 any CardType.Figure).Should().BeTrue()

        [<Fact>]
        let ``All has one position-locked slot per list item`` () =
            let all = Logical.All [ CardType.Event; CardType.Figure ]

            %(Logical.slotTypes 0 all).Should().Be(Some [ CardType.Event ])
            %(Logical.slotTypes 1 all).Should().Be(Some [ CardType.Figure ])
            %(Logical.slotTypes 2 all).Should().Be(None)

            %(Logical.acceptsAt 0 all CardType.Event).Should().BeTrue()
            %(Logical.acceptsAt 0 all CardType.Figure).Should().BeFalse()
            %(Logical.acceptsAt 1 all CardType.Figure).Should().BeTrue()
            %(Logical.acceptsAt 1 all CardType.Event).Should().BeFalse()

    type ``CardHelpers activeCue``() =

        let cues = {
            Bottom = Some(Cue.Simple "B")
            Left = Some(Cue.Simple "L")
            Top = Some(Cue.Simple "T")
            Right = Some(Cue.Simple "R")
        }

        [<Fact>]
        let ``with activeEdge Top and no rotation, returns the card's own Top cue`` () =
            %(CardHelpers.activeCue cues CardEdge.Top 0).Should().Be(Some(Cue.Simple "T"))

        [<Fact>]
        let ``with activeEdge Top and 180 degree rotation, returns the card's own Bottom cue`` () =
            %(CardHelpers.activeCue cues CardEdge.Top 180).Should().Be(Some(Cue.Simple "B"))

        [<Fact>]
        let ``with activeEdge Top and 90 degree rotation, returns the card's own Left cue`` () =
            %(CardHelpers.activeCue cues CardEdge.Top 90).Should().Be(Some(Cue.Simple "L"))

        [<Fact>]
        let ``with activeEdge Top and 270 degree rotation, returns the card's own Right cue`` () =
            %(CardHelpers.activeCue cues CardEdge.Top 270).Should().Be(Some(Cue.Simple "R"))

        [<Fact>]
        let ``with activeEdge Bottom and no rotation, returns the card's own Bottom cue`` () =
            %(CardHelpers.activeCue cues CardEdge.Bottom 0).Should().Be(Some(Cue.Simple "B"))

        [<Fact>]
        let ``with activeEdge Bottom and 180 degree rotation, returns the card's own Top cue`` () =
            %(CardHelpers.activeCue cues CardEdge.Bottom 180).Should().Be(Some(Cue.Simple "T"))
