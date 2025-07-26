namespace LoreBuilder.Components

open Bolero
open Bolero.Html
open LoreBuilder
open LoreBuilder.Model
open Microsoft.AspNetCore.Components

type Card() =
    inherit Component()
        
    let cardCues cues =
        [
            ("top", cues.Top)
            ("bottom", cues.Bottom)
            ("left", cues.Left)
            ("right", cues.Right)
        ]
        |> List.map (fun item ->
            div {
                attr.``class`` $"cue {fst item}"
                snd item
            }
        )
        |> Utils.renderList
    
    override _.CssScope = CssScopes.Card
    
    [<Parameter>]
    member val Data = Card.empty with get, set
    
    [<Parameter>]
    member val CurrentSide = CardSide.Primary with get, set
    
    [<Parameter>]
    member val Rotation = 0 with get, set
    
    [<Parameter>]
    member val IsHovered = false with get, set
    
    [<Parameter>]
    member val IsFlipping = false with get, set
    
    [<Parameter>]
    member val CanBeFlipped = true with get, set
    
    [<Parameter>]
    member val CanBeRotated = true with get, set
    
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
            this.Rotation <- this.Rotation + 90
            
        let rotateCounterClockwise _ =
            this.Rotation <- this.Rotation - 90
            
        let cardVisuals =
            CardVisuals.fromCardType this.Data.Type
        
        let controlColor =
            match this.CurrentSide with
            | CardSide.Primary -> cardVisuals.PrimaryTextColor
            | CardSide.Secondary -> cardVisuals.SecondaryTextColor
                
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
                attr.style $"transform: rotate({this.Rotation}deg);"
                
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
