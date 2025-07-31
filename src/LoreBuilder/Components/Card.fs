namespace LoreBuilder.Components

open Bolero
open Bolero.Html
open FunSharp.Common
open LoreBuilder
open LoreBuilder.Model
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Web

[<RequireQualifiedAccess>]
module CardHelpers =
        
    let cueExpansions (separator: string) (cardTypes: CardType list) =
        
        div {
            attr.``class`` "cue-expansions"
            
            cardTypes
            |> List.map (fun t ->
                let iconColor = CardType.iconColor t
                let icon = CardType.icon t
                
                div {
                    attr.style $"color: {iconColor};"
                    
                    i { attr.``class`` $"fa-solid {icon}" }
                }
            )
            |> List.join (text separator)
            |> Utils.renderList
        }
        
    let complexCue cue =
        
        div {
            attr.``class`` "cue-header-and-text-and-expansions"
            
            if cue.Header.IsSome then
                div {
                    attr.``class`` "cue-header"
                    text cue.Header.Value
                }
            
            div {
                attr.``class`` "cue-text-and-expansions"
                
                div {
                    attr.``class`` "cue-text"
                    cue.Text
                }
                
                if cue.Expansions.IsSome then
                    match cue.Expansions.Value with
                    | Logical.One v -> cueExpansions "" [v]
                    | Logical.Any v -> cueExpansions "/" v
                    | Logical.All v -> cueExpansions "+" v
            }
        }
        
    let cardCue cardType cue =
        
        match cue with
        | None -> Node.Empty ()
        | Some cue ->
            match cue with
            | Cue.Simple s -> text s
            | Cue.Icon fileName -> img { attr.src (Cue.iconUri cardType fileName) }
            | Cue.Complex cue -> complexCue cue
            
    let edgeFromRotation rotation =
        
        match rotation % 360 with
        | 0 -> CardEdge.Bottom
        
        | 270
        | -90 -> CardEdge.Left
        
        | 180
        | -180 -> CardEdge.Top
        
        | 90
        | -270 -> CardEdge.Right
        
        | _ -> failwith $"unexpected rotation: {rotation}"
            
    let isVisible (activeEdge: CardEdge option) rotation edge =
        
        match activeEdge with
        | None -> true
        | Some v ->
            let rotatedEdge = edgeFromRotation rotation
            
            match v with
            | CardEdge.Bottom ->
                edge = rotatedEdge
            | CardEdge.Top ->
                edge = (CardEdge.opposite rotatedEdge)
            | _ -> failwith $"unexpected active edge value: {v}"
            
    type CardData = {
        Class: string
        Style: string
        ActiveEdge: CardEdge option
        Rotation: int
        Type: CardType
        Visuals: CardVisuals
        Cues: Cues
    }
            
    let cardCues data =
        
        div {
            attr.``class`` data.Class
            attr.style data.Style
            
            [
            (CardEdge.Bottom, data.Cues.Bottom)
            (CardEdge.Left, data.Cues.Left)
            (CardEdge.Top, data.Cues.Top)
            (CardEdge.Right, data.Cues.Right)
            ]
            |> List.map (fun (edge, cue) ->
                let cueKind =
                    cue |> Option.defaultValue (Cue.Simple "") |> (fun x -> (Union.toString x).ToLower())
                    
                let visibility =
                    if isVisible data.ActiveEdge data.Rotation edge then "visible" else "hidden"
                    
                let edgeName =
                    (Union.toString edge).ToLower()
                
                div {
                    attr.``class`` $"cue {cueKind}-cue {cueKind}-{edgeName}-edge"
                    attr.style $"visibility: {visibility};"
                    
                    cardCue data.Type cue
                }
            )
            |> Utils.renderList
            
            comp<CardBadge> {
                "Visuals" => data.Visuals
            }
        }
            
    let arrow className arrowStyle controlColor rotate icon =
            
        div {
            attr.``class`` $"arrow {className}"
            attr.style $"{arrowStyle}color: {controlColor};"
            
            on.click rotate
            on.stopPropagation "click" true
            
            i { attr.``class`` $"fa-solid {icon}" }
        }

