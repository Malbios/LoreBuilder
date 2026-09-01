namespace LoreBuilder.Components

open Bolero
open Bolero.Html
open FunSharp.Common
open LoreBuilder
open LoreBuilder.Model
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Web
open Microsoft.JSInterop

[<RequireQualifiedAccess>]
module CardHelpers =
        
    let cueExpansions (separator: string) (cardTypes: CardType list) =
        
        div {
            attr.``class`` "cue-expansions"
            
            cardTypes
            |> List.map (fun t ->
                let iconColor = CardType.iconColor t
                let icon = CardType.icon t
                
                div {
                    attr.style $"color: {iconColor};"
                    
                    i { attr.``class`` $"fa-solid {icon}" }
                }
            )
            |> List.join (text separator)
            |> Utils.renderList
        }
        
    let complexCue cue =
        
        div {
            attr.``class`` "cue-header-and-text-and-expansions"
            
            if cue.Header.IsSome then
                div {
                    attr.``class`` "cue-header"
                    text cue.Header.Value
                }
            
            div {
                attr.``class`` "cue-text-and-expansions"
                
                div {
                    attr.``class`` "cue-text"
                    cue.Text
                }
                
                if cue.Expansions.IsSome then
                    match cue.Expansions.Value with
                    | Logical.One v -> cueExpansions "" [v]
                    | Logical.Any v -> cueExpansions "/" v
                    | Logical.All v -> cueExpansions "+" v
            }
        }
        
    let cardCue cardType cue =
        
        match cue with
        | None -> Node.Empty ()
        | Some cue ->
            match cue with
            | Cue.Simple s -> text s
            | Cue.Icon fileName -> img { attr.src (Cue.iconUri cardType fileName) }
            | Cue.Complex cue -> complexCue cue
            
    let edgeFromRotation rotation =
        
        match rotation % 360 with
        | 0 -> CardEdge.Bottom
        
        | 270
        | -90 -> CardEdge.Left
        
        | 180
        | -180 -> CardEdge.Top
        
        | 90
        | -270 -> CardEdge.Right
        
        | _ -> failwith $"unexpected rotation: {rotation}"
            
    let isVisible (activeEdge: CardEdge option) rotation edge =
        
        match activeEdge with
        | None -> true
        | Some v ->
            let rotatedEdge = edgeFromRotation rotation
            
            match v with
            | CardEdge.Bottom ->
                edge = rotatedEdge
            | CardEdge.Top ->
                edge = (CardEdge.opposite rotatedEdge)
            | _ -> failwith $"unexpected active edge value: {v}"

    // The single physical edge (Bottom/Left/Top/Right) currently facing the given activeEdge,
    // accounting for rotation. Factored out of activeCue so callers that only need the edge
    // identity (not the cue itself, e.g. growthEdge below) don't have to re-derive it.
    let activePhysicalEdge (activeEdge: CardEdge option) rotation =

        match activeEdge with
        | None -> None
        | Some edge ->
            let rotatedEdge = edgeFromRotation rotation

            match edge with
            | CardEdge.Bottom -> Some rotatedEdge
            | CardEdge.Top -> Some (CardEdge.opposite rotatedEdge)
            | _ -> failwith $"unexpected active edge value: {edge}"

    // The single Cue currently facing the given activeEdge, accounting for rotation.
    // Shares the edge/rotation math with isVisible so the two can't drift apart.
    let activeCue (cues: Cues) (activeEdge: CardEdge) rotation =

        let visibleEdge = activePhysicalEdge (Some activeEdge) rotation |> Option.get

        match visibleEdge with
        | CardEdge.Bottom -> cues.Bottom
        | CardEdge.Left -> cues.Left
        | CardEdge.Top -> cues.Top
        | CardEdge.Right -> cues.Right

    // The physical edge that needs extra room to fit its Complex cue's content (header + text +
    // expansions) without it overflowing the card's own background/border - see Card.fs's
    // Render()/OnAfterRenderAsync and Card.bolero.css's .complex-cue rule. None for Simple/Icon
    // cues (already sized correctly) and for the no-cluster-context case (activeEdge = None, e.g.
    // the sidebar CardStack preview).
    let growthEdge (cues: Cues) (activeEdge: CardEdge option) rotation =

        match activeEdge with
        | None -> None
        | Some edge ->
            match activeCue cues edge rotation with
            | Some (Cue.Complex _) -> activePhysicalEdge (Some edge) rotation
            | _ -> None

    type CardData = {
        Class: string
        Style: string
        ActiveEdge: CardEdge option
        Rotation: int
        Type: CardType
        Visuals: CardVisuals
        Cues: Cues
        GrowthEdge: CardEdge option
        // Some for the CardData matching Card's CurrentSide (never the other side - only one side
        // is ever CurrentSide, and it never changes at runtime), None otherwise. When Some, EVERY
        // edge div gets its own ref bound, unconditionally, every render - not just the active
        // one - because a ref's presence/absence toggling between renders at the same tree
        // position crashes Blazor's diffing ("Unexpected frame type during RemoveOldFrame:
        // ElementReferenceCapture"), which is exactly what conditioning it on GrowthEdge (which
        // changes when the card is rotated) did. Card.fs's OnAfterRenderAsync only ever reads the
        // one ref matching the current GrowthEdge; the other 3 stay bound but unused.
        EdgeRefs: (CardEdge -> HtmlRef) option
        // The active Left/Right cue's own left/top (or right/top) position, computed in Card.fs
        // from its last measured height - None for Bottom/Top (never needed) or before the first
        // measurement lands (falls back to Card.bolero.css's static default).
        LeftRightOffsetPx: float option
    }

    let cardCues data =

        div {
            attr.``class`` data.Class
            attr.style data.Style

            let artAnchorClass =
                match data.GrowthEdge with
                | None -> ""
                | Some CardEdge.Bottom -> " card-art-anchor-top"
                | Some CardEdge.Top -> " card-art-anchor-bottom"
                | Some CardEdge.Left -> " card-art-anchor-right"
                | Some CardEdge.Right -> " card-art-anchor-left"

            div {
                attr.``class`` $"card-art{artAnchorClass}"

                comp<CardBadge> {
                    "Visuals" => data.Visuals
                }
            }

            [
            (CardEdge.Bottom, data.Cues.Bottom)
            (CardEdge.Left, data.Cues.Left)
            (CardEdge.Top, data.Cues.Top)
            (CardEdge.Right, data.Cues.Right)
            ]
            |> List.map (fun (edge, cue) ->
                let cueKind =
                    cue |> Option.defaultValue (Cue.Simple "") |> (fun x -> (Union.toString x).ToLower())

                let visibility =
                    if isVisible data.ActiveEdge data.Rotation edge then "visible" else "hidden"

                let edgeName =
                    (Union.toString edge).ToLower()

                let isActiveEdge = data.GrowthEdge = Some edge

                // Overrides Card.bolero.css's static left/right-edge fallback position with the
                // one computed from this cue's own last measured height - see CardData.LeftRightOffsetPx.
                let positionOverrideStyle =
                    match isActiveEdge, data.LeftRightOffsetPx, edge with
                    | true, Some offset, CardEdge.Left -> $"left: {offset}px; top: {-offset}px; "
                    | true, Some offset, CardEdge.Right -> $"right: {offset}px; top: {-offset}px; "
                    | _ -> ""

                let cueClass = $"cue {cueKind}-cue {cueKind}-{edgeName}-edge"
                let cueStyle = $"{positionOverrideStyle}visibility: {visibility};"

                // All modifiers work both sides, per an edge/direction (see LoreCluster.fs's
                // extraction feature) - flanking triangles are a permanent visual signature of
                // that, not tied to any one card instance's history. Sits inside the same
                // already-flex .simple-cue/.icon-cue/.complex-cue container as the cue content
                // itself, so it lays out as part of that row rather than needing its own
                // positioning, and inherits the surrounding text color for free.
                //
                // Both point "up" in this container's own unrotated frame, not left/right - for
                // an Inner/Outer/Outer2 card this container already carries a rotation chain
                // (this position's own wrapper rotation, this card's own Rotation, and this
                // edge's own CSS rotation) that exists specifically to make the active cue read
                // facing the primary - and that chain always composes to the same constant per
                // direction (the card's own Rotation and the edge's own CSS rotation cancel by
                // construction, regardless of which edge ends up active), so "up" pre-rotation
                // reliably ends up pointing at the primary post-rotation, for any of the 4
                // directions, with no extra per-direction logic needed here.
                let cueContent =
                    if data.Type = CardType.Modifier then
                        concat {
                            i { attr.``class`` "fa-solid fa-caret-up modifier-edge-triangle" }
                            cardCue data.Type cue
                            i { attr.``class`` "fa-solid fa-caret-up modifier-edge-triangle" }
                        }
                    else
                        cardCue data.Type cue

                // Whether a ref gets bound here depends only on data.EdgeRefs (i.e. only on which
                // side this is - stable for the component's whole lifetime), never on isActiveEdge
                // (which changes when rotated) - see CardData.EdgeRefs for why that distinction
                // matters. Ref-binding can only appear as a bare top-level statement in a div CE,
                // not conditionally alongside Node children in the same block, hence the whole div
                // being duplicated here rather than just the one ref statement.
                match data.EdgeRefs with
                | Some getRef ->
                    div {
                        attr.key edgeName
                        attr.``class`` cueClass
                        attr.style cueStyle

                        getRef edge

                        cueContent
                    }
                | None ->
                    div {
                        attr.key edgeName
                        attr.``class`` cueClass
                        attr.style cueStyle

                        cueContent
                    }
            )
            |> Utils.renderList
        }

    let arrow className arrowStyle controlColor rotate icon =
            
        div {
            attr.``class`` $"arrow {className}"
            attr.style $"{arrowStyle}color: {controlColor};"
            
            on.click rotate
            on.stopPropagation "click" true

            // Without this, a mousedown starting here would bubble up to LoreCluster's
            // whole-cluster drag handler before this element's own click ever fires.
            on.stopPropagation "mousedown" true

            i { attr.``class`` $"fa-solid {icon}" }
        }

