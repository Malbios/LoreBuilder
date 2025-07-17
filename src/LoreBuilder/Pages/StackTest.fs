namespace LoreBuilder.Pages

open System
open Bolero
open Bolero.Html
open FunSharp.Common
open LoreBuilder
open LoreBuilder.Components
open LoreBuilder.Model
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Web
open Microsoft.Extensions.Logging
open Plk.Blazor.DragDrop
open Radzen
open Radzen.Blazor

type StackTest() =
    inherit Component()
    
    let themedText cardType (text: string) =
        let cardVisuals = CardVisuals.fromCardType cardType
        
        div {
            attr.style $"color: {cardVisuals.FrontTextColor}; background-color: {cardVisuals.ThemeColor}; text-align: center;"
            
            text
        }
        
    let cards = Utils.cards
    
    let droppedCards = System.Collections.Generic.List<Card>()
    
    override _.CssScope = CssScopes.LoreBuilder
    
    [<Inject>]
    member val Logger : ILogger<StackTest> = Unchecked.defaultof<_> with get, set
    
    override this.Render() =
        
        let dropzoneList () =
            let list = System.Collections.Generic.List<Card>()
            let topCard = droppedCards |> Seq.toList |> List.tryHead
            
            if topCard.IsSome then
                list.Add(topCard.Value)
                
            list
            
        let onDrop (card: Card) =
            this.Logger.LogInformation $"card dropped: {card.Id} ({Union.toString card.Type})"
            droppedCards.Insert(0, card)
        
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
            
            comp<RadzenStack> {
                "Orientation" => Orientation.Vertical
                "Gap" => "1rem"
                
                div {
                    let dropzoneList = dropzoneList ()
                    
                    let dottedBorder =
                        if dropzoneList.Count = 0 then
                            " border: 2px dotted black;"
                        else
                            ""
                        
                    attr.style $"width: 270px; height: 270px;{dottedBorder}"
                    
                    comp<Dropzone<Card>> {
                        "Items" => dropzoneList
                        "Accepts" => Func<Card, Card, bool>(fun dragged target -> true) // TODO: handle 'target could be null'
                        "CopyItem" => Func<Card, Card>(Card.copy) 
                        "OnItemDrop" => EventCallbackFactory().Create(this, onDrop)
                        
                        attr.fragmentWith "ChildContent" (fun (card: Card) ->
                            comp<LoreBuilder.Components.Card> {
                                "Data" => card
                                "Size" => 270
                            }
                        )
                    }
                }
                
                comp<RadzenButton> {
                    let onClick (_: MouseEventArgs) =
                        droppedCards.Clear()
                        
                    "Text" => "Clear"
                    "Click" => EventCallback.Factory.Create<MouseEventArgs>(this, onClick)
                }
            }
            
            comp<RadzenStack> {
                "Orientation" => Orientation.Vertical
                "Gap" => "0.5rem"
                
                for card in droppedCards do
                    themedText card.Type $"{card.Id} ({Union.toString card.Type})"
            }
        }
