namespace LoreBuilder.Model

open LoreBuilder.Builders.Cards

[<RequireQualifiedAccess>]
module Creatures =
    
    let private card = CardBuilder(creature)
    
    let cards = [
        card {
            primary ( cues { bottom "bear"; left "falcon"; top "fungus"; right "anglerfish" } )
            secondary ( cues {
                bottom ( background {
                    text "sacred or profane to a culture (or faction)"
                    expansion faction
                } )
                left ( traitCue {
                    text "shifting skin or fur markings"
                } )
                top ( traitCue {
                    text "swarm or frenzy feeder"
                } )
                right ( traitCue {
                    text "horns, tusks, or antlers"
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
