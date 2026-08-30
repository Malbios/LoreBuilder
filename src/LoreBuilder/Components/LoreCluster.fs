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

    // The side/rotation a slot's card shows by default, before any user interaction - Inner
    // slots face inward (Secondary), since they're genuinely sandwiched between the primary and
    // their own outer card. Outer and Outer2 both face outward (Primary) rather than alternating
    // - each represents its own independent new card (e.g. the two Figure cards a
    // Logical.All [figure; figure] cue calls for), not a "backside attachment" to the card before
    // it in the chain, so neither should show its back.
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
            | ClusterPosition.Outer2_Bottom
            | ClusterPosition.Outer2_Left
            | ClusterPosition.Outer2_Top
            | ClusterPosition.Outer2_Right -> CardSide.Primary
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

    // Each slot's current growth in px (0 when not growing), reported by its own Card via
    // OnGrowthChanged - lets the next slot in the same direction's chain be pushed out far
    // enough to clear a grown card instead of overlapping it (see growthOffsetFor in Render()).
    let growthByPosition =
        Union.toList<ClusterPosition>()
        |> List.map(fun position -> (position, 0.0))
        |> Dictionary.ofList

    override _.CssScope = CssScopes.LoreCluster
    
    [<Inject>]
    member val Logger : ILogger<LoreCluster> = Unchecked.defaultof<_> with get, set
    
    [<Parameter>]
    member val DropzonesAreActive = false with get, set

    // The card currently being dragged from the sidebar (if any) - used alongside
    // DropzonesAreActive so a dropzone only shows/behaves as active when it would actually accept
    // this specific card (reusing acceptDrop below), not just whenever a drag of any kind is
    // happening. None (e.g. the dev-only LoreClusterTest.fs page, which doesn't wire this up)
    // falls back to the old undiscriminating behavior - every structurally-open dropzone is shown
    // as active.
    [<Parameter>]
    member val DraggedCard: Card option = None with get, set

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

    // Extra rotation (degrees, in 90-degree steps) applied to the whole cluster as a rigid unit,
    // via the primary card's own rotate arrows once it has inner cards attached (see
    // onRotationChanged below) - separate from any individual card's own Rotation, which stays
    // exactly what it was.
    member val private ClusterRotation = 0 with get, set

    member private this.HasCard position =
        cards[position] <> Card.empty

    member private this.NoInnerCards =
        not (
            this.HasCard ClusterPosition.Inner_Bottom
            || this.HasCard ClusterPosition.Inner_Left
            || this.HasCard ClusterPosition.Inner_Top
            || this.HasCard ClusterPosition.Inner_Right
        )

    member private this.NoOuterCards =
        not (
            this.HasCard ClusterPosition.Outer_Bottom
            || this.HasCard ClusterPosition.Outer_Left
            || this.HasCard ClusterPosition.Outer_Top
            || this.HasCard ClusterPosition.Outer_Right
        )

    // Shared by Render() (for the cluster-interior CSS margin) and OnAfterRender (for
    // OnFootprintChanged) so the two can't drift apart.
    member private this.ComputeMargin() =
        let innerMargin = if this.HasCard ClusterPosition.Primary then 60 else 0
        let outerMargin = if this.NoInnerCards then 0 else 40
        let outer2Margin = if this.NoOuterCards then 0 else 40

        innerMargin + outerMargin + outer2Margin

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

        let innerPositionFor position =
            match position with
            | ClusterPosition.Outer_Bottom | ClusterPosition.Outer2_Bottom -> ClusterPosition.Inner_Bottom
            | ClusterPosition.Outer_Left | ClusterPosition.Outer2_Left -> ClusterPosition.Inner_Left
            | ClusterPosition.Outer_Top | ClusterPosition.Outer2_Top -> ClusterPosition.Inner_Top
            | ClusterPosition.Outer_Right | ClusterPosition.Outer2_Right -> ClusterPosition.Inner_Right
            | other -> failwith $"{other} is not an outer/outer2 position"

        // Outer2's tier-1 sibling in the same direction (e.g. Outer2_Bottom -> Outer_Bottom) -
        // it has to be filled first, same as an inner card has to exist before its outer slot
        // opens up.
        let outer1PositionFor position =
            match position with
            | ClusterPosition.Outer2_Bottom -> ClusterPosition.Outer_Bottom
            | ClusterPosition.Outer2_Left -> ClusterPosition.Outer_Left
            | ClusterPosition.Outer2_Top -> ClusterPosition.Outer_Top
            | ClusterPosition.Outer2_Right -> ClusterPosition.Outer_Right
            | other -> failwith $"{other} is not an outer2 position"

        let slotIndexFor position =
            match position with
            | ClusterPosition.Outer2_Bottom
            | ClusterPosition.Outer2_Left
            | ClusterPosition.Outer2_Top
            | ClusterPosition.Outer2_Right -> 1
            | _ -> 0

        // How many extra px this position's own slot needs to be pushed further out, to clear
        // every closer-to-center card in the same direction's chain that has grown. Relies on the
        // self-consistency established for Card growth (a position's card only ever grows in that
        // position's own outward direction) - Primary's own growth is always in the Bottom
        // direction specifically (see its activeEdge = Some CardEdge.Bottom assignment below), so
        // it only compensates the Bottom chain, never Left/Top/Right.
        let growthOffsetFor position =
            match position with
            | ClusterPosition.Inner_Bottom -> growthByPosition[ClusterPosition.Primary]
            | ClusterPosition.Inner_Left
            | ClusterPosition.Inner_Top
            | ClusterPosition.Inner_Right -> 0.0

            | ClusterPosition.Outer_Bottom ->
                growthByPosition[ClusterPosition.Primary] + growthByPosition[ClusterPosition.Inner_Bottom]
            | ClusterPosition.Outer_Left -> growthByPosition[ClusterPosition.Inner_Left]
            | ClusterPosition.Outer_Top -> growthByPosition[ClusterPosition.Inner_Top]
            | ClusterPosition.Outer_Right -> growthByPosition[ClusterPosition.Inner_Right]

            | ClusterPosition.Outer2_Bottom ->
                growthByPosition[ClusterPosition.Primary]
                + growthByPosition[ClusterPosition.Inner_Bottom]
                + growthByPosition[ClusterPosition.Outer_Bottom]
            | ClusterPosition.Outer2_Left ->
                growthByPosition[ClusterPosition.Inner_Left] + growthByPosition[ClusterPosition.Outer_Left]
            | ClusterPosition.Outer2_Top ->
                growthByPosition[ClusterPosition.Inner_Top] + growthByPosition[ClusterPosition.Outer_Top]
            | ClusterPosition.Outer2_Right ->
                growthByPosition[ClusterPosition.Inner_Right] + growthByPosition[ClusterPosition.Outer_Right]

            | ClusterPosition.Primary -> 0.0

        // The base (un-grown) offset baked into Card.bolero.css's .inner_*/.outer_*/.outer2_*
        // rules - must stay in sync with those (58/90/122).
        let baseOffsetFor position =
            match position with
            | ClusterPosition.Inner_Bottom
            | ClusterPosition.Inner_Left
            | ClusterPosition.Inner_Top
            | ClusterPosition.Inner_Right -> 58.0
            | ClusterPosition.Outer_Bottom
            | ClusterPosition.Outer_Left
            | ClusterPosition.Outer_Top
            | ClusterPosition.Outer_Right -> 90.0
            | ClusterPosition.Outer2_Bottom
            | ClusterPosition.Outer2_Left
            | ClusterPosition.Outer2_Top
            | ClusterPosition.Outer2_Right -> 122.0
            | ClusterPosition.Primary -> 0.0

        // Which CSS offset property this position's own CSS class sets (bottom/left/top/right) -
        // None for Primary, which doesn't have a chain offset at all.
        let offsetPropertyFor position =
            match position with
            | ClusterPosition.Inner_Bottom | ClusterPosition.Outer_Bottom | ClusterPosition.Outer2_Bottom -> Some "bottom"
            | ClusterPosition.Inner_Left | ClusterPosition.Outer_Left | ClusterPosition.Outer2_Left -> Some "left"
            | ClusterPosition.Inner_Top | ClusterPosition.Outer_Top | ClusterPosition.Outer2_Top -> Some "top"
            | ClusterPosition.Inner_Right | ClusterPosition.Outer_Right | ClusterPosition.Outer2_Right -> Some "right"
            | ClusterPosition.Primary -> None

        // The inner card's raw Expansions requirement, based on whichever of its cues is
        // currently facing outward (its Secondary side's Top edge, same edge math the rendering
        // uses). None means that inner card expects nothing further - neither an outer nor
        // outer2 slot accepts any card, and their dropzones stay hidden.
        let expansionsForInner innerPosition =
            let innerCard = cards[innerPosition]

            if innerCard = Card.empty then
                None
            else
                let rotation = cardUiStates[innerPosition].Rotation

                match CardHelpers.activeCue innerCard.SecondarySide CardEdge.Top rotation with
                | Some (Cue.Complex complexCue) -> complexCue.Expansions
                | _ -> None

        // The card type(s) this specific outer/outer2 slot accepts, or None if it isn't an
        // attachment point at all right now (see Logical.slotTypes - Any/One only ever have one
        // slot, All has one position-locked slot per list item).
        let slotTypesFor position =
            expansionsForInner (innerPositionFor position)
            |> Option.bind (Logical.slotTypes (slotIndexFor position))

        // Primary intentionally stays droppable/replaceable even once filled (dropping a new
        // primary card swaps it out) - Inner/Outer/Outer2 slots don't support replace-by-drop, so
        // once filled their dropzone must stop showing/accepting drops entirely (the card has to
        // be removed, e.g. via delete mode, before a new one can go there).
        let showDropzone position =
            match position with
            | ClusterPosition.Primary -> noInnerCards

            | ClusterPosition.Inner_Bottom
            | ClusterPosition.Inner_Left
            | ClusterPosition.Inner_Top
            | ClusterPosition.Inner_Right -> hasCard ClusterPosition.Primary && not (hasCard position)

            | ClusterPosition.Outer_Bottom
            | ClusterPosition.Outer_Left
            | ClusterPosition.Outer_Top
            | ClusterPosition.Outer_Right ->
                hasCard (innerPositionFor position) && (slotTypesFor position).IsSome && not (hasCard position)

            | ClusterPosition.Outer2_Bottom
            | ClusterPosition.Outer2_Left
            | ClusterPosition.Outer2_Top
            | ClusterPosition.Outer2_Right ->
                hasCard (outer1PositionFor position) && (slotTypesFor position).IsSome && not (hasCard position)
            
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
                    not (hasCard position) && card.Type = cards[ClusterPosition.Primary].Type

                | ClusterPosition.Outer_Bottom
                | ClusterPosition.Outer_Left
                | ClusterPosition.Outer_Top
                | ClusterPosition.Outer_Right ->
                    not (hasCard position) && (slotTypesFor position |> Option.exists (List.contains card.Type))

                | ClusterPosition.Outer2_Bottom
                | ClusterPosition.Outer2_Left
                | ClusterPosition.Outer2_Top
                | ClusterPosition.Outer2_Right ->
                    not (hasCard position)
                    && hasCard (outer1PositionFor position)
                    && (slotTypesFor position |> Option.exists (List.contains card.Type))
            
            let replaceCard newCard =
                let oldCard = cards[position]
                cards[position] <- newCard
                cardUiStates[position] <- initialUiState position
                if oldCard <> Card.empty then this.OnCardReplace(oldCard)
                if position = ClusterPosition.Primary && oldCard <> Card.empty && newCard = Card.empty then
                    this.OnClusterEmptied()
                // Once the cluster is back to a lone primary (or fully empty), there's no more
                // "whole cluster" left to have a group rotation - reset it rather than leave a
                // stale spin applied to a now-single, independently-rotatable-again primary card.
                if this.NoInnerCards then
                    this.ClusterRotation <- 0

            let onDrop card = replaceCard card

            let onRemove () = replaceCard Card.empty
            
            // Whether this position's dropzone would actually accept the card currently being
            // dragged (reusing acceptDrop, rather than duplicating its type-matching logic) - the
            // second argument mirrors "Accepts" below, which never uses it either. Falls back to
            // true when DraggedCard isn't wired up (see DraggedCard's own doc comment).
            let dropzoneAccepts =
                this.DraggedCard
                |> Option.map (fun draggedCard -> acceptDrop draggedCard card)
                |> Option.defaultValue true

            let blinkerClass =
                if this.DropzonesAreActive && dropzoneAccepts then " blink_me" else ""

            let pointerEventsClass =
                if this.DropzonesAreActive && dropzoneAccepts then " auto-pointer" else " no-pointer"
                
            let onRotationChanged newRotation =
                if position = ClusterPosition.Primary && not noInnerCards then
                    // Primary's own Rotation is frozen while it has inner cards (see canBeRotated
                    // below) - its arrows spin the whole cluster instead. Card's own Rotation
                    // parameter always resets to cardUiStates[Primary].Rotation (frozen, unchanged
                    // here) at the start of each click, then increments by exactly +-90 before
                    // reporting back - so newRotation minus that frozen base is always exactly
                    // this click's own delta.
                    let delta = newRotation - cardUiStates[position].Rotation
                    this.ClusterRotation <- this.ClusterRotation + delta
                    this.NotifyStateChanged()
                else
                    cardUiStates[position] <- { cardUiStates[position] with Rotation = newRotation }
                    this.NotifyStateChanged()

            let onGrowthChanged growth =
                if growthByPosition[position] <> growth then
                    growthByPosition[position] <- growth
                    this.NotifyStateChanged()

            // Overrides the position's static CSS offset (bottom/left/top/right, matching
            // Card.bolero.css's .inner_*/.outer_*/.outer2_* rules) whenever something closer to
            // center in the same direction's chain has grown - see growthOffsetFor. Redundant
            // (same value as the CSS class) when nothing has grown, so it's fine to always
            // include it rather than conditionally omit it.
            let offsetStyle =
                match offsetPropertyFor position with
                | None -> ""
                | Some prop -> $"{prop}: -{baseOffsetFor position + growthOffsetFor position}px;"

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
                | ClusterPosition.Outer_Right
                | ClusterPosition.Outer2_Bottom
                | ClusterPosition.Outer2_Left
                | ClusterPosition.Outer2_Top
                | ClusterPosition.Outer2_Right -> Some CardEdge.Top

            // canBeRemoved keeps the original "nothing depends on it" removal rule for Primary
            // (noInnerCards) - unrelated to canBeRotated below, since Primary's rotate arrows now
            // always do *something* (rotate itself alone, or the whole cluster once it has inner
            // cards - see onRotationChanged), but removing it while things are attached still
            // doesn't make sense.
            let canBeRemoved =
                match position with
                | ClusterPosition.Primary -> noInnerCards

                | ClusterPosition.Inner_Bottom -> not (hasCard ClusterPosition.Outer_Bottom)
                | ClusterPosition.Inner_Left -> not (hasCard ClusterPosition.Outer_Left)
                | ClusterPosition.Inner_Top -> not (hasCard ClusterPosition.Outer_Top)
                | ClusterPosition.Inner_Right -> not (hasCard ClusterPosition.Outer_Right)

                // Unlike Inner->Outer (a genuine physical dependency - outer is tugged directly
                // off inner's own back edge), Outer and Outer2 are independent siblings, each its
                // own new card (see initialUiState's doc comment) - so Outer2 being attached
                // doesn't block Outer from rotating or being removed, and vice versa.
                | ClusterPosition.Outer_Bottom
                | ClusterPosition.Outer_Left
                | ClusterPosition.Outer_Top
                | ClusterPosition.Outer_Right
                | ClusterPosition.Outer2_Bottom
                | ClusterPosition.Outer2_Left
                | ClusterPosition.Outer2_Top
                | ClusterPosition.Outer2_Right -> true

            let canBeRotated =
                match position with
                | ClusterPosition.Primary -> true
                | _ -> canBeRemoved

            concat {
                let dropzoneVisibility =
                    if showDropzone position then "" else "display: none;"

                div {
                    attr.``class`` $"{dropzoneClassName}{blinkerClass}{pointerEventsClass}"
                    attr.style $"{dropzoneVisibility}{offsetStyle}"

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
                let dragHandleStyle = if isDragHandle then "cursor: grab;" else ""
                let cardStyle = $"{rotation}{offsetStyle}{dragHandleStyle}"
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
                            // Cancels .cluster-interior's own rotation just for the primary's
                            // arrows once they're spinning the whole cluster, so they stay put on
                            // screen across repeated clicks instead of ending up wherever the
                            // last rotation carried them (see Card.fs's CounterRotation doc).
                            "CounterRotation" =>
                                (if position = ClusterPosition.Primary && not noInnerCards then
                                     -this.ClusterRotation
                                 else
                                     0)
                            "CanBeRotated" => canBeRotated
                            // Removal keeps the "nothing depends on it" rule - an outer card is
                            // always free to remove, but an inner or primary card can't be pulled
                            // out from under a card attached to it (see canBeRemoved above,
                            // separate from canBeRotated since Primary's rotate arrows now always
                            // do something even when it can't be removed).
                            "CanBeRemoved" => canBeRemoved
                            "IsDeleteMode" => this.IsDeleteMode
                            "ActiveEdge" => activeEdge
                            "OnRotationChanged" => onRotationChanged
                            "OnGrowthChanged" => onGrowthChanged
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
                attr.style $"margin: {margin}px; transform: rotate({this.ClusterRotation}deg);"
                
                Union.toList<ClusterPosition>()
                |> List.map cardAndDropzone
                |> LoreBuilder.Utils.renderList
            }
        }
