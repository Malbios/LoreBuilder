namespace LoreBuilder.Model

open FunSharp.Common

[<RequireQualifiedAccess>]
type CardSide =
    | Primary
    | Secondary

[<RequireQualifiedAccess>]
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
        | CardType.Unknown -> "#FF00FF"
        | CardType.Faction -> "#543A7A"
        | CardType.Figure -> "#C68C2E"
        | CardType.Event -> "#AC3E5D"
        | CardType.Location -> "#995735"
        | CardType.Object -> "#5A9BD2"
        | CardType.Creature -> "#06B7A2"
        | CardType.Material -> "#EA6F5A"
        | CardType.Deity -> "#C2B452"
        | CardType.Emblem -> "#F7F7FA"
        | CardType.Modifier -> "#FFFFFF"

    let iconColor cardType =
        
        match cardType with
        | CardType.Emblem
        | CardType.Modifier -> "#000000"
        | _ -> themeColor cardType

    let primaryTextColor cardType =
        
        match cardType with
        | CardType.Emblem
        | CardType.Modifier -> "#000000"
        | _ -> "#FFFFFF"
        
    let secondaryTextColor cardType =
        
        match cardType with
        | CardType.Emblem
        | CardType.Modifier -> "#000000"
        | _ -> themeColor cardType

    let icon cardType =
        
        match cardType with
        | CardType.Unknown -> "fa-circle-question"
        | CardType.Faction -> "fa-users"
        | CardType.Figure -> "fa-user"
        | CardType.Event -> "fa-clock"
        | CardType.Location -> "fa-compass"
        | CardType.Object -> "fa-anchor"
        | CardType.Creature -> "fa-paw"
        | CardType.Material -> "fa-recycle"
        | CardType.Deity -> "fa-eye"
        | CardType.Emblem -> "fa-shield-cat"
        | CardType.Modifier -> "fa-masks-theater"
        
type Cues = {
    Bottom: string
    Left: string
    Top: string
    Right: string
}

module Cues =
    
    let empty = {
        Bottom = String.empty
        Left = String.empty
        Top = String.empty
        Right = String.empty
    }

type CardVisuals = {
    ThemeColor: string
    PrimaryTextColor: string
    SecondaryTextColor: string
    Icon: string
    IconColor: string
    Type: string
}

module CardVisuals =
    
    let fromCardType cardType = {
        ThemeColor = CardType.themeColor cardType
        PrimaryTextColor = CardType.primaryTextColor cardType
        SecondaryTextColor = CardType.secondaryTextColor cardType
        Icon = CardType.icon cardType
        IconColor = CardType.iconColor cardType
        Type = Union.toString cardType
    }
    
    let empty = fromCardType CardType.Unknown

type Card = {
    Type: CardType
    PrimarySide: Cues
    SecondarySide: Cues
}

module Card =
    
    let empty = {
        Type = CardType.Unknown
        PrimarySide = Cues.empty
        SecondarySide = Cues.empty
    }
    
    let copy (card: Card) = {
        card with Type = card.Type
    }
