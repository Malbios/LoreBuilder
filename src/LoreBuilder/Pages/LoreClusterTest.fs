namespace LoreBuilder.Pages

open Bolero
open Bolero.Html
open LoreBuilder
open LoreBuilder.Components
open Microsoft.AspNetCore.Components
open Microsoft.Extensions.Logging
open Radzen
open Radzen.Blazor

type private Model = {
    IsDragging: bool
}

type LoreClusterTest() =
    inherit Component()
    
    let cards = Utils.allCards
    
    let mutable model = {
        IsDragging = false
    }
    
    override _.CssScope = CssScopes.LoreBuilder
    
    [<Inject>]
    member val Logger : ILogger<LoreClusterTest> = Unchecked.defaultof<_> with get, set
    
    member this.TriggerReRender() =
        this.StateHasChanged()
    
    override this.Render() =
        
        comp<RadzenStack> {
            "Orientation" => Orientation.Horizontal
            "Gap" => "0.5rem"
            
            div {
                attr.``class`` "card-stack"
                
                for cards in cards do
                    comp<CardStack> {
                        "Size" => 110
                        "Cards" => cards
                        "OnDragStart" => fun () ->
                            model <- { model with IsDragging = true }
                            this.TriggerReRender()
                        "OnDragEnd" => fun () ->
                            model <- { model with IsDragging = false }
                            this.TriggerReRender()
                    }
            }
            
            comp<LoreCluster> {
                "DropzonesAreActive" => model.IsDragging
            }
        }
