namespace LoreBuilder.Components

open System
open Bolero
open Bolero.Html
open FunSharp.Common
open LoreBuilder.Model
open Microsoft.AspNetCore.Components
open Microsoft.Extensions.Logging
open Plk.Blazor.DragDrop

type LoreCluster() =
    inherit Component()
    
    // TODO: implement a lore cluster
    
    let droppedCards = System.Collections.Generic.List<Card>()
    
    override _.CssScope = CssScopes.LoreCluster
    
    [<Inject>]
    member val Logger : ILogger<LoreCluster> = Unchecked.defaultof<_> with get, set
    
    (*
.stack-container {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.card {
  width: 270px;
  aspect-ratio: 1;
  border-radius: 20px;
  background: #3A2178;
}

.overlap {
  margin-top: -30%; /* pull upward */
  background: #C2B452;
  z-index: -1;
}
    *)

    override this.Render() =
        
        let onDrop card =
            this.Logger.LogInformation $"card dropped: {card.Id} ({Union.toString card.Type})"
            
        concat {
            div {
                let dottedBorder =
                    if droppedCards.Count = 0 then
                        " border: 2px dotted black;"
                    else
                        ""
                                    
                attr.style $"width: 270px; height: 270px;{dottedBorder}"
                
                comp<Dropzone<Card>> {
                    "Items" => droppedCards
                    "Accepts" => Func<Card, Card, bool>(fun dragged target -> true) // TODO: handle 'target could be null'
                    "OnItemDrop" => EventCallbackFactory().Create(this, onDrop)
                }
            }
            
            div {
                let card = droppedCards |> Seq.toList |> List.tryHead |> Option.defaultValue Card.empty
                
                attr.style ""
                
                comp<LoreBuilder.Components.Card> {
                    "Data" => card
                    "Size" => 270
                }
            }
        }
