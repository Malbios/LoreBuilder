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

                        cardCue data.Type cue
                    }
                | None ->
                    div {
                        attr.key edgeName
                        attr.``class`` cueClass
                        attr.style cueStyle

                        cardCue data.Type cue
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

    [<Parameter>]
    member val Data = Card.empty with get, set
    
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

    [<Parameter>]
    member val ActiveEdge = None with get, set
    
    [<Parameter>]
    member val Size = 0 with get, set
    
    [<Parameter>]
    member val Rotation = 0 with get, set

    [<Parameter>]
    member val OnRotationChanged: int -> unit = ignore with get, set

    [<Parameter>]
    member val OnRemove: unit -> unit = ignore with get, set

    member private this.FlippedClass () =

        match this.CurrentSide with
        | CardSide.Primary -> ""
        | CardSide.Secondary -> " flipped-card"

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

    // Only the currently-shown side's cues can ever need growth room - CurrentSide never changes
    // at runtime (click-to-flip is permanently removed), so the other side's cue is never shown
    // and doesn't need space reserved for it. Shared by Render() and OnAfterRenderAsync so the
    // two can't drift apart (same precedent as LoreCluster.fs's ComputeMargin()).
    member private this.CurrentSideCues() =
        match this.CurrentSide with
        | CardSide.Primary -> this.Data.PrimarySide
        | CardSide.Secondary -> this.Data.SecondarySide

    member private this.GrowthEdge() =
        CardHelpers.growthEdge (this.CurrentSideCues()) this.ActiveEdge this.Rotation

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
        }
        :> System.Threading.Tasks.Task

    override this.Render() =

        let cardVisuals =
            CardVisuals.fromCardType this.Data.Type

        let growthEdge = this.GrowthEdge()

        // How far .card needs to grow to enclose the active cue's real content, and, for
        // Left/Right edges, where that cue itself needs to sit - both computed from the last
        // real measurement (see OnAfterRenderAsync). Before the first measurement lands, growthPx
        // is 0 (matching .card's un-grown base state) and the cue falls back to
        // Card.bolero.css's static default position.
        let growthPx =
            match growthEdge, this.MeasuredCueHeight with
            | Some _, Some height -> max 0.0 (height - 50.0)
            | _ -> 0.0

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

        let primaryEdgeRefs = if this.CurrentSide = CardSide.Primary then Some cueRefFor else None
        let secondaryEdgeRefs = if this.CurrentSide = CardSide.Secondary then Some cueRefFor else None

        let onCardClick (_: MouseEventArgs) =
            if this.IsDeleteMode && this.CanBeRemoved then
                this.OnRemove ()

        div {
            attr.``class`` $"card-root{this.DeleteModeClass ()}"
            attr.style $"width: {this.Size}px; height: {this.Size}px; position: relative;"

            on.mouseover (fun _ -> this.IsHovered <- true)
            on.mouseout (fun _ -> this.IsHovered <- false)
            on.click onCardClick

            div {
                attr.``class`` "flippable-card-container"
                attr.style $"transform: rotate({this.Rotation}deg);"

                div {
                    attr.``class`` $"card{this.FlippedClass ()}"
                    attr.style cardGrowthStyle

                    on.event "transitionstart" (fun _ -> this.IsFlipping <- true)
                    on.event "transitionend" (fun _ -> this.IsFlipping <- false)

                    ({
                        Class = "primary-side"
                        Style = $"color: {cardVisuals.PrimaryTextColor}; background-color: {cardVisuals.ThemeColor};"
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
                        Style = $"color: {cardVisuals.SecondaryTextColor}; background-color: #FFFFFF; border: 2px solid {cardVisuals.SecondaryTextColor};"
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

            CardHelpers.arrow "arrow-left" (this.ArrowsVisibility ()) controlColor rotateCounterClockwise "fa-rotate-left"
            CardHelpers.arrow "arrow-right" (this.ArrowsVisibility ()) controlColor rotateClockwise "fa-rotate-right"

            // Tints the whole card red on hover when it's deletable in delete mode - pointer
            // events pass straight through (see CSS) so it never blocks the click beneath it.
            div { attr.``class`` "delete-overlay" }
        }
