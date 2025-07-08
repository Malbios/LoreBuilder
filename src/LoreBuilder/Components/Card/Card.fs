namespace LoreBuilder.Components

open System
open Bolero
open Bolero.Html
open Bolero.Node
open LoreBuilder.Model
open Microsoft.AspNetCore.Components
open FunSharp.Common

type CardData = {
    Id: Guid
    Type: CardType
    Top: string
    Right: string
    Bottom: string
    Left: string
}

module CardData =
    
    let empty = {
        Id = Guid.Empty
        Type = CardType.Unknown
        Top = String.empty
        Right = String.empty
        Bottom = String.empty
        Left = String.empty
    }

type Card() =
    inherit Component()
    
    let svg textColor cardType =
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
    
    override _.CssScope = CssScopes.Card
    
    [<Parameter>]
    member val Data: CardData = CardData.empty with get, set

    override this.Render() =
        
        let themeColor = CardType.themeColor this.Data.Type
        let textColor = CardType.textColor this.Data.Type
        
        div {
            attr.``class`` "card"
            attr.style $"color: {textColor}; background-color: {themeColor};"
            
            div {
                attr.``class`` "side top"
                
                this.Data.Top
            }
            
            div {
                attr.``class`` "side right"
                
                this.Data.Right
            }
            
            div {
                attr.``class`` "side bottom"
                
                this.Data.Bottom
            }
            
            div {
                attr.``class`` "side left"
                
                this.Data.Left
            }
            
            div {
                attr.``class`` "center"
                
                div {
                    attr.``class`` "outer-circle"
                    attr.style $"background-color: {themeColor};"
                }
                
                div {
                    attr.``class`` "inner-circle"
                }
                
                div {
                    attr.``class`` "icon"
                    attr.style $"color: {themeColor};"
                    
                    i { attr.``class`` "fa-solid fa-user fa-2x" }
                }
                
                div {
                    attr.``class`` "category"
                    
                    svg textColor (Union.toString this.Data.Type)
                }
            }
        }
