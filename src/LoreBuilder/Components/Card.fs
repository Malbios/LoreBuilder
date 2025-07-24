namespace LoreBuilder.Components

open Bolero
open Bolero.Html
open LoreBuilder
open LoreBuilder.Model
open Microsoft.AspNetCore.Components

type Card() =
    inherit Component()
        
    let cardSides sides =
        
        let side (className: string) (text: string) =
            div {
                attr.``class`` $"side {className}"
                
                text
            }
        
        [
            ("top", sides.Top)
            ("bottom", sides.Bottom)
            ("left", sides.Left)
            ("right", sides.Right)
        ]
        |> List.map (fun x -> side (fst x) (snd x))
        |> Utils.renderList
        
    let cardFront cardVisuals sides =
        
        div {
            attr.``class`` "card-front"
            attr.style $"color: {cardVisuals.FrontTextColor}; background-color: {cardVisuals.ThemeColor};"
        
            cardSides sides
            
            comp<CardBadge> {
                "Visuals" => cardVisuals
            }
        }
        
    let cardBack cardVisuals (sides: Sides) =
        
        div {
            attr.``class`` "card-back"
            attr.style $"color: {cardVisuals.BackTextColor}; background-color: #FFFFFF;"
            
            cardSides sides
            
            comp<CardBadge> {
                "Visuals" => cardVisuals
            }
        }
    
    let mutable rotation = 0
    let mutable isFlipped = false
    let mutable isHovered = false
    let mutable isFlipping = false
    
    override _.CssScope = CssScopes.Card
    
    [<Parameter>]
    member val Data: LoreBuilder.Model.Card = Card.empty with get, set
    
    [<Parameter>]
    member val Size: int = 0 with get, set
    
    [<Parameter>]
    member val CanBeFlipped: bool = true with get, set
    
    [<Parameter>]
    member val CanBeRotated: bool = true with get, set
    
    member private this.FlippedClass () =
        if isFlipped then " flipped-card" else ""

    override this.Render() =
        
        let flip _ =
            if this.CanBeFlipped then
                isFlipped <- not isFlipped
                
        let rotateClockwise _ =
            rotation <- rotation + 90
            
        let rotateCounterClockwise _ =
            rotation <- rotation - 90
            
        let cardVisuals = CardVisuals.fromCardType this.Data.Type
        
        let controlColor = if isFlipped then cardVisuals.BackTextColor else cardVisuals.FrontTextColor
                
        let arrowWidget className rotate icon =
            let arrowVisibility =
                if this.CanBeRotated && not isFlipping && isHovered then
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
            
            on.mouseover (fun _ -> isHovered <- true)
            on.mouseout (fun _ -> isHovered <- false)
            
            div {
                attr.``class`` "flippable-card-container"
                attr.style $"transform: rotate({rotation}deg);"
                
                on.click flip
                
                div {
                    attr.``class`` $"card{this.FlippedClass ()}"
                    
                    on.event "transitionstart" (fun _ -> isFlipping <- true)
                    on.event "transitionend" (fun _ -> isFlipping <- false)
                    
                    cardFront cardVisuals this.Data.Front
                    cardBack cardVisuals this.Data.Back
                }
            }
            
            arrowWidget "arrow-left" rotateCounterClockwise "fa-rotate-left"
            arrowWidget "arrow-right" rotateClockwise "fa-rotate-right"
        }
