namespace LoreBuilder.Model

open System
open FunSharp.Common

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
        | Emblem -> "#F7F7FA"
        | Modifier -> "#FFFFFF"

    let iconColor cardType =
        
        match cardType with
        | Emblem
        | Modifier -> "#000000"
        | _ -> themeColor cardType

    let frontTextColor cardType =
        
        match cardType with
        | Emblem
        | Modifier -> "#000000"
        | _ -> "#FFFFFF"
        
    let backTextColor cardType =
        
        match cardType with
        | Emblem
        | Modifier -> "#000000"
        | _ -> themeColor cardType

    let icon cardType =
        
        match cardType with
        | Unknown -> "fa-circle-question"
        | Faction -> "fa-users"
        | Figure -> "fa-user"
        | Event -> "fa-clock"
        | Location -> "fa-compass"
        | Object -> "fa-anchor"
        | Creature -> "fa-paw"
        | Material -> "fa-recycle"
        | Deity -> "fa-eye"
        | Emblem -> "fa-shield-cat"
        | Modifier -> "fa-masks-theater"
        
type Sides = {
    Top: string
    Right: string
    Bottom: string
    Left: string
}

module Sides =
    
    let empty = {
        Top = String.empty
        Right = String.empty
        Bottom = String.empty
        Left = String.empty
    }

type CardData = {
    Id: Guid
    Type: CardType
    Front: Sides
    Back: Sides
}

type CardVisuals = {
    ThemeColor: string
    FrontTextColor: string
    BackTextColor: string
    Icon: string
    IconColor: string
    Type: string
}

module CardVisuals =
    
    let fromCardType cardType = {
        ThemeColor = CardType.themeColor cardType
        FrontTextColor = CardType.frontTextColor cardType
        BackTextColor = CardType.backTextColor cardType
        Icon = CardType.icon cardType
        IconColor = CardType.iconColor cardType
        Type = Union.toString cardType
    }

module CardData =
    
    let empty = {
        Id = Guid.Empty
        Type = CardType.Unknown
        Front = Sides.empty
        Back = Sides.empty
    }
