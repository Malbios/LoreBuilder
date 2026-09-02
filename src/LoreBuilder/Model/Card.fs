namespace LoreBuilder.Model

open System
open FunSharp.Common

[<RequireQualifiedAccess>]
type CardSide =
    | Primary
    | Secondary

type CardUiState = {
    CurrentSide: CardSide
    Rotation: int
}

[<RequireQualifiedAccess>]
module CardUiState =

    let initial = {
        CurrentSide = CardSide.Primary
        Rotation = 0
    }

[<RequireQualifiedAccess>]
type CardEdge =
    | Bottom
    | Left
    | Top
    | Right
    
[<RequireQualifiedAccess>]
module CardEdge =
    
    let opposite edge =
        
        match edge with
        | CardEdge.Bottom -> CardEdge.Top
        | CardEdge.Left -> CardEdge.Right
        | CardEdge.Top -> CardEdge.Bottom
        | CardEdge.Right -> CardEdge.Left

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
    
type ComplexCue = {
    Header: string option
    Text: string
    Expansions: Logical<CardType> option
}

// The literal text split around one inline CardType icon (e.g. "A Deity's Choir" ->
// Before="A "; Icon=Deity; After="'s Choir") - physical cards from some expansions embed an
// actual type-icon glyph mid-phrase instead of spelling the type out, and (see LoreCluster.fs's
// innerRequiredType) that icon also means an Inner slot filled while this card is Primary must
// be that specific type, not one matching Primary's own.
type IconTextCue = {
    Before: string
    Icon: CardType
    After: string
}

[<RequireQualifiedAccess>]
type Cue =
    | Simple of text: string
    | Complex of ComplexCue
    | Icon of fileName: string
    | IconText of IconTextCue
    
[<RequireQualifiedAccess>]
module Cue =
    
    let private iconKind cardType =
        
        match cardType with
        | CardType.Emblem
        | CardType.Modifier -> "black"
        | _ -> "white"
        
    let iconUri cardType fileName =
        Uri($"assets/symbols/{iconKind cardType}/{fileName}", UriKind.Relative)
    
type Cues = {
    Bottom: Cue option
    Left: Cue option
    Top: Cue option
    Right: Cue option
}

[<RequireQualifiedAccess>]
module Cues =
    
    let empty = {
        Bottom = None
        Left = None
        Top = None
        Right = None
    }

type CardVisuals = {
    ThemeColor: string
    PrimaryTextColor: string
    SecondaryTextColor: string
    Icon: string
    IconColor: string
    Type: string
}

[<RequireQualifiedAccess>]
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
    // Which expansion this card came from, if any - data-level only for now (no UI surfaces it
    // yet), e.g. "Deity Expansion". None for every base-deck card.
    Expansion: string option
}

[<RequireQualifiedAccess>]
module Card =

    let empty = {
        Type = CardType.Unknown
        PrimarySide = Cues.empty
        SecondarySide = Cues.empty
        Expansion = None
    }
    
    // Forces a fresh reference (F# record-update always allocates a new object, even
    // when no field's value changes), which is what Dropzone<Card>'s CopyItem callback
    // in StackTest.fs needs. Card has structural equality, so `copy c = c` is still true -
    // this is about reference identity, not producing a distinguishable value.
    let copy (card: Card) = {
        card with Type = card.Type
    }
