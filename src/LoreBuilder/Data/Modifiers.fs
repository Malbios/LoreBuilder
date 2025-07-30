namespace LoreBuilder.Model

open LoreBuilder.Builders.Cards

[<RequireQualifiedAccess>]
module Modifiers =
    
    let private card = CardBuilder(modifier)
    
    let cards = [
        card {
            primary ( cues { bottom "imaginative"; left "weathered"; top "untouched"; right "cruel" } )
            secondary ( cues { bottom "cracked"; left "babbling"; top "humorous"; right "avian" } )
        }
    ]
