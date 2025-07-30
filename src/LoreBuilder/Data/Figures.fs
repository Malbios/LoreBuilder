namespace LoreBuilder.Model

open LoreBuilder.Builders.Cards

[<RequireQualifiedAccess>]
module Figures =
    
    let private card = CardBuilder(figure)
    
    let cards = [
        card {
            primary ( cues { bottom "Thinker"; left "Reporter"; top "Recruit"; right "Beekeeper" } )
            secondary ( cues {
                bottom ( background {
                    text "caused the downfall of a faction or figure"
                    expansions_any [faction; figure]
                } )
                left ( traitCue {
                    text "honest to a fault"
                } )
                top ( traitCue {
                    text "brave or reckless"
                } )
                right ( agenda {
                    text "complete an innovative prototype"
                    expansion object
                } )
            } )
        }
        
        // card {
        //     primary ( cues { bottom ""; left ""; top ""; right "" } )
        //     secondary ( cues {
        //         bottom ( background {
        //             text ""
        //         } )
        //         left ( traitCue {
        //             text ""
        //         } )
        //         top ( traitCue {
        //             text ""
        //         } )
        //         right ( agenda {
        //             text ""
        //         } )
        //     } )
        // }
    ]
