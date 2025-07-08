namespace LoreBuilder.Model

type CardType =
    | Unknown
    | Faction
    | Figure
    | Event
    | Location
    | Object
    | Creature
    | Material
    | Deity
    | Emblem
    | Modifier

[<RequireQualifiedAccess>]
module CardType =
    
    let themeColor cardType =
        
        match cardType with
        | Unknown -> "#FF00FF"
        | Faction -> "#543A7A"
        | Figure -> "#C68C2E"
        | Event -> "#AC3E5D"
        | Location -> "#995735"
        | Object -> "#5A9BD2"
        | Creature -> "#06B7A2"
        | Material -> "#EA6F5A"
        | Deity -> "#C2B452"
        | Emblem
        | Modifier -> "#FFFFFF"

    let textColor cardType =
        
        match cardType with
        | Emblem
        | Modifier -> "#000000"
        | _ -> "#FFFFFF"
