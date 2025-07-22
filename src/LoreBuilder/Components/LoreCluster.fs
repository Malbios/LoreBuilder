namespace LoreBuilder.Components

open System
open Bolero
open Bolero.Html
open FunSharp.Common
open LoreBuilder.Model
open Microsoft.AspNetCore.Components
open Microsoft.Extensions.Logging
open Plk.Blazor.DragDrop

[<RequireQualifiedAccess>]
type Orientation =
    | Center
    | Bottom
    | Left
    | Top
    | Right
    
module Orientation =
    
    let fromIndex index =
        
        match index with
        | 0 -> Orientation.Center
        | 1 -> Orientation.Bottom
        | 2 -> Orientation.Left
        | 3 -> Orientation.Top
        | 4 -> Orientation.Right
        | _ -> failwith $"unexpected index: {index}"

type LoreCluster() =
    inherit Component()
        
    let cardWithStyle index card =
        let className = (Union.toString (Orientation.fromIndex index)).ToLower()
        
        let rotation =
            match Orientation.fromIndex index with
            | Orientation.Center -> String.empty
            | Orientation.Bottom -> "transform: rotate(180deg);"
            | Orientation.Left -> "transform: rotate(270deg);"
            | Orientation.Top -> String.Empty
            | Orientation.Right -> "transform: rotate(90deg);"
            
        div {
            attr.``class`` className
            attr.style rotation
            
            if card <> Card.empty then
                comp<LoreBuilder.Components.Card> {
                    "Data" => card
                    "Size" => 270
                }
            else
                div {
                    attr.style "width: 270px; height: 270px;"
                }
        }
    
    let droppedCards = System.Collections.Generic.List<Card>()
    
    override _.CssScope = CssScopes.LoreCluster
    
    [<Inject>]
    member val Logger : ILogger<LoreCluster> = Unchecked.defaultof<_> with get, set
    
    [<Parameter>]
    member val DropzoneIsActive : bool = false with get, set
    
    [<Parameter>]
    member val Lore : string = String.empty with get, set

    override this.Render() =
        
        let accept _ _ = // dropped, target, target could be null
            droppedCards.Count < 5
        
        let onDrop card =
            this.Logger.LogInformation $"card dropped: {card.Id} ({Union.toString card.Type})"
            droppedCards.Add(card)
        
        div {
            attr.``class`` "cluster-exterior"
            
            div {
                attr.``class`` "cluster-interior"
                
                div {
                    let blinkerClass =
                        if this.DropzoneIsActive then "blink_me" else String.empty
                    
                    let pointerEvents =
                        if this.DropzoneIsActive then " pointer-events: auto;" else " pointer-events: none;"
                    
                    attr.``class`` $"{blinkerClass}"
                    attr.style $"position: absolute; width: 270px; height: 270px; z-index: 6;{pointerEvents}"
                    
                    comp<Dropzone<Card>> {
                        "MaxItems" => 1
                        "Items" => System.Collections.Generic.List<Card>()
                        "Accepts" => Func<Card, Card, bool>(accept)
                        "OnItemDrop" => EventCallbackFactory().Create(this, onDrop)
                    }
                }
                
                let cardsWithStyle =
                    [0..4]
                    |> List.map (fun n ->
                        droppedCards
                        |> Seq.toList
                        |> List.tryItem n
                        |> Option.defaultValue Card.empty
                    )
                    |> List.mapi cardWithStyle
                
                concat {
                    for card in cardsWithStyle do
                        yield card
                }
            }
        }
