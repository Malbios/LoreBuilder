namespace LoreBuilder.Model

open LoreBuilder.Builders.Cards

[<RequireQualifiedAccess>]
module Deities =
    
    let private card = CardBuilder(deity)
    
    let cards = [
        card {
            primary ( cues {
                bottom ( deityCue {
                    text "lion"
                } )
                left ( deityCue {
                    text "friend"
                } )
                top ( deityCue {
                    text "smuggler"
                } )
                right ( deityCue {
                    text "reveler"
                } )
            } )
            secondary ( cues {
                bottom ( background {
                    text "dysfunctional relationship with another deity"
                    expansion deity
                } )
                left ( traitCue {
                    text "meddlesome"
                } )
                top ( domain {
                    text "travel or hospitality"
                } )
                right ( agenda {
                    text "bring about an event"
                    expansion event
                } )
            } )
        }
        
        // card {
        //     primary ( cues {
        //         bottom ( deityCue {
        //             text ""
        //         } )
        //         left ( deityCue {
        //             text ""
        //         } )
        //         top ( deityCue {
        //             text ""
        //         } )
        //         right ( deityCue {
        //             text ""
        //         } )
        //     } )
        //     secondary ( cues {
        //         bottom ( background {
        //             text ""
        //         } )
        //         left ( traitCue {
        //             text ""
        //         } )
        //         top ( domain {
        //             text ""
        //         } )
        //         right ( agenda {
        //             text ""
        //         } )
        //     } )
        // }
    ]
