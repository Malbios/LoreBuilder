namespace LoreBuilder.Pages

open Bolero
open Bolero.Html
open Elmish
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
        
        let onDropHandler (card: Card) =
            this.Logger.LogInformation $"OnDropHandler: {card.Data.Id}"
            dispatch (StackTest.Message.Drop card)
        
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
                
                comp<Dropzone<Card>> {
                    "Class" => "single-card-drop"
                    "MaxItems" => 1
                    "Items" => cardStack
                    "OnItemDrop" => EventCallbackFactory().Create(this, onDropHandler)
                    
                    attr.fragmentWith "ChildContent" (fun (item: Card) ->
                        comp<Components.Card> {
                            "Data" => item.Data
                        }
                    )
                }
            }
        }

[<RequireQualifiedAccess>]
module StackTest =
    
    let update (logger: ILogger) message (model: StackTest.State) =
        
        match message with
        
        | StackTest.Message.Drop card ->
            logger.LogInformation $"StackTest.Message.Drop: {card.Data.Id}"
            { model with Cards = [card] @ model.Cards }, Cmd.none
