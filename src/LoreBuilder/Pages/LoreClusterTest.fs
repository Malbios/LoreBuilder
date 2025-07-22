namespace LoreBuilder.Pages

open Bolero
open Bolero.Html
open FunSharp.Common
open LoreBuilder
open LoreBuilder.Components
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Web
open Microsoft.Extensions.Logging
open Radzen
open Radzen.Blazor

type LoreClusterTest() =
    inherit Component()
    
    let cards = Utils.cards
    
    let mutable isDragging = false
    
    let mutable lore = String.empty
    
    override _.CssScope = CssScopes.LoreBuilder
    
    [<Inject>]
    member val Logger : ILogger<LoreCluster> = Unchecked.defaultof<_> with get, set
    
    member this.TriggerRender() =
        this.StateHasChanged()
    
    override this.Render() =
        
        comp<RadzenStack> {
            "Orientation" => Orientation.Horizontal
            "Gap" => "0.5rem"
            
            div {
                attr.style "display: grid; grid-template-columns: repeat(2, 1fr); gap: 0.5rem;"
                
                for card in cards do
                    comp<CardStack> {
                        "Size" => 110
                        "Cards" => [card]
                        "OnDragStart" => fun () ->
                            this.Logger.LogInformation "isDragging <- true"
                            isDragging <- true
                            this.TriggerRender()
                        "OnDragEnd" => fun () ->
                            this.Logger.LogInformation "isDragging <- false"
                            isDragging <- false
                            this.TriggerRender()
                    }
            }
            
            comp<LoreCluster> {
                "DropzoneIsActive" => isDragging
                "Lore" => lore
            }
            
            comp<RadzenButton> {
                "Text" => "Print Lore"
                "Style" => "width: 100px; height: 100px;"
                "Click" => EventCallback.Factory.Create(this, fun (_: MouseEventArgs) -> printfn $"Lore: {lore}")
            }
        }
