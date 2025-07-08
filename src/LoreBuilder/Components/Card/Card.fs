namespace LoreBuilder.Components

open Bolero
open Bolero.Html
open Bolero.Node
open LoreBuilder.Model
open Microsoft.AspNetCore.Components
open FunSharp.Common

type CardData = {
    Type: CardType
    Top: string
    Right: string
    Bottom: string
    Left: string
}

module CardData =
    
    let empty = {
        Type = CardType.Unknown
        Top = String.empty
        Right = String.empty
        Bottom = String.empty
        Left = String.empty
    }

type Card() =
    inherit Component()
    
    let svg category =
        """
<svg class="category" viewBox="0 0 100 100">
 	<defs>
		<path id="curve" d="M 10,70 A45,45 0 0,0 90,70" />
 	</defs>
 	<text>
		<textPath href="#curve" startOffset="50%" text-anchor="middle">
 			{category}
 		</textPath>
 	</text>
</svg>
"""
        |> _.Replace("{category}", category)
        |> RawHtml
    
    override _.CssScope = CssScopes.Card
    
    [<Parameter>]
    member val Data: CardData = CardData.empty with get, set

    override this.Render() =
                
        div {
            attr.``class`` "card"
            
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
                    attr.``class`` "circle"
                }
                
                div {
                    attr.``class`` "icon"
                    
                    i { attr.``class`` "fa-solid fa-user fa-2x" }
                }
                
                div {
                    attr.``class`` "category"
                    
                    Union.toString this.Data.Type
                    |> svg
                }
            }
        }
