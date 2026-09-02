namespace LoreBuilder.Test

open Xunit
open Faqt
open Faqt.Operators
open LoreBuilder
open LoreBuilder.Model

[<Trait("Category", "Standard")>]
module ``CardData Tests`` =

    type ``parseCards``() =

        [<Fact>]
        let ``parses a card with Simple, Complex and Icon cues on the same side`` () =
            // Arrange
            let json =
                """
                [
                  {
                    "type": "faction",
                    "primarySide": {
                      "bottom": { "simple": "City" },
                      "right": { "icon": "axe.svg" }
                    },
                    "secondarySide": {
                      "left": { "complex": { "header": "Trait", "text": "highly corrupted or corruptible" } }
                    }
                  }
                ]
                """

            // Act
            let actual = CardData.parseCards json

            // Assert
            let expected = [
                {
                    Card.empty with
                        Type = CardType.Faction
                        PrimarySide = {
                            Cues.empty with
                                Bottom = Some(Cue.Simple "City")
                                Right = Some(Cue.Icon "axe.svg")
                        }
                        SecondarySide = {
                            Cues.empty with
                                Left =
                                    Some(
                                        Cue.Complex {
                                            Header = Some "Trait"
                                            Text = "highly corrupted or corruptible"
                                            Expansions = None
                                        }
                                    )
                        }
                }
            ]

            %actual.Should().Be(expected)

        [<Fact>]
        let ``parses a Complex cue's One/Any/All expansions`` () =
            // Arrange
            let json =
                """
                [
                  {
                    "type": "event",
                    "primarySide": {},
                    "secondarySide": {
                      "bottom": { "complex": { "text": "one", "expansions": { "one": "figure" } } },
                      "left": { "complex": { "text": "any", "expansions": { "any": ["location", "location"] } } },
                      "top": { "complex": { "text": "all", "expansions": { "all": ["faction", "location"] } } }
                    }
                  }
                ]
                """

            // Act
            let card = CardData.parseCards json |> List.head

            let expansionsOf cue =
                match cue with
                | Some(Cue.Complex complexCue) -> complexCue.Expansions
                | _ -> None

            let actualOne = expansionsOf card.SecondarySide.Bottom
            let actualAny = expansionsOf card.SecondarySide.Left
            let actualAll = expansionsOf card.SecondarySide.Top

            // Assert
            %actualOne.Should().Be(Some(Logical.One CardType.Figure))
            %actualAny.Should().Be(Some(Logical.Any [ CardType.Location; CardType.Location ]))
            %actualAll.Should().Be(Some(Logical.All [ CardType.Faction; CardType.Location ]))

        [<Fact>]
        let ``omitting a Complex cue's header parses it as None`` () =
            // Arrange
            let json =
                """
                [
                  { "type": "modifier", "primarySide": { "bottom": { "complex": { "text": "no header" } } }, "secondarySide": {} }
                ]
                """

            // Act
            let actual = (CardData.parseCards json |> List.head).PrimarySide.Bottom

            // Assert
            let expected = Some(Cue.Complex { Header = None; Text = "no header"; Expansions = None })
            %actual.Should().Be(expected)
