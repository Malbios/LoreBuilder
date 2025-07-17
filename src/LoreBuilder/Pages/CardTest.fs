namespace LoreBuilder.Pages

open Bolero
open Bolero.Html
open LoreBuilder.Components
open Radzen
open Radzen.Blazor
open LoreBuilder

type CardTest() =
    inherit Component()
    
    override _.CssScope = CssScopes.LoreBuilder
    
    override this.Render() =
        
        comp<RadzenStack> {
            "Orientation" => Orientation.Horizontal
            "Wrap" => FlexWrap.Wrap
            "Gap" => "1rem"
            
            for card in Utils.cards do
                comp<Card> {
                    "Size" => 270
                    "Data" => card
                }
        }