type Card() =
    inherit Component()

    // One stable ref per physical edge, each always bound to that edge's cue div on
    // CurrentSide (see CardHelpers.CardData.EdgeRefs) regardless of which one is currently
    // active - OnAfterRenderAsync reads only the one matching the current GrowthEdge. Mirrors
    // Pages/Home.fs's canvasRef.
    let bottomCueRef = HtmlRef()
    let leftCueRef = HtmlRef()
    let topCueRef = HtmlRef()
    let rightCueRef = HtmlRef()

    let cueRefFor edge =
        match edge with
        | CardEdge.Bottom -> bottomCueRef
        | CardEdge.Left -> leftCueRef
        | CardEdge.Top -> topCueRef
        | CardEdge.Right -> rightCueRef

    override _.CssScope = CssScopes.Card

    [<Inject>]
    member val JSRuntime: IJSRuntime = Unchecked.defaultof<_> with get, set

    // The active Complex cue's last real measured height in px, or None before the first
    // measurement lands - drives both Render()'s growth amount and, for Left/Right edges, the
    // cue's own position (see CardHelpers.CardData.LeftRightOffsetPx). Set only from
    // OnAfterRenderAsync, guarded so a re-render is triggered only when the value actually changes.
    member val private MeasuredCueHeight: float option = None with get, set

    // The last growth amount reported via OnGrowthChanged - guards that callback the same way
    // MeasuredCueHeight guards its own re-render, so it only fires on an actual change.
    member val private PreviousGrowthPx: float option = None with get, set

    // Captures CurrentSide's value at mount and never changes afterward, even though CurrentSide
    // itself now can (Modifier cards toggle it - see onCardClick below). EdgeRefs' Some/None
    // split (below) has to stay keyed off something that's stable for the component's whole
    // lifetime: a ref's presence/absence toggling between renders at the *same* tree position
    // crashes Blazor's diffing ("Unexpected frame type during RemoveOldFrame:
    // ElementReferenceCapture") - exactly what conditioning it directly on CurrentSide now would
    // do on every flip. Which side actually ends up holding the refs stops mattering once it's
    // stable: growth measurement is a no-op for Modifier cards anyway (they're always Simple
    // cues - see CardHelpers.growthEdge), which is the only thing that would ever ask for them.
    member val private StableEdgeRefsSide = CardSide.Primary with get, set

    override this.OnInitialized() =
        this.StableEdgeRefsSide <- this.CurrentSide

    [<Parameter>]
    member val Data = Card.empty with get, set

    // Fixed per slot by LoreCluster's initialUiState for every card type except Modifier, which
    // the user can toggle by clicking the card body (see onCardClick below) - "all modifiers work
    // both sides". Animates via Card.bolero.css's .card transition (re-added after an earlier
    // attempt was pulled for conflicting with the growth feature - see that rule's own comment
    // for why the two can't actually collide any more).
    [<Parameter>]
    member val CurrentSide = CardSide.Primary with get, set

    [<Parameter>]
    member val IsHovered = false with get, set

    [<Parameter>]
    member val IsFlipping = false with get, set

    [<Parameter>]
    member val CanBeRotated = true with get, set

    [<Parameter>]
    member val CanBeRemoved = false with get, set

    // While true, clicking the card body removes it (if CanBeRemoved) instead of flipping it -
    // set from Pages/Home.fs's delete-mode toggle, since a per-card remove button has nowhere
    // reliable to sit (two of a tucked card's four corners are covered by its neighbor in the
    // cluster ring).
    [<Parameter>]
    member val IsDeleteMode = false with get, set

    // Double-clicking an eligible (CanBeExtracted) card copies it into a brand-new, independent
    // cluster elsewhere on the canvas - see onCardDoubleClick below. No highlighting on eligible
    // cards (unlike delete's overlay) - double-click always extracts an eligible card regardless
    // of any visual state, so a highlight wouldn't tell the user anything they don't already know.
    [<Parameter>]
    member val CanBeExtracted = false with get, set

    [<Parameter>]
    member val OnExtract: unit -> unit = ignore with get, set

    // True for a card that's the source of a live extraction (see LoreCluster.fs's canDiveIn) -
    // makes clicking it navigate into that sub-canvas instead of doing anything else, unless
    // delete mode is active (see onCardClick below).
    [<Parameter>]
    member val CanDiveIn = false with get, set

    [<Parameter>]
    member val OnDiveIn: unit -> unit = ignore with get, set

    [<Parameter>]
    member val OnCurrentSideChanged: CardSide -> unit = ignore with get, set

    [<Parameter>]
    member val ActiveEdge = None with get, set
    
    [<Parameter>]
    member val Size = 0 with get, set
    
    [<Parameter>]
    member val Rotation = 0 with get, set

    // Non-zero only for LoreCluster's whole-cluster-rotate mode (the primary card once it has
    // inner cards) - applied to the arrows' own wrapper (not the card face) to cancel out
    // .cluster-interior's rotation just for them, so they stay put at the same screen position
    // across repeated clicks instead of ending up wherever the last rotation carried them.
    [<Parameter>]
    member val CounterRotation = 0 with get, set

    [<Parameter>]
    member val OnRotationChanged: int -> unit = ignore with get, set

    [<Parameter>]
    member val OnRemove: unit -> unit = ignore with get, set

    // Fired (after render, whenever it actually changes) with how far this card's own box has
    // grown past its base size in px (0 when not growing) - lets LoreCluster.fs push the next
    // card in the same direction's chain out far enough to clear it, since a grown card's real
    // visual size otherwise isn't reflected anywhere outside this component.
    [<Parameter>]
    member val OnGrowthChanged: float -> unit = ignore with get, set

    // Counter-rotates by -Rotation before the actual flip and back by +Rotation after it, rather
    // than a plain rotateY(180deg) - .flippable-card-container's own ancestor rotation is a real
    // screen-space 2D rotation, and since transforms compose within their ancestor's already-
    // transformed frame, an un-compensated rotateY's apparent hinge axis gets carried around by
    // it: at Rotation 90/270 the flip visually hinges top-to-bottom instead of left-to-right.
    // Sandwiching it between an equal and opposite rotateZ cancels that out, keeping the hinge
    // axis screen-vertical regardless of Rotation.
    //
    // The two rotateZ values are the SAME constant in both the flipped and unflipped case (0deg
    // for CardSide.Primary is not "no transform" here - it's this same sandwich with the middle
    // rotateY at 0deg, deliberately) - only rotateY's own 0<->180 ever changes between them. CSS
    // transitions interpolate each transform function's own parameter independently, so if the
    // rotateZ values differed between the two states (e.g. omitted entirely on one side), they'd
    // *also* animate instead of staying fixed, breaking this same compensation mid-transition -
    // every intermediate flip frame would show a stray leftover rotation instead of a clean
    // screen-vertical hinge throughout.
    member private this.FlipStyle () =

        let flipAngle =
            match this.CurrentSide with
            | CardSide.Primary -> 0
            | CardSide.Secondary -> 180

        $"transform: rotateZ({-this.Rotation}deg) rotateY({flipAngle}deg) rotateZ({this.Rotation}deg);"

    member private this.Rotate direction =

        match direction with
        | RotationDirection.Clockwise ->
            this.Rotation <- this.Rotation + 90
        | RotationDirection.CounterClockwise ->
            this.Rotation <- this.Rotation - 90
        this.OnRotationChanged this.Rotation

    member private this.ArrowsVisibility () =

        if this.CanBeRotated && not this.IsFlipping && this.IsHovered then
            "visibility: visible; opacity: 1;"
        else
            "visibility: hidden; opacity: 0;"

    // "" outside delete mode, otherwise which of the two delete-mode affordances applies -
    // deletable (click removes it) or delete-mode-inert (click does nothing, something else
    // still depends on this card).
    member private this.DeleteModeClass () =

        if not this.IsDeleteMode then ""
        elif this.CanBeRemoved then " deletable"
        else " delete-mode-inert"

    // Only the currently-shown side's cues can ever need growth room. For every card type except
    // Modifier, CurrentSide is fixed for the component's whole lifetime (set once by LoreCluster's
    // initialUiState), so the other side's cue is never shown and doesn't need space reserved for
    // it - Modifier cards can change CurrentSide at runtime (see onCardClick), but this still holds
    // since it re-derives from whatever CurrentSide currently is on every render, the same way it
    // already tracks Rotation changing at runtime. Shared by Render() and OnAfterRenderAsync so the
    // two can't drift apart (same precedent as LoreCluster.fs's ComputeMargin()).
    member private this.CurrentSideCues() =
        match this.CurrentSide with
        | CardSide.Primary -> this.Data.PrimarySide
        | CardSide.Secondary -> this.Data.SecondarySide

    member private this.GrowthEdge() =
        CardHelpers.growthEdge (this.CurrentSideCues()) this.ActiveEdge this.Rotation

    // How far .card needs to grow to enclose the active cue's real content, based on the last
    // real measurement (see OnAfterRenderAsync) - 0 before the first measurement lands. Shared by
    // Render() (the actual growth style) and OnAfterRenderAsync (reporting it via
    // OnGrowthChanged) so the two can't drift apart (same precedent as
    // LoreCluster.fs's ComputeMargin()).
    member private this.CurrentGrowthPx() =
        match this.GrowthEdge(), this.MeasuredCueHeight with
        | Some _, Some height -> max 0.0 (height - 50.0)
        | _ -> 0.0

    // StateHasChanged is protected and can't be called directly from within a lambda/task CE -
    // this member wrapper is the standard F#/Blazor workaround (same precedent as
    // LoreCluster.fs's NotifyStateChanged).
    member private this.NotifyStateChanged() =
        this.StateHasChanged()

    // Measures the active Complex cue's real rendered height (if any) after every render, and
    // re-renders only when it actually changed - same guard pattern as LoreCluster.fs's
    // PreviousFootprint/OnAfterRender, so this settles after one extra render pass instead of
    // looping.
    override this.OnAfterRenderAsync(_firstRender: bool) =
        task {
            match this.GrowthEdge() |> Option.map cueRefFor |> Option.bind (fun r -> r.Value) with
            | Some element ->
                // JS interop crosses a real boundary - the referenced element can legitimately go
                // stale between being captured at render time and this call resolving (e.g. a
                // rotation landing mid-flight re-renders the div this ref pointed at). An unhandled
                // exception here would otherwise fault this lifecycle method, which can silently
                // break further interaction with the component.
                try
                    let! height =
                        this.JSRuntime
                            .InvokeAsync<float>("loreBuilderCue.measureHeight", element)
                            .AsTask()

                    if this.MeasuredCueHeight <> Some height then
                        this.MeasuredCueHeight <- Some height
                        this.NotifyStateChanged()
                with ex ->
                    printfn $"Card cue measurement failed: {ex.Message}"
            | _ -> ()

            // Growth can change either because a new measurement just landed above, or because
            // GrowthEdge() itself changed (e.g. the card was rotated away from its growing cue)
            // with no new measurement involved - checked unconditionally here so both cases are
            // covered by the same guard.
            let currentGrowthPx = this.CurrentGrowthPx()

            if this.PreviousGrowthPx <> Some currentGrowthPx then
                this.PreviousGrowthPx <- Some currentGrowthPx
                this.OnGrowthChanged currentGrowthPx
        }
        :> System.Threading.Tasks.Task

    override this.Render() =

        let cardVisuals =
            CardVisuals.fromCardType this.Data.Type

        let growthEdge = this.GrowthEdge()

        // For Left/Right edges, where the cue itself needs to sit, computed from the last real
        // measurement (see OnAfterRenderAsync). Before the first measurement lands, growthPx is 0
        // (matching .card's un-grown base state) and the cue falls back to Card.bolero.css's
        // static default position.
        let growthPx = this.CurrentGrowthPx()

        let cardGrowthStyle =
            match growthEdge, this.MeasuredCueHeight with
            | Some CardEdge.Bottom, Some _ -> $"bottom: -{growthPx}px;"
            | Some CardEdge.Top, Some _ -> $"top: -{growthPx}px;"
            | Some CardEdge.Left, Some _ -> $"left: -{growthPx}px;"
            | Some CardEdge.Right, Some _ -> $"right: -{growthPx}px;"
            | _ -> ""

        let leftRightOffsetPx =
            match growthEdge, this.MeasuredCueHeight with
            | Some CardEdge.Left, Some height
            | Some CardEdge.Right, Some height -> Some (height / 2.0 - 135.0)
            | _ -> None

        let primaryEdgeRefs = if this.StableEdgeRefsSide = CardSide.Primary then Some cueRefFor else None
        let secondaryEdgeRefs = if this.StableEdgeRefsSide = CardSide.Secondary then Some cueRefFor else None

        let onCardClick (_: MouseEventArgs) =
            if this.IsDeleteMode && this.CanBeRemoved then
                this.OnRemove ()
            elif not this.IsDeleteMode && this.CanDiveIn then
                this.OnDiveIn ()

        // Extraction and flipping both live on double-click rather than onCardClick's own
        // single-click branches - neither eligibility check overlaps with the other (a Modifier
        // can never itself be CanBeExtracted - see LoreCluster's canBeExtracted - so a double-click
        // on any one card only ever matches at most one of these two branches).
        let onCardDoubleClick (_: MouseEventArgs) =
            if not this.IsDeleteMode && this.CanBeExtracted then
                this.OnExtract ()
            elif not this.IsDeleteMode && this.Data.Type = CardType.Modifier then
                let newSide =
                    match this.CurrentSide with
                    | CardSide.Primary -> CardSide.Secondary
                    | CardSide.Secondary -> CardSide.Primary

                this.CurrentSide <- newSide
                this.OnCurrentSideChanged newSide

        let diveInClass = if this.CanDiveIn then " dive-in-enabled" else ""

        div {
            attr.``class`` $"card-root{this.DeleteModeClass ()}{diveInClass}"
            attr.style $"width: {this.Size}px; height: {this.Size}px; position: relative;"

            on.mouseover (fun _ -> this.IsHovered <- true)
            on.mouseout (fun _ -> this.IsHovered <- false)
            on.click onCardClick
            on.dblclick onCardDoubleClick

            div {
                attr.``class`` "flippable-card-container"
                attr.style $"transform: rotate({this.Rotation}deg);"

                div {
                    attr.``class`` "card"
                    attr.style $"{cardGrowthStyle}{this.FlipStyle ()}"

                    on.event "transitionstart" (fun _ -> this.IsFlipping <- true)
                    on.event "transitionend" (fun _ -> this.IsFlipping <- false)

                    ({
                        Class = "primary-side"
                        Style = $"color: {cardVisuals.PrimaryTextColor}; background-color: {cardVisuals.ThemeColor}; border: 2px solid {cardVisuals.PrimaryTextColor};"
                        ActiveEdge = this.ActiveEdge
                        Rotation = this.Rotation
                        Type = this.Data.Type
                        Visuals = cardVisuals
                        Cues = this.Data.PrimarySide
                        GrowthEdge = growthEdge
                        EdgeRefs = primaryEdgeRefs
                        LeftRightOffsetPx = leftRightOffsetPx
                    } : CardHelpers.CardData)
                    |> CardHelpers.cardCues

                    ({
                        Class = "secondary-side"
                        // The pre-rotation that makes this face land right-side-up once .card's
                        // own flip brings it forward - previously a static rotateY(180deg)
                        // (Card.bolero.css's old .secondary-side rule), which only canceled
                        // .card's OWN flip correctly when Rotation was 0. Once .card's flip
                        // became the Rotation-aware sandwich above, a plain rotateY(180deg) here
                        // no longer cancels it exactly - conjugating a 180deg Y-rotation by a
                        // Z-rotation negates the Z-rotation's own angle, so this side's content
                        // ended up with a stray extra 2x-Rotation-degree spin (blank/misrotated
                        // edges at Rotation=90/270). Needs the exact same sandwich to cancel
                        // correctly again, for the same reason .card's own flip does.
                        Style = $"color: {cardVisuals.SecondaryTextColor}; background-color: #FFFFFF; border: 2px solid {cardVisuals.SecondaryTextColor}; transform: rotateZ({-this.Rotation}deg) rotateY(180deg) rotateZ({this.Rotation}deg);"
                        ActiveEdge = this.ActiveEdge
                        Rotation = this.Rotation
                        Type = this.Data.Type
                        Visuals = cardVisuals
                        Cues = this.Data.SecondarySide
                        GrowthEdge = growthEdge
                        EdgeRefs = secondaryEdgeRefs
                        LeftRightOffsetPx = leftRightOffsetPx
                    } : CardHelpers.CardData)
                    |> CardHelpers.cardCues
                }
            }
            
            let controlColor =
                match this.CurrentSide with
                | CardSide.Primary -> cardVisuals.PrimaryTextColor
                | CardSide.Secondary -> cardVisuals.SecondaryTextColor
                
            let rotateCounterClockwise (_: MouseEventArgs) =
                this.Rotate RotationDirection.CounterClockwise
                
            let rotateClockwise (_: MouseEventArgs) =
                this.Rotate RotationDirection.Clockwise

            div {
                attr.``class`` "arrow-counter-rotate"
                attr.style $"transform: rotate({this.CounterRotation}deg);"

                CardHelpers.arrow "arrow-left" (this.ArrowsVisibility ()) controlColor rotateCounterClockwise "fa-rotate-left"
                CardHelpers.arrow "arrow-right" (this.ArrowsVisibility ()) controlColor rotateClockwise "fa-rotate-right"
            }

            // Tints the whole card red on hover when it's deletable in delete mode - pointer
            // events pass straight through (see CSS) so it never blocks the click beneath it.
            div { attr.``class`` "delete-overlay" }
        }
