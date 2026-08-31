namespace LoreBuilder.Model

open System
open System.Collections.Generic

// A container, not an immutable value - most fields are Dictionaries mutated in place on every
// drag/drop tick, the same convention Pages/Home.fs already used for its own flat
// clusterPositions/etc. before they moved here. Only Zoom is a plain mutable scalar field, for
// the same reason.
type CanvasState = {
    Id: Guid

    // None for the root canvas. Some (parentCanvasId, parentClusterId, parentPosition) for every
    // sub-canvas - set once at creation, never changes afterward. Drives the deletion cascade
    // (a sub-canvas's lone cluster being emptied tears down the whole canvas and unlocks the
    // parent position) and the breadcrumb's ancestor walk.
    ParentLink: (Guid * Guid * ClusterPosition) option

    // A copy of the card and Primary rotation this canvas was spawned from, kept locally rather
    // than re-derived from the parent's own dictionaries each render - so breadcrumb labeling
    // still works even if the parent canvas itself has since been torn down some other way.
    // None for the root canvas.
    SpawnedFromCard: Card option
    SpawnedFromRotation: int option

    mutable Zoom: float

    ClusterPositions: Dictionary<Guid, float * float>
    ClusterFootprints: Dictionary<Guid, float>
    InitialCards: Dictionary<Guid, Card>
    InitialInnerCards: Dictionary<Guid, ClusterPosition * Card>
    InitialPrimaryRotations: Dictionary<Guid, int>

    // Which of this canvas's own (clusterId, position) pairs currently own a live child canvas,
    // and which one - the per-canvas replacement for the old flat extractionSources. Drives
    // LoreCluster's LockedPositions (rotation lock + re-extraction lock, one Set for both), the
    // dive-in navigation target, and the deletion cascade's "remove the parent's lock" step.
    ChildCanvasOf: Dictionary<Guid * ClusterPosition, Guid>
}

module CanvasState =

    let createRoot id = {
        Id = id
        ParentLink = None
        SpawnedFromCard = None
        SpawnedFromRotation = None
        Zoom = 1.0
        ClusterPositions = Dictionary()
        ClusterFootprints = Dictionary()
        InitialCards = Dictionary()
        InitialInnerCards = Dictionary()
        InitialPrimaryRotations = Dictionary()
        ChildCanvasOf = Dictionary()
    }

    // A sub-canvas always starts with exactly one cluster (the extracted copy), already seeded -
    // there's no "empty, waiting for a drop" state for a sub-canvas the way root has one, since
    // drag-and-drop is disabled there entirely.
    let createSubCanvas
        (id: Guid)
        (parentCanvasId: Guid)
        (parentClusterId: Guid)
        (parentPosition: ClusterPosition)
        (clusterId: Guid)
        (clusterPosition: float * float)
        (primaryCard: Card)
        (primaryRotation: int)
        (innerPosition: ClusterPosition)
        (innerCard: Card)
        (footprint: float)
        =
        let canvas = createRoot id

        canvas.ClusterPositions[clusterId] <- clusterPosition
        canvas.ClusterFootprints[clusterId] <- footprint
        canvas.InitialCards[clusterId] <- primaryCard
        canvas.InitialInnerCards[clusterId] <- (innerPosition, innerCard)
        canvas.InitialPrimaryRotations[clusterId] <- primaryRotation

        { canvas with
            ParentLink = Some(parentCanvasId, parentClusterId, parentPosition)
            SpawnedFromCard = Some primaryCard
            SpawnedFromRotation = Some primaryRotation }
