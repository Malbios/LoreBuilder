namespace LoreBuilder.Model

open LoreBuilder.Builders.Cards

[<RequireQualifiedAccess>]
module Factions =
    
    let private card = CardBuilder(CardType.Faction)
    
    let cards = [
        card {
            primary ( cues { bottom "City"; left "College"; top "Union"; right "Battalion" } )
            secondary ( cues {
                bottom ( background {
                    text "co-founded by friends or rivals"
                    requires ( [CardType.Figure; CardType.Figure] |> all )
                } )
                left ( traitCue {
                    text "highly corrupted or corruptible"
                } )
                top ( traitCue {
                    text "frequently work out of a certain kind of location"
                    requires ( [CardType.Location; CardType.Location] |> any )
                } )
                right ( agenda {
                    text "undo an event"
                    requires CardType.Event
                } )
            } )
        }
    ]
