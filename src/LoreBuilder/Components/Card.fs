namespace LoreBuilder.Components

open Bolero
open Bolero.Html
open FunSharp.Common
open LoreBuilder
open LoreBuilder.Model
open Microsoft.AspNetCore.Components

type Card() =
    inherit Component()
    
    let mutable rotation = 0
    
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
    
    member private this.FlippedClass () =
        match this.CurrentSide with
        | CardSide.Primary -> ""
        | CardSide.Secondary -> " flipped-card"

    override this.Render() =
        
        let flip _ =
            if this.CanBeFlipped then
                this.CurrentSide <-
                    match this.CurrentSide with
                    | CardSide.Primary -> CardSide.Secondary
                    | CardSide.Secondary -> CardSide.Primary
                
        let rotateClockwise _ =
            rotation <- rotation + 90
            
        let rotateCounterClockwise _ =
            rotation <- rotation - 90
            
        let cardVisuals =
            CardVisuals.fromCardType this.Data.Type
        
        let controlColor =
            match this.CurrentSide with
            | CardSide.Primary -> cardVisuals.PrimaryTextColor
            | CardSide.Secondary -> cardVisuals.SecondaryTextColor
            
        let cueRequires (separator: string) (t: CardType list) =
            
            let icons =
                t
                |> List.map (fun t ->
                    let iconColor = CardType.iconColor t
                    let icon = CardType.icon t
                    
                    div {
                        attr.style $"color: {iconColor};"
                        
                        i { attr.``class`` $"fa-solid {icon}" }
                    }
                )
            
            div {
                attr.``class`` "cue-requires"
                
                icons
                |> List.join (text separator)
                |> Utils.renderList
            }
            
        let complexCue cue =
            div {
                attr.``class`` "cue-header-and-text-and-requires"
                
                if cue.Header.IsSome then
                    div {
                        attr.``class`` "cue-header"
                        text cue.Header.Value
                    }
                
                div {
                    attr.``class`` "cue-text-and-requires"
                    
                    div {
                        attr.``class`` "cue-text"
                        cue.Text
                    }
                    
                    if cue.Requires.IsSome then
                        match cue.Requires.Value with
                        | Logical.And v -> cueRequires "+" v
                        | Logical.Or v -> cueRequires "/" v
                }
            }
            
        let cardCue cue =
            match cue with
            | None -> Node.Empty ()
            | Some cue ->
                match cue with
                | Cue.Simple s -> text s
                | Cue.Icon fileName -> img { attr.src (Cue.iconUri this.Data.Type fileName) }
                | Cue.Complex cue -> complexCue cue
                
        let edgeFromRotation () =
            match rotation % 360 with
            | 0 -> CardEdge.Bottom
            
            | 270
            | -90 -> CardEdge.Left
            
            | 180
            | -180 -> CardEdge.Top
            
            | 90
            | -270 -> CardEdge.Right
            
            | _ -> failwith $"unexpected rotation: {rotation}"
                
        let isVisible edge =
            if this.ActiveEdge.IsNone then true
            else
                let rotatedEdge = edgeFromRotation ()
                
                match this.ActiveEdge.Value with
                | CardEdge.Bottom ->
                    edge = rotatedEdge
                | CardEdge.Top ->
                    edge = (CardEdge.opposite rotatedEdge)
                | _ -> failwith $"unexpected active edge value: {this.ActiveEdge.Value}"
                
        let cardCues cues =
            [
                (CardEdge.Bottom, cues.Bottom)
                (CardEdge.Left, cues.Left)
                (CardEdge.Top, cues.Top)
                (CardEdge.Right, cues.Right)
            ]
            |> List.map (fun (edge, cue) ->
                let cueKind =
                    cue |> Option.defaultValue (Cue.Simple String.empty) |> (fun x -> (Union.toString x).ToLower())
                    
                let visibility =
                    if isVisible edge then "visible" else "hidden"
                    
                let edgeName =
                    (Union.toString edge).ToLower() 
                
                div {
                    attr.``class`` $"cue {cueKind}-cue {cueKind}-{edgeName}-edge"
                    attr.style $"visibility: {visibility};"
                    
                    cardCue cue
                }
            )
            |> Utils.renderList
                
        let arrow className rotate icon =
            let arrowVisibility =
                if this.CanBeRotated && not this.IsFlipping && this.IsHovered then
                    "visibility: visible; opacity: 1;"
                else
                    "visibility: hidden; opacity: 0;"
                
            div {
                attr.``class`` $"arrow {className}"
                attr.style $"{arrowVisibility}color: {controlColor};"
                
                on.click rotate
                on.stopPropagation "click" true
                
                i { attr.``class`` $"fa-solid {icon}" }
            }
        
        div {
            attr.style $"width: {this.Size}px; height: {this.Size}px; position: relative;"
            
            on.mouseover (fun _ -> this.IsHovered <- true)
            on.mouseout (fun _ -> this.IsHovered <- false)
            
            div {
                attr.``class`` "flippable-card-container"
                attr.style $"transform: rotate({rotation}deg);"
                
                on.click flip
                
                div {
                    attr.``class`` $"card{this.FlippedClass ()}"
                    
                    on.event "transitionstart" (fun _ -> this.IsFlipping <- true)
                    on.event "transitionend" (fun _ -> this.IsFlipping <- false)
                    
                    div {
                        attr.``class`` "primary-side"
                        attr.style $"color: {cardVisuals.PrimaryTextColor}; background-color: {cardVisuals.ThemeColor};"
                    
                        cardCues this.Data.PrimarySide
                        
                        comp<CardBadge> {
                            "Visuals" => cardVisuals
                        }
                    }
                    
                    div {
                        attr.``class`` "secondary-side"
                        attr.style $"color: {cardVisuals.SecondaryTextColor}; background-color: #FFFFFF; border: 2px solid {cardVisuals.SecondaryTextColor};"
                        
                        cardCues this.Data.SecondarySide
                        
                        comp<CardBadge> {
                            "Visuals" => cardVisuals
                        }
                    }
                }
            }
            
            arrow "arrow-left" rotateCounterClockwise "fa-rotate-left"
            arrow "arrow-right" rotateClockwise "fa-rotate-right"
        }
