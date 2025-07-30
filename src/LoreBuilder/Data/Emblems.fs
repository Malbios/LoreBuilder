namespace LoreBuilder.Model

open LoreBuilder.Builders.Cards

[<RequireQualifiedAccess>]
module Emblems =
    
    let private card = CardBuilder(emblem)
    
    let cards = [
        card {
            primary ( cues { bottom (icon "cow"); left (icon "tri"); top (icon "fireball"); right (icon "teapot") } )
            secondary ( cues { bottom (icon "royal"); left (icon "dress"); top (icon "class_diagram"); right (icon "sun_path") } )
        }
        
        // card {
        //     primary ( cues { bottom (icon ""); left (icon ""); top (icon ""); right (icon "") } )
        //     secondary ( cues { bottom (icon ""); left (icon ""); top (icon ""); right (icon "") } )
        // }
    ]
