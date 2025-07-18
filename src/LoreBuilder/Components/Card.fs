namespace LoreBuilder.Components

open Bolero
open Bolero.Html
open Bolero.Node
open LoreBuilder.Model
open Microsoft.AspNetCore.Components
open Microsoft.Extensions.Logging

type Card() =
    inherit Component()
    
    let svgCardTypeLabel textColor cardType =
        
        """
<svg viewBox="0 0 100 100">
 	<defs>
		<path id="curve" d="M 10,60 A45,45 0 0,0 90,60" />
 	</defs>
 	<text
      fill="{TextColor}"
      font-size="10px"
      font-weight="bold"
      text-transform="uppercase"
      letter-spacing="2px"
    >
		<textPath href="#curve" startOffset="50%" text-anchor="middle">
 			{CardType}
 		</textPath>
 	</text>
</svg>
"""
        |> _.Replace("{TextColor}", textColor)
        |> _.Replace("{CardType}", cardType)
        |> RawHtml
        
    let cardSides sides =
        
        concat {
            div {
                attr.``class`` "side top"
                
                sides.Top
            }
            
            div {
                attr.``class`` "side bottom"
                
                sides.Bottom
            }
            
            div {
                attr.``class`` "side left"
                
                sides.Left
            }
            
            div {
                attr.``class`` "side right"
                
                sides.Right
            }
        }
        
    let cardCenter cardVisuals =
        
        div {
            attr.``class`` "center"
            
            div {
                attr.``class`` "outer-circle"
                attr.style $"background-color: {cardVisuals.ThemeColor};"
            }
            
            div {
                attr.``class`` "inner-circle"
            }
            
            div {
                attr.``class`` "icon"
                attr.style $"color: {cardVisuals.IconColor};"
                
                i { attr.``class`` $"fa-solid {cardVisuals.Icon} fa-2x" }
            }
            
            div {
                attr.``class`` "category"
                attr.style "top: 63%; left: 50%; transform: translate(-50%, -50%) rotate(0deg);"
                
                svgCardTypeLabel cardVisuals.FrontTextColor cardVisuals.Type
            }
            
            div {
                attr.``class`` "category"
                attr.style "top: 50%; left: 38%; transform: translate(-50%, -50%) rotate(90deg);"
                
                svgCardTypeLabel cardVisuals.FrontTextColor cardVisuals.Type
            }
            
            div {
                attr.``class`` "category"
                attr.style "top: 38%; left: 50%; transform: translate(-50%, -50%) rotate(180deg);"
                
                svgCardTypeLabel cardVisuals.FrontTextColor cardVisuals.Type
            }
            
            div {
                attr.``class`` "category"
                attr.style "top: 50%; left: 63%; transform: translate(-50%, -50%) rotate(270deg);"
                
                svgCardTypeLabel cardVisuals.FrontTextColor cardVisuals.Type
            }
        }
        
    let cardFront cardVisuals sides =
        
        div {
            attr.``class`` "card-front"
            attr.style $"color: {cardVisuals.FrontTextColor}; background-color: {cardVisuals.ThemeColor};"
        
            cardSides sides
            
            cardCenter cardVisuals
        }
        
    let cardBack cardVisuals (sides: Sides) =
        
        div {
            attr.``class`` "card-back"
            attr.style $"color: {cardVisuals.BackTextColor}; background-color: #FFFFFF;"
            
            cardSides sides
            
            cardCenter cardVisuals
        }
        
    let cardPlaceholder cardVisuals =
        
        div {
            attr.``class`` "card-placeholder"
            attr.style $"background-color: {cardVisuals.ThemeColor};"
            
            cardCenter cardVisuals
        }
    
    let mutable rotation = 0
    let mutable isFlipped = false
    let mutable isHovered = false
    let mutable isFlipping = false
    
    override _.CssScope = CssScopes.Card
    
    [<Inject>]
    member val Logger : ILogger<Card> = Unchecked.defaultof<_> with get, set
    
    [<Parameter>]
    member val Data: LoreBuilder.Model.Card = Card.empty with get, set
    
    [<Parameter>]
    member val Size: int = 0 with get, set
    
    [<Parameter>]
    member val Placeholder: bool = false with get, set
    
    member private this.FlippedClass () =
        if isFlipped then " flipped-card" else ""

    override this.Render() =
        
        let flip _ =
            if not this.Placeholder then
                isFlipped <- not isFlipped
                
        let rotateClockwise _ =
            rotation <- rotation + 90
            
        let rotateCounterClockwise _ =
            rotation <- rotation - 90
            
        let cardVisuals = CardVisuals.fromCardType this.Data.Type
        
        let controlColor = if isFlipped then cardVisuals.BackTextColor else cardVisuals.FrontTextColor
                
        let arrowWidget className rotate icon =
            let arrowVisibility =
                if not isFlipping && isHovered then
                    // "visibility: visible; opacity: 1; transition: opacity 0.6s ease;"
                    "visibility: visible; opacity: 1;"
                else
                    // "visibility: hidden; opacity: 0; transition: opacity 0.6s ease;"
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
                attr.style $"transform: rotate({rotation}deg); transition: transform 0.3s ease;"
                
                on.click flip
                
                div {
                    attr.``class`` $"card{this.FlippedClass ()}"
                    
                    on.event "transitionstart" (fun _ -> isFlipping <- true)
                    on.event "transitionend" (fun _ -> isFlipping <- false)
                    
                    if this.Placeholder then
                        cardPlaceholder cardVisuals
                    else
                        cardFront cardVisuals this.Data.Front
                        
                        cardBack cardVisuals this.Data.Back
                }
            }
                
            if not this.Placeholder then
                arrowWidget "arrow-left" rotateCounterClockwise "fa-rotate-left"
                arrowWidget "arrow-right" rotateClockwise "fa-rotate-right"
        }
