namespace LoreBuilder.Components

open Bolero
open Bolero.Html
open Bolero.Node
open LoreBuilder.Model
open Microsoft.AspNetCore.Components

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
        
    let mutable isFlipped = true
    let flippedClass () = if isFlipped then " flipped" else ""
    
    override _.CssScope = CssScopes.Card
    
    [<Parameter>]
    member val Data: CardData = CardData.empty with get, set

    override this.Render() =
        
        let cardVisuals = CardVisuals.fromCardType this.Data.Type
        
        div {
            attr.``class`` $"card-flip{flippedClass ()}"
                
            on.click (fun _ -> isFlipped <- not isFlipped)
            
            div {
                attr.``class`` "card"
                
                cardFront cardVisuals this.Data.Front
                
                cardBack cardVisuals this.Data.Back
            }
        }
