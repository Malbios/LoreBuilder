namespace LoreBuilder.Components

open System
open Bolero
open Bolero.Html
open Bolero.Node
open LoreBuilder.Model
open Microsoft.AspNetCore.Components
open FunSharp.Common
open Microsoft.Extensions.Logging
open Microsoft.JSInterop

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
    
    let setDragData (js: IJSRuntime) (e: obj) (data: string) =
        js.InvokeVoidAsync("dragDropHelper.setData", e, data)
        |> ignore
    
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
    
    [<Inject>]
    member val JSRuntime: IJSRuntime = Unchecked.defaultof<_> with get, set
    
    [<Inject>]
    member val Logger : ILogger<Card> = Unchecked.defaultof<_> with get, set
    
    [<Parameter>]
    member val Data: CardData = CardData.empty with get, set

    override this.Render() =
        
        let themeColor = CardType.themeColor this.Data.Type
        let textColor = CardType.textColor this.Data.Type
                
        div {
            attr.``class`` "card"
            attr.style $"color: {textColor}; background-color: {themeColor};"
            attr.draggable true
            
            on.dragstart (fun e ->
                this.Logger.LogInformation $"drag start"
                setDragData this.JSRuntime e (this.Data.Id.ToString())
            )
            
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
