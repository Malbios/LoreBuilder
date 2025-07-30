namespace LoreBuilder.Model

open LoreBuilder.Builders.Cards

[<RequireQualifiedAccess>]
module Factions =
    
    let private card = CardBuilder(faction)
    
    let cards = [
        card {
            primary ( cues { bottom "City"; left "College"; top "Union"; right "Battalion" } )
            secondary ( cues {
                bottom ( background {
                    text "co-founded by friends or rivals"
                    expansions_all [figure; figure]
                } )
                left ( traitCue {
                    text "highly corrupted or corruptible"
                } )
                top ( traitCue {
                    text "frequently work out of a certain kind of location"
                    expansions_any [location; location]
                } )
                right ( agenda {
                    text "undo an event"
                    expansion event
                } )
            } )
        }
    ]
