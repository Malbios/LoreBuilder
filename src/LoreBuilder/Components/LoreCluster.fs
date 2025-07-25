namespace LoreBuilder.Components

open System
open System.Collections.Generic
open Bolero
open Bolero.Html
open FunSharp.Common
open LoreBuilder.Model
open Microsoft.AspNetCore.Components
open Microsoft.Extensions.Logging
open Plk.Blazor.DragDrop

[<RequireQualifiedAccess>]
type ClusterPosition =
    | Center
    | Inner_Bottom
    | Outer_Bottom
    | Inner_Left
    | Outer_Left
    | Inner_Top
    | Outer_Top
    | Inner_Right
    | Outer_Right
    
module ClusterPosition =
    
    let fromIndex index =
        
        match index with
        | 0 -> ClusterPosition.Center
        | 1 -> ClusterPosition.Inner_Bottom
        | 2 -> ClusterPosition.Inner_Left
        | 3 -> ClusterPosition.Inner_Top
        | 4 -> ClusterPosition.Inner_Right
        | 5 -> ClusterPosition.Outer_Bottom
        | 6 -> ClusterPosition.Outer_Left
        | 7 -> ClusterPosition.Outer_Top
        | 8 -> ClusterPosition.Outer_Right
        | _ -> failwith $"unexpected index: {index}"
        
    let toRotation position =
        
        match position with
        | ClusterPosition.Center
        | ClusterPosition.Inner_Top
        | ClusterPosition.Outer_Top ->
            String.empty
        | ClusterPosition.Inner_Right
        | ClusterPosition.Outer_Right ->
            "transform: rotate(90deg);"
        | ClusterPosition.Inner_Bottom
        | ClusterPosition.Outer_Bottom ->
            "transform: rotate(180deg);"
        | ClusterPosition.Inner_Left
        | ClusterPosition.Outer_Left ->
            "transform: rotate(270deg);"
            
    let toString position =
        
        (Union.toString position).ToLower()
        
    let emptyDict =
        let dict = Dictionary<ClusterPosition, Card>()
        
        Union.toList<ClusterPosition>()
        |> List.iter (fun position -> dict[position] <- Card.empty)
        
        dict

type LoreCluster() =
    inherit Component()
        
    let cards = ClusterPosition.emptyDict
    
    let hasCard position =
        cards[position] <> Card.empty
        
    let showDropzone position =
        match position with
        | ClusterPosition.Center ->
            not (hasCard ClusterPosition.Inner_Bottom || hasCard ClusterPosition.Inner_Left
                 || hasCard ClusterPosition.Inner_Top || hasCard ClusterPosition.Inner_Right)
        
        | ClusterPosition.Inner_Bottom
        | ClusterPosition.Inner_Left
        | ClusterPosition.Inner_Top
        | ClusterPosition.Inner_Right -> hasCard ClusterPosition.Center
        
        | ClusterPosition.Outer_Bottom -> hasCard ClusterPosition.Inner_Bottom
        | ClusterPosition.Outer_Left -> hasCard ClusterPosition.Inner_Left
        | ClusterPosition.Outer_Top -> hasCard ClusterPosition.Inner_Top
        | ClusterPosition.Outer_Right -> hasCard ClusterPosition.Inner_Right
    
    override _.CssScope = CssScopes.LoreCluster
    
    [<Inject>]
    member val Logger : ILogger<LoreCluster> = Unchecked.defaultof<_> with get, set
    
    [<Parameter>]
    member val DropzonesAreActive = false with get, set
    
    [<Parameter>]
    member val Lore = String.empty with get, set
    
    [<Parameter>]
    member val OnCardReplace: Card -> unit = ignore with get, set

    override this.Render() =
            
        let cardAndDropzone position =
            let card = cards[position]
            let cardClassName = ClusterPosition.toString position
            let dropzoneClassName = $"{cardClassName}-dropzone"
            let rotation = ClusterPosition.toRotation position
            
            let acceptDrop (card: Card) _ = // droppedCard, target (target could be null)
                match position with
                | ClusterPosition.Center -> true
                
                | ClusterPosition.Inner_Bottom
                | ClusterPosition.Inner_Left
                | ClusterPosition.Inner_Top
                | ClusterPosition.Inner_Right ->
                    card.Type = cards[ClusterPosition.Center].Type
                    
                | ClusterPosition.Outer_Bottom -> true // TODO: based on inner
                | ClusterPosition.Outer_Left -> true // TODO: based on inner
                | ClusterPosition.Outer_Top -> true // TODO: based on inner
                | ClusterPosition.Outer_Right -> true // TODO: based on inner
            
            let onDrop card =
                this.Logger.LogInformation $"position: {position}"
                let oldCard = cards[position]
                cards[position] <- card
                if oldCard <> Card.empty then this.OnCardReplace(oldCard)
            
            let blinkerClass =
                if this.DropzonesAreActive then " blink_me" else String.empty
            
            let pointerEventsClass =
                if this.DropzonesAreActive then " auto-pointer" else " no-pointer"
                
            let cardSide =
                match position with
                | ClusterPosition.Center -> CardSide.Front
                | ClusterPosition.Inner_Bottom
                | ClusterPosition.Inner_Left
                | ClusterPosition.Inner_Top
                | ClusterPosition.Inner_Right -> CardSide.Back
                | ClusterPosition.Outer_Bottom
                | ClusterPosition.Outer_Left
                | ClusterPosition.Outer_Top
                | ClusterPosition.Outer_Right -> CardSide.Front
                
            let canBeRotated =
                match position with
                | ClusterPosition.Center ->
                    not (hasCard ClusterPosition.Inner_Bottom || hasCard ClusterPosition.Inner_Left
                         || hasCard ClusterPosition.Inner_Top || hasCard ClusterPosition.Inner_Right)
                    
                | ClusterPosition.Inner_Bottom -> not (hasCard ClusterPosition.Outer_Bottom)
                | ClusterPosition.Inner_Left -> not (hasCard ClusterPosition.Outer_Left)
                | ClusterPosition.Inner_Top -> not (hasCard ClusterPosition.Outer_Top)
                | ClusterPosition.Inner_Right -> not (hasCard ClusterPosition.Outer_Right)
                
                | ClusterPosition.Outer_Bottom
                | ClusterPosition.Outer_Left
                | ClusterPosition.Outer_Top
                | ClusterPosition.Outer_Right -> true
            
            concat {
                if showDropzone position then
                    div {
                        attr.``class`` $"{dropzoneClassName}{blinkerClass}{pointerEventsClass}"
                        
                        comp<Dropzone<Card>> {
                            "MaxItems" => 1
                            "Items" => List<Card>()
                            "Accepts" => Func<Card, Card, bool>(acceptDrop)
                            "OnItemDrop" => EventCallbackFactory().Create(this, onDrop)
                        }
                    }
                        
                div {
                    attr.``class`` cardClassName
                    attr.style rotation
                    
                    if card <> Card.empty then
                        comp<LoreBuilder.Components.Card> {
                            "Data" => card
                            "Size" => 270
                            "CurrentSide" => cardSide
                            "CanBeFlipped" => false
                            "CanBeRotated" => canBeRotated
                        }
                    else
                        div { attr.style $"width: 270px; height: 270px;" }
                }
            }
        
        div {
            attr.``class`` "cluster-exterior"
            
            div {
                attr.``class`` "cluster-interior"
                
                Union.toList<ClusterPosition>()
                |> List.map cardAndDropzone
                |> LoreBuilder.Utils.renderList
            }
        }
