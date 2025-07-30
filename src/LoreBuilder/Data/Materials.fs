namespace LoreBuilder.Model

open LoreBuilder.Builders.Cards

[<RequireQualifiedAccess>]
module Materials =
    
    let private card = CardBuilder(material)
    
    let cards = [
        card {
            primary ( cues { bottom "broth"; left "fuel"; top "mortar"; right "fur" } )
            secondary ( cues {
                bottom ( background {
                    text "used in creating an object"
                    expansion object
                } )
                left ( traitCue {
                    text "useful for writing, carving, or sculpting"
                } )
                top ( traitCue {
                    text "bright, multicolored, or diaphanous"
                } )
                right ( background {
                    text "prized by a specific profession"
                    expansions_any [figure; figure]
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
