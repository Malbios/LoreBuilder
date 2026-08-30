namespace LoreBuilder.Components

open System
open System.Collections.Generic
open Bolero
open Bolero.Html
open FunSharp.Common
open LoreBuilder.Model
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Web
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
    // without fighting Card's own local state (Card notifies us via OnRotationChanged
    // whenever the user rotates it - CurrentSide is fixed per slot by initialUiState, never
    // toggled by Card itself, which has no flip mechanic).
    let cardUiStates =
        Union.toList<ClusterPosition>()
        |> List.map(fun position -> (position, initialUiState position))
        |> Dictionary.ofList

    override _.CssScope = CssScopes.LoreCluster
    
    [<Inject>]
    member val Logger : ILogger<LoreCluster> = Unchecked.defaultof<_> with get, set
    
    [<Parameter>]
    member val DropzonesAreActive = false with get, set

    // Forwarded to every card - see Card.IsDeleteMode's doc comment.
    [<Parameter>]
    member val IsDeleteMode = false with get, set

    [<Parameter>]
    member val Lore = "" with get, set

    // Seeds the primary slot at creation time only (read once in OnInitialized) - used by
    // Pages/Home.fs when a card is dropped directly onto empty canvas space and a brand new
    // cluster is created there already holding that card, rather than starting empty and
    // waiting for a drop into its primary dropzone.
    [<Parameter>]
    member val InitialPrimaryCard: Card option = None with get, set

    override this.OnInitialized() =
        match this.InitialPrimaryCard with
        | Some card -> cards[ClusterPosition.Primary] <- card
        | None -> ()
    
    [<Parameter>]
    member val OnCardReplace: Card -> unit = ignore with get, set

    // Fired when the primary card is removed and leaves the cluster with no cards at all - the
    // primary can only be removed once it has no inner cards depending on it (see canBeRotated
    // below), which by construction means every other slot is already empty too.
    [<Parameter>]
    member val OnClusterEmptied: unit -> unit = ignore with get, set

    // Fired on mousedown on the primary card specifically - the grab handle for repositioning
    // the whole cluster on a free-form canvas (e.g. Pages/Home.fs). Not wired to Inner/Outer
    // cards - only the primary card serves as the drag handle for the whole cluster.
    [<Parameter>]
    member val OnPrimaryMouseDown: MouseEventArgs -> unit = ignore with get, set

    // Fired (after render, whenever it actually changes) with this cluster's current total
    // visual footprint in pixels - cluster-interior's fixed 270px plus twice ComputeMargin().
    // Lets Pages/Home.fs's overlap check use each cluster's real current size (a bare primary
    // card vs. one fully decorated with inner/outer cards) instead of one flat worst-case value.
    [<Parameter>]
    member val OnFootprintChanged: float -> unit = ignore with get, set

    member val private PreviousFootprint: float option = None with get, set

    member private this.HasCard position =
        cards[position] <> Card.empty

    member private this.NoInnerCards =
        not (
            this.HasCard ClusterPosition.Inner_Bottom
            || this.HasCard ClusterPosition.Inner_Left
            || this.HasCard ClusterPosition.Inner_Top
            || this.HasCard ClusterPosition.Inner_Right
        )

    // Shared by Render() (for the cluster-interior CSS margin) and OnAfterRender (for
    // OnFootprintChanged) so the two can't drift apart.
    member private this.ComputeMargin() =
        let innerMargin = if this.HasCard ClusterPosition.Primary then 60 else 0
        let outerMargin = if this.NoInnerCards then 0 else 40

        innerMargin + outerMargin

    override this.OnAfterRender(_firstRender: bool) =
        let footprint = 270.0 + 2.0 * float (this.ComputeMargin())

        if this.PreviousFootprint <> Some footprint then
            this.PreviousFootprint <- Some footprint
            this.OnFootprintChanged footprint

    // StateHasChanged is protected and can't be called directly from within a lambda -
    // this member wrapper is the standard F#/Blazor workaround.
    member private this.NotifyStateChanged() =
        this.StateHasChanged()

    override this.Render() =

        let hasCard = this.HasCard

        let noInnerCards = this.NoInnerCards

        let innerPositionFor outerPosition =
            match outerPosition with
            | ClusterPosition.Outer_Bottom -> ClusterPosition.Inner_Bottom
            | ClusterPosition.Outer_Left -> ClusterPosition.Inner_Left
            | ClusterPosition.Outer_Top -> ClusterPosition.Inner_Top
            | ClusterPosition.Outer_Right -> ClusterPosition.Inner_Right
            | other -> failwith $"{other} is not an outer position"

        // What the inner card next to this outer slot expects to be attached there, based on
        // whichever of its cues is currently facing outward (its Secondary side's Top edge,
        // same edge math the rendering uses). None means that inner card expects nothing
        // further - the outer slot doesn't accept any card, and its dropzone stays hidden.
        let expectedOuterExpansion outerPosition =
            let innerPosition = innerPositionFor outerPosition
            let innerCard = cards[innerPosition]

            if innerCard = Card.empty then
                None
            else
                let rotation = cardUiStates[innerPosition].Rotation

                match CardHelpers.activeCue innerCard.SecondarySide CardEdge.Top rotation with
                | Some (Cue.Complex complexCue) -> complexCue.Expansions
                | _ -> None

        let showDropzone position =
            match position with
            | ClusterPosition.Primary -> noInnerCards

            | ClusterPosition.Inner_Bottom
            | ClusterPosition.Inner_Left
            | ClusterPosition.Inner_Top
            | ClusterPosition.Inner_Right -> hasCard ClusterPosition.Primary

            | ClusterPosition.Outer_Bottom
            | ClusterPosition.Outer_Left
            | ClusterPosition.Outer_Top
            | ClusterPosition.Outer_Right ->
                hasCard (innerPositionFor position) && (expectedOuterExpansion position).IsSome
            
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
                    
                | ClusterPosition.Outer_Bottom
                | ClusterPosition.Outer_Left
                | ClusterPosition.Outer_Top
                | ClusterPosition.Outer_Right ->
                    match expectedOuterExpansion position with
                    | None -> false
                    | Some expansion -> Logical.accepts expansion card.Type
            
            let replaceCard newCard =
                let oldCard = cards[position]
                cards[position] <- newCard
                cardUiStates[position] <- initialUiState position
                if oldCard <> Card.empty then this.OnCardReplace(oldCard)
                if position = ClusterPosition.Primary && oldCard <> Card.empty && newCard = Card.empty then
                    this.OnClusterEmptied()

            let onDrop card = replaceCard card

            let onRemove () = replaceCard Card.empty
            
            let blinkerClass =
                if this.DropzonesAreActive then " blink_me" else ""
            
            let pointerEventsClass =
                if this.DropzonesAreActive then " auto-pointer" else " no-pointer"
                
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

                // isDragHandle/cardStyle/onCardMouseDown are always applied unconditionally
                // below (rather than branching attr.style/on.mousedown per-position inside the
                // div) so this element's attribute/handler shape never changes between renders -
                // a shape change (e.g. when a card first lands in the primary slot) is exactly
                // the kind of thing that can make Blazor's render-tree diffing drop a handler.
                let isDragHandle = position = ClusterPosition.Primary && card <> Card.empty
                let cardStyle = if isDragHandle then $"{rotation}cursor: grab;" else rotation
                let onCardMouseDown (e: MouseEventArgs) =
                    if isDragHandle then this.OnPrimaryMouseDown e

                div {
                    attr.``class`` cardClassName
                    attr.style cardStyle
                    on.mousedown onCardMouseDown

                    if card <> Card.empty then
                        comp<LoreBuilder.Components.Card> {
                            "Data" => card
                            "Size" => 270
                            "CurrentSide" => cardUiStates[position].CurrentSide
                            "Rotation" => cardUiStates[position].Rotation
                            "CanBeRotated" => canBeRotated
                            // Removing a card follows the same "nothing depends on it" rule as
                            // rotating it - an outer card is always free to remove, but an inner
                            // or primary card can't be pulled out from under a card attached to it.
                            "CanBeRemoved" => canBeRotated
                            "IsDeleteMode" => this.IsDeleteMode
                            "ActiveEdge" => activeEdge
                            "OnRotationChanged" => onRotationChanged
                            "OnRemove" => onRemove
                        }
                    else
                        div { attr.style $"width: 270px; height: 270px;" }
                }
            }
            
        let margin = this.ComputeMargin()

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
