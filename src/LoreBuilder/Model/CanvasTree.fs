namespace LoreBuilder.Model

open System
open System.Collections.Generic

// Pure(-ish - these do mutate the Dictionary they're given, same convention Pages/Home.fs's own
// overlap-check closures already use) logic for navigating and maintaining the canvas hierarchy -
// kept dependency-free (no Bolero/DOM) so it can be unit tested directly, the same reasoning
// ClusterPlacement.fs (removed - see this feature's plan for why) was split out for.
module CanvasTree =

    // Which of this canvas's own positions (on the given cluster) currently have a live child
    // canvas - see CanvasState.ChildCanvasOf's own doc comment for what this drives.
    let lockedPositionsFor (canvas: CanvasState) (clusterId: Guid) : Set<ClusterPosition> =
        canvas.ChildCanvasOf.Keys
        |> Seq.choose (fun (cid, position) -> if cid = clusterId then Some position else None)
        |> Set.ofSeq

    // The root-first chain of canvas ids from root down to activeId, by walking ParentLink
    // backward - no separate stored navigation stack to keep in sync with ActiveCanvasId.
    let breadcrumbTrail (canvases: Dictionary<Guid, CanvasState>) (activeId: Guid) : Guid list =
        let rec walk id acc =
            match canvases.TryGetValue id with
            | false, _ -> acc
            | true, canvas ->
                match canvas.ParentLink with
                | None -> id :: acc
                | Some(parentId, _, _) -> walk parentId (id :: acc)

        walk activeId []

    // Removes a now-empty sub-canvas entirely and unlinks it from its parent's ChildCanvasOf,
    // which is what naturally re-enables rotation/extraction on that position (see
    // CanvasState.ChildCanvasOf's own doc comment - no explicit "unlock" step needed beyond this).
    // No-op for the root canvas (identified by having no ParentLink) - root's own
    // OnClusterEmptied handling is unrelated to any of this.
    // Returns the id the caller should navigate to if it was currently viewing the now-removed
    // canvas (its parent), or None if no navigation change is needed.
    let removeEmptySubCanvas
        (canvases: Dictionary<Guid, CanvasState>)
        (canvasId: Guid)
        (activeCanvasId: Guid)
        : Guid option =

        match canvases.TryGetValue canvasId with
        | false, _ -> None
        | true, canvas ->
            match canvas.ParentLink with
            | None -> None
            | Some(parentCanvasId, parentClusterId, parentPosition) ->
                canvases.Remove canvasId |> ignore

                match canvases.TryGetValue parentCanvasId with
                | true, parentCanvas -> parentCanvas.ChildCanvasOf.Remove((parentClusterId, parentPosition)) |> ignore
                | false, _ -> ()

                if activeCanvasId = canvasId then Some parentCanvasId else None
