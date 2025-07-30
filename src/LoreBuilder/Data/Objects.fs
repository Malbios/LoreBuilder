namespace LoreBuilder.Model

open LoreBuilder.Builders.Cards

[<RequireQualifiedAccess>]
module Objects =
    
    let private card = CardBuilder(object)
    
    let cards = [
        card {
            primary ( cues { bottom "mace"; left "globe"; top "robe"; right "token" } )
            secondary ( cues {
                bottom ( background {
                    text "crafting method protected by a figure or faction"
                    expansions_any [figure; faction]
                } )
                left ( traitCue {
                    text "shadowy or ethereal appearance"
                } )
                top ( traitCue {
                    text "mind-controlling or -controllable properties"
                } )
                right ( background {
                    text "created or given as a peace offering in a conflict"
                    expansions_any [event; event]
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
        //         right ( background {
        //             text ""
        //         } )
        //     } )
        // }
    ]
