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
    
    let mutable model = {
        IsDragging = false
    }
    
    override _.CssScope = CssScopes.LoreBuilder
    
    [<Inject>]
    member val Logger : ILogger<LoreClusterTest> = Unchecked.defaultof<_> with get, set
        
    member this.Cards =
        
        Utils.allCards
        |> List.map (fun x ->
            match x with
            | Ok v -> v
            | Error error ->
                this.Logger.LogError $"{error}"
                List.Empty
        ) 
        
    member this.TriggerReRender() =
        this.StateHasChanged()
    
    override this.Render() =
        
        comp<RadzenStack> {
            "Orientation" => Orientation.Horizontal
            "Gap" => "0.5rem"
            
            div {
                attr.``class`` "card-stack"
                
                for cards in this.Cards do
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
            
            div {
                attr.``class`` "test-clusters"
                
                for _ in [1..4] do
                    comp<LoreCluster> {
                        "DropzonesAreActive" => model.IsDragging
                    }
            }
        }
