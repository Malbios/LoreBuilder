namespace LoreBuilder.Model

open LoreBuilder.Builders.Cards

[<RequireQualifiedAccess>]
module Locations =
    
    let private card = CardBuilder(location)
    
    let cards = [
        card {
            primary ( cues { bottom "vault"; left "guildhall"; top "pavillion"; right "wilds" } )
            secondary ( cues {
                bottom ( background {
                    text "named after a creature"
                    expansion creature
                } )
                left ( traitCue {
                    text "shunned or forbidden by a culture (or faction)"
                    expansion faction
                } )
                top ( traitCue {
                    text "highly accessible"
                } )
                right ( traitCue {
                    text "shielded or hidden by engineering or enchantment"
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
        //         right ( traitCue {
        //             text ""
        //         } )
        //     } )
        // }
    ]
