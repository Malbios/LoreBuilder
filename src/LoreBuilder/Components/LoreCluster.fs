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

type LoreCluster() =
    inherit Component()

    // The side/rotation a slot's card shows by default, before any user interaction -
    // Inner slots face inward (Secondary), everything else faces outward (Primary).
    let initialUiState position =
        let side =
            match position with
            | ClusterPosition.Primary -> CardSide.Primary
            | ClusterPosition.Inner_Bottom
            | ClusterPosition.Inner_Left
            | ClusterPosition.Inner_Top
            | ClusterPosition.Inner_Right -> CardSide.Secondary
            | ClusterPosition.Outer_Bottom
            | ClusterPosition.Outer_Left
            | ClusterPosition.Outer_Top
            | ClusterPosition.Outer_Right -> CardSide.Primary
        { CardUiState.initial with CurrentSide = side }

    let cards =
        Union.toList<ClusterPosition>()
        |> List.map(fun position -> (position, Card.empty))
        |> Dictionary.ofList

    // Owns each slot's current side/rotation so it can be passed down on every render
    // without fighting Card's own local state (Card notifies us via OnCurrentSideChanged/
    // OnRotationChanged whenever the user flips/rotates it).
    let cardUiStates =
        Union.toList<ClusterPosition>()
        |> List.map(fun position -> (position, initialUiState position))
        |> Dictionary.ofList

    override _.CssScope = CssScopes.LoreCluster
    
    [<Inject>]
    member val Logger : ILogger<LoreCluster> = Unchecked.defaultof<_> with get, set
    
    [<Parameter>]
    member val DropzonesAreActive = false with get, set
    
    [<Parameter>]
    member val Lore = "" with get, set
    
    [<Parameter>]
    member val OnCardReplace: Card -> unit = ignore with get, set

    // StateHasChanged is protected and can't be called directly from within a lambda -
    // this member wrapper is the standard F#/Blazor workaround.
    member private this.NotifyStateChanged() =
        this.StateHasChanged()

    override this.Render() =
        
        let hasCard position =
            cards[position] <> Card.empty
            
        let noInnerCards =
            not (hasCard ClusterPosition.Inner_Bottom || hasCard ClusterPosition.Inner_Left || hasCard ClusterPosition.Inner_Top || hasCard ClusterPosition.Inner_Right)
        
        let showDropzone position =
            match position with
            | ClusterPosition.Primary -> noInnerCards
            
            | ClusterPosition.Inner_Bottom
            | ClusterPosition.Inner_Left
            | ClusterPosition.Inner_Top
            | ClusterPosition.Inner_Right -> hasCard ClusterPosition.Primary
            
            | ClusterPosition.Outer_Bottom -> hasCard ClusterPosition.Inner_Bottom
            | ClusterPosition.Outer_Left -> hasCard ClusterPosition.Inner_Left
            | ClusterPosition.Outer_Top -> hasCard ClusterPosition.Inner_Top
            | ClusterPosition.Outer_Right -> hasCard ClusterPosition.Inner_Right
            
        let cardAndDropzone position =
            let card = cards[position]
            let cardClassName = ClusterPosition.toString position
            let dropzoneClassName = $"{cardClassName}-dropzone"
            let rotation = ClusterPosition.toRotation position
            
            let acceptDrop (card: Card) _ = // droppedCard, target (target could be null)
                match position with
                | ClusterPosition.Primary -> true
                
                | ClusterPosition.Inner_Bottom
                | ClusterPosition.Inner_Left
                | ClusterPosition.Inner_Top
                | ClusterPosition.Inner_Right ->
                    card.Type = cards[ClusterPosition.Primary].Type
                    
                | ClusterPosition.Outer_Bottom -> true // TODO: based on inner
                | ClusterPosition.Outer_Left -> true // TODO: based on inner
                | ClusterPosition.Outer_Top -> true // TODO: based on inner
                | ClusterPosition.Outer_Right -> true // TODO: based on inner
            
            let onDrop card =
                let oldCard = cards[position]
                cards[position] <- card
                cardUiStates[position] <- initialUiState position
                if oldCard <> Card.empty then this.OnCardReplace(oldCard)
            
            let blinkerClass =
                if this.DropzonesAreActive then " blink_me" else ""
            
            let pointerEventsClass =
                if this.DropzonesAreActive then " auto-pointer" else " no-pointer"
                
            let onCurrentSideChanged newSide =
                cardUiStates[position] <- { cardUiStates[position] with CurrentSide = newSide }
                this.NotifyStateChanged()

            let onRotationChanged newRotation =
                cardUiStates[position] <- { cardUiStates[position] with Rotation = newRotation }
                this.NotifyStateChanged()

            let activeEdge =
                match position with
                | ClusterPosition.Primary -> if noInnerCards then None else Some CardEdge.Bottom
                
                | ClusterPosition.Inner_Bottom
                | ClusterPosition.Inner_Left
                | ClusterPosition.Inner_Top
                | ClusterPosition.Inner_Right
                | ClusterPosition.Outer_Bottom
                | ClusterPosition.Outer_Left
                | ClusterPosition.Outer_Top
                | ClusterPosition.Outer_Right -> Some CardEdge.Top
                
            let canBeRotated =
                match position with
                | ClusterPosition.Primary -> noInnerCards
                    
                | ClusterPosition.Inner_Bottom -> not (hasCard ClusterPosition.Outer_Bottom)
                | ClusterPosition.Inner_Left -> not (hasCard ClusterPosition.Outer_Left)
                | ClusterPosition.Inner_Top -> not (hasCard ClusterPosition.Outer_Top)
                | ClusterPosition.Inner_Right -> not (hasCard ClusterPosition.Outer_Right)
                
                | ClusterPosition.Outer_Bottom
                | ClusterPosition.Outer_Left
                | ClusterPosition.Outer_Top
                | ClusterPosition.Outer_Right -> true
                
            let canBeFlipped =
                match position with
                | ClusterPosition.Primary -> noInnerCards
                | _ -> false
            
            concat {
                let dropzoneVisibility =
                    if showDropzone position then "" else "display: none;"
                    
                div {
                    attr.``class`` $"{dropzoneClassName}{blinkerClass}{pointerEventsClass}"
                    attr.style $"{dropzoneVisibility}"
                    
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
                            "CurrentSide" => cardUiStates[position].CurrentSide
                            "Rotation" => cardUiStates[position].Rotation
                            "CanBeFlipped" => canBeFlipped
                            "CanBeRotated" => canBeRotated
                            "ActiveEdge" => activeEdge
                            "OnCurrentSideChanged" => onCurrentSideChanged
                            "OnRotationChanged" => onRotationChanged
                        }
                    else
                        div { attr.style $"width: 270px; height: 270px;" }
                }
            }
            
        let margin =
            let innerMargin = if hasCard ClusterPosition.Primary then 60 else 0
            let outerMargin = if noInnerCards then 0 else 40
            
            innerMargin + outerMargin
        
        div {
            attr.``class`` "cluster-exterior"
            
            div {
                attr.``class`` "cluster-interior"
                attr.style $"margin: {margin}px"
                
                Union.toList<ClusterPosition>()
                |> List.map cardAndDropzone
                |> LoreBuilder.Utils.renderList
            }
        }
