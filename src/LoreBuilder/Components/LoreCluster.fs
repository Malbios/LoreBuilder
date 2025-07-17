namespace LoreBuilder.Components

open System
open Bolero
open Bolero.Html
open FunSharp.Common
open LoreBuilder.Model
open Microsoft.AspNetCore.Components
open Plk.Blazor.DragDrop
open Radzen
open Radzen.Blazor

type LoreCluster() =
    inherit Component()
    
    override this.CssScope = CssScopes.LoreCluster
    
    [<Parameter>]
    member val Cards: Card list = List.empty with get, set
    
    [<Parameter>]
    member val OnDrop: Card -> unit = ignore with get, set

    override this.Render() =
        
        let cardStack =
            this.Cards
            |> ResizeArray
            |> System.Collections.Generic.List
            
        let themedText cardType (text: string) =
            let cardVisuals = CardVisuals.fromCardType cardType
            
            div {
                attr.style $"color: {cardVisuals.FrontTextColor}; background-color: {cardVisuals.ThemeColor}; text-align: center;"
                
                text
            }
            
        comp<RadzenStack> {
            "Orientation" => Orientation.Horizontal
            
            comp<Dropzone<Card>> {
                "Class" => "lore-cluster-dropzone"
                "Items" => cardStack
                "Accepts" => Func<Card, Card, bool>(fun dragged target -> true) // TODO: handle 'target could be null'
                "CopyItem" => Func<Card, Card>(fun card -> { card with Type = card.Type })
                "OnItemDrop" => EventCallbackFactory().Create(this, this.OnDrop)
            }
            
            comp<RadzenStack> {
                "Orientation" => Orientation.Vertical
                
                for card in this.Cards do
                    themedText card.Type $"{card.Id} ({Union.toString card.Type})"
            }
        }