type Card() =
    inherit Component()
    
    override _.CssScope = CssScopes.Card
    
    [<Parameter>]
    member val Data = Card.empty with get, set
    
    [<Parameter>]
    member val CurrentSide = CardSide.Primary with get, set
    
    [<Parameter>]
    member val IsHovered = false with get, set
    
    [<Parameter>]
    member val IsFlipping = false with get, set
    
    [<Parameter>]
    member val CanBeFlipped = true with get, set
    
    [<Parameter>]
    member val CanBeRotated = true with get, set
    
    [<Parameter>]
    member val ActiveEdge = None with get, set
    
    [<Parameter>]
    member val Size = 0 with get, set
    
    [<Parameter>]
    member val Rotation = 0 with get, set
    
    member private this.FlippedClass () =
        
        match this.CurrentSide with
        | CardSide.Primary -> ""
        | CardSide.Secondary -> " flipped-card"
        
    member private this.Flip () =
        
        if this.CanBeFlipped then
            this.CurrentSide <-
                match this.CurrentSide with
                | CardSide.Primary -> CardSide.Secondary
                | CardSide.Secondary -> CardSide.Primary
                
    member private this.Rotate direction =
        
        match direction with
        | RotationDirection.Clockwise ->
            this.Rotation <- this.Rotation + 90
        | RotationDirection.CounterClockwise ->
            this.Rotation <- this.Rotation - 90
            
    member private this.ArrowsVisibility () =
        
        if this.CanBeRotated && not this.IsFlipping && this.IsHovered then
            "visibility: visible; opacity: 1;"
        else
            "visibility: hidden; opacity: 0;"

    override this.Render() =
        
        let cardVisuals =
            CardVisuals.fromCardType this.Data.Type
        
        div {
            attr.style $"width: {this.Size}px; height: {this.Size}px; position: relative;"
            
            on.mouseover (fun _ -> this.IsHovered <- true)
            on.mouseout (fun _ -> this.IsHovered <- false)
            
            div {
                attr.``class`` "flippable-card-container"
                attr.style $"transform: rotate({this.Rotation}deg);"
                
                on.click (fun (_: MouseEventArgs) -> this.Flip ())
                
                div {
                    attr.``class`` $"card{this.FlippedClass ()}"
                    
                    on.event "transitionstart" (fun _ -> this.IsFlipping <- true)
                    on.event "transitionend" (fun _ -> this.IsFlipping <- false)
                    
                    ({
                        Class = "primary-side"
                        Style = $"color: {cardVisuals.PrimaryTextColor}; background-color: {cardVisuals.ThemeColor};"
                        ActiveEdge = this.ActiveEdge
                        Rotation = this.Rotation
                        Type = this.Data.Type
                        Visuals = cardVisuals
                        Cues = this.Data.PrimarySide
                    } : CardHelpers.CardData)
                    |> CardHelpers.cardCues
                    
                    ({
                        Class = "secondary-side"
                        Style = $"color: {cardVisuals.SecondaryTextColor}; background-color: #FFFFFF; border: 2px solid {cardVisuals.SecondaryTextColor};"
                        ActiveEdge = this.ActiveEdge
                        Rotation = this.Rotation
                        Type = this.Data.Type
                        Visuals = cardVisuals
                        Cues = this.Data.SecondarySide
                    } : CardHelpers.CardData)
                    |> CardHelpers.cardCues
                }
            }
            
            let controlColor =
                match this.CurrentSide with
                | CardSide.Primary -> cardVisuals.PrimaryTextColor
                | CardSide.Secondary -> cardVisuals.SecondaryTextColor
                
            let rotateCounterClockwise (_: MouseEventArgs) =
                this.Rotate RotationDirection.CounterClockwise
                
            let rotateClockwise (_: MouseEventArgs) =
                this.Rotate RotationDirection.Clockwise
            
            CardHelpers.arrow "arrow-left" (this.ArrowsVisibility ()) controlColor rotateCounterClockwise "fa-rotate-left"
            CardHelpers.arrow "arrow-right" (this.ArrowsVisibility ()) controlColor rotateClockwise "fa-rotate-right"
        }
