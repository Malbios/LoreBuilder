namespace LoreBuilder.Components

open Bolero
open Bolero.Html
open Bolero.Node
open LoreBuilder.Model
open Microsoft.AspNetCore.Components

type CardBadge() =
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
    
    override _.CssScope = CssScopes.CardBadge
    
    [<Parameter>]
    member val Visuals: CardVisuals = CardVisuals.empty with get, set

    override this.Render() =
        
        let category top left rotation =
            let top = $"{top}" + "%"
            let left = $"{left}" + "%"
            let translate = "translate(-50%, -50%)"
            
            div {
                attr.``class`` "category"
                attr.style $"top: {top}; left: {left}; transform: {translate} rotate({rotation}deg);"
                
                svgCardTypeLabel this.Visuals.FrontTextColor this.Visuals.Type
            }
        
        div {
            attr.``class`` "card-badge"
            
            div {
                attr.``class`` "outer-circle"
                attr.style $"background-color: {this.Visuals.ThemeColor};"
            }
            
            div {
                attr.``class`` "inner-circle"
            }
            
            div {
                attr.``class`` "icon"
                attr.style $"color: {this.Visuals.IconColor};"
                
                i { attr.``class`` $"fa-solid {this.Visuals.Icon} fa-2x" }
            }
            
            category 63 50 0
            category 50 38 90
            category 38 50 180
            category 50 63 270
        }
