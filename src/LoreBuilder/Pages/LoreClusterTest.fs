namespace LoreBuilder.Pages

open System.Net.Http
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

    [<Inject>]
    member val Http: HttpClient = Unchecked.defaultof<_> with get, set

    member this.Cards =
        Utils.allCards ()

    member this.TriggerReRender() =
        this.StateHasChanged()

    // Idempotent (see CardData.loadAsync) - harmless even if Pages/Home.fs's own copy of this call
    // already loaded the pool first.
    override this.OnInitializedAsync() =
        task { do! CardData.loadAsync this.Http this.Logger }
        :> System.Threading.Tasks.Task

    override this.Render() =
        
        comp<RadzenStack> {
            "Orientation" => Orientation.Horizontal
            "Gap" => "0.5rem"
            
            div {
                attr.``class`` "card-stack"
                
                for cards in this.Cards do
                    comp<CardStack> {
                        attr.key (List.head cards).Type
                        "Size" => 110
                        "Cards" => cards
                        "OnDragStart" => fun (_: LoreBuilder.Model.Card) ->
                            model <- { model with IsDragging = true }
                            this.TriggerReRender()
                        "OnDragEnd" => fun () ->
                            model <- { model with IsDragging = false }
                            this.TriggerReRender()
                    }
            }
            
            div {
                attr.``class`` "test-clusters"
                
                for index in [ 1..4 ] do
                    comp<LoreCluster> {
                        attr.key index
                        "DropzonesAreActive" => model.IsDragging
                    }
            }
        }
