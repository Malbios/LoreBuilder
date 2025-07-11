namespace LoreBuilder.Pages

open System
open Bolero
open Bolero.Html
open Elmish
open FunSharp.Common
open LoreBuilder
open LoreBuilder.Model
open Microsoft.AspNetCore.Components
open Microsoft.Extensions.Logging
open Radzen
open Radzen.Blazor
open Plk.Blazor.DragDrop

type StackTest() =
    inherit ElmishComponent<StackTest.State, StackTest.Message>()
    
    let cardList: System.Collections.Generic.List<Card> =
        Utils.cards
        |> List.map (fun cardData -> { IsFlipped = false; Data = cardData })
        |> ResizeArray
        |> System.Collections.Generic.List
        
    [<Inject>]
    member val Logger : ILogger<StackTest> = Unchecked.defaultof<_> with get, set

    override this.ShouldRender(oldModel, newModel) =
        
        oldModel.Cards <> newModel.Cards
    
    override this.View model dispatch =
        
        let cardStack =
            model.Cards
            |> ResizeArray
            |> System.Collections.Generic.List
        
        div {
            attr.``class`` "content"
            
            comp<RadzenStack> {
                "Orientation" => Orientation.Vertical
                
                comp<Dropzone<Card>> {
                    "Items" => cardList
                    
                    attr.fragmentWith "ChildContent" (fun (item: Card) ->
                        comp<Components.Card> {
                            "IsFlipped" => item.IsFlipped
                            "Data" => item.Data
                        }
                    )
                }
                
                comp<RadzenStack> {
                    "Orientation" => Orientation.Horizontal
                    
                    comp<Dropzone<Card>> {
                        "Class" => "card-stack"
                        "Items" => cardStack
                        "Accepts" => Func<Card, Card, bool>(fun dragged target -> true) // target could be null
                        "CopyItem" => Func<Card, Card>(fun card -> { card with Data = card.Data })
                        "OnItemDrop" => EventCallbackFactory().Create(this, (fun card ->
                            this.Logger.LogInformation $"cardStack.Count: {cardStack.Count}"
                            dispatch (StackTest.Message.Drop card)
                            cardStack.Clear()
                            this.Logger.LogInformation $"cardStack.Count: {cardStack.Count}"
                        ))
                    }
                    
                    comp<RadzenStack> {
                        "Orientation" => Orientation.Vertical
                        
                        for card in model.Cards do
                            let cardVisuals = CardVisuals.fromCardType card.Data.Type
                            
                            div {
                                attr.style $"color: {cardVisuals.FrontTextColor}; background-color: {cardVisuals.ThemeColor};"
                                
                                text $"{card.Data.Id} ({Union.toString card.Data.Type})"
                            }
                    }
                }
            }
        }

[<RequireQualifiedAccess>]
module StackTest =
    
    let update (_: ILogger) message (model: StackTest.State) =
        
        match message with
        
        | StackTest.Message.Drop card ->
            { model with Cards = [card] @ model.Cards }, Cmd.none
