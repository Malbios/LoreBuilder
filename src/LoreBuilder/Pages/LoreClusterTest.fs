namespace LoreBuilder.Pages

open Bolero
open Bolero.Html
open LoreBuilder
open LoreBuilder.Components
open Radzen
open Radzen.Blazor

type LoreClusterTest() =
    inherit Component()
    
    let cards = Utils.cards
    
    override _.CssScope = CssScopes.LoreBuilder
    
    override _.Render() =
        
        comp<RadzenStack> {
            "Orientation" => Orientation.Horizontal
            "Gap" => "0.5rem"
            
            div {
                attr.style "display: grid; grid-template-columns: repeat(2, 1fr); gap: 0.5rem;"
                
                for card in cards do
                    comp<CardStack> {
                        "Size" => 110
                        "Cards" => [card]
                    }
            }
            
            comp<LoreCluster> { attr.empty() }
        }
