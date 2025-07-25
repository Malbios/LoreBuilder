namespace LoreBuilder.Components

open Bolero
open Bolero.Html
open LoreBuilder
open LoreBuilder.Model
open Microsoft.AspNetCore.Components

type CardSide =
    | Front
    | Back

type Card() =
    inherit Component()
        
    let cardSides sides =
        [
            ("top", sides.Top)
            ("bottom", sides.Bottom)
            ("left", sides.Left)
            ("right", sides.Right)
        ]
        |> List.map (fun item ->
            div {
                attr.``class`` $"side {fst item}"
                snd item
            }
        )
        |> Utils.renderList
    
    override _.CssScope = CssScopes.Card
    
    [<Parameter>]
    member val Data = Card.empty with get, set
    
    [<Parameter>]
    member val CurrentSide = CardSide.Front with get, set
    
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
        | Front -> ""
        | Back -> " flipped-card"

    override this.Render() =
        
        let flip _ =
            if this.CanBeFlipped then
                this.CurrentSide <-
                    match this.CurrentSide with
                    | Front -> CardSide.Back
                    | Back -> CardSide.Front
                
        let rotateClockwise _ =
            this.Rotation <- this.Rotation + 90
            
        let rotateCounterClockwise _ =
            this.Rotation <- this.Rotation - 90
            
        let cardVisuals = CardVisuals.fromCardType this.Data.Type
        
        let controlColor =
            match this.CurrentSide with
            | Front -> cardVisuals.FrontTextColor
            | Back -> cardVisuals.BackTextColor
                
        let arrowWidget className rotate icon =
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
                        attr.``class`` "card-front"
                        attr.style $"color: {cardVisuals.FrontTextColor}; background-color: {cardVisuals.ThemeColor};"
                    
                        cardSides this.Data.Front
                        
                        comp<CardBadge> {
                            "Visuals" => cardVisuals
                        }
                    }
                    
                    div {
                        attr.``class`` "card-back"
                        attr.style $"color: {cardVisuals.BackTextColor}; background-color: #FFFFFF;"
                        
                        cardSides this.Data.Back
                        
                        comp<CardBadge> {
                            "Visuals" => cardVisuals
                        }
                    }
                }
            }
            
            arrowWidget "arrow-left" rotateCounterClockwise "fa-rotate-left"
            arrowWidget "arrow-right" rotateClockwise "fa-rotate-right"
        }
