namespace LoreBuilder.Pages

open Bolero
open Bolero.Html
open LoreBuilder.Components
open Radzen
open Radzen.Blazor
open LoreBuilder

type CardTest() =
    inherit Component()
    
    override this.Render() =
        
        div {
            attr.``class`` "content"
            
            comp<RadzenStack> {
                "Orientation" => Orientation.Horizontal
                "Wrap" => FlexWrap.Wrap
                "Gap" => "1rem"
                
                for card in Utils.cards do
                    comp<Card> {
                        "Data" => card
                    }
            }
        }
