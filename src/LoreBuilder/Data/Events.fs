namespace LoreBuilder.Model

open LoreBuilder.Builders.Cards

[<RequireQualifiedAccess>]
module Events =
    
    let private card = CardBuilder(event)
    
    let cards = [
        card {
            primary ( cues { bottom "accord"; left "stampede"; top "battle"; right "annihilation" } )
            secondary ( cues {
                bottom ( catalyst {
                    text "belief in a false prediction or prophet"
                    expansions_any [event; figure]
                } )
                left ( traitCue {
                    text "notably silent or loud"
                } )
                top ( catalyst_or_fallout {
                    text "staple food, ingredient, or species depleted"
                    expansions_any [material; creature]
                } )
                right ( fallout {
                    text "faction founding a new nation or headquarters"
                    expansions_all [faction; location]
                } )
            } )
        }
        
        // card {
        //     primary ( cues { bottom ""; left ""; top ""; right "" } )
        //     secondary ( cues {
        //         bottom ( catalyst {
        //             text ""
        //         } )
        //         left ( traitCue {
        //             text ""
        //         } )
        //         top ( catalyst_or_fallout {
        //             text ""
        //         } )
        //         right ( fallout {
        //             text ""
        //         } )
        //     } )
        // }
    ]
