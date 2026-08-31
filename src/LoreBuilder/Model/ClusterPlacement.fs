namespace LoreBuilder.Model

// Pure placement math for spawning a brand-new cluster near an existing one (see Pages/Home.fs's
// OnExtractCard) - kept dependency-free (no Bolero/DOM) so it can be unit tested directly.
module ClusterPlacement =

    let private cardinalOffsets ring =
        [ (0, ring); (0, -ring); (-ring, 0); (ring, 0) ]

    // The rest of a ring's square perimeter, excluding the 4 cardinal cells (tried separately,
    // first, since they get an exact/unambiguous orientation - see directionFor).
    let private perimeterOffsets ring =
        [
            for dx in -ring .. ring do
                for dy in -ring .. ring do
                    if (abs dx = ring || abs dy = ring) && dx <> 0 && dy <> 0 then
                        (dx, dy)
        ]

    // Which of the new cluster's Inner slots should hold the auto-attached Modifier so it
    // visually faces back toward the source cluster - the slot on the *opposite* side from where
    // the new cluster landed (e.g. placed below the source -> its Inner_Top faces back up at it).
    // For a non-cardinal offset, uses whichever axis dominates as a best-effort orientation rather
    // than an arbitrary constant, only truly arbitrary (Inner_Bottom) at an exact diagonal tie.
    let private directionFor (dx: int, dy: int) =
        if abs dx > abs dy then
            if dx > 0 then ClusterPosition.Inner_Left else ClusterPosition.Inner_Right
        elif abs dy > abs dx then
            if dy > 0 then ClusterPosition.Inner_Top else ClusterPosition.Inner_Bottom
        else
            ClusterPosition.Inner_Bottom

    // Generous but finite - a solid ring of clusters in every direction out to this distance is
    // practically unreachable on this canvas, so this is just a safety cap against ever looping
    // forever, not a limit expected to matter in normal use.
    let private maxRing = 50

    // Searches outward in cellSize-sized rings around sourcePos for a free spot for a
    // newly-extracted cluster, preferring (within each ring) the 4 cardinal directions before the
    // rest of that ring's perimeter. wouldOverlap should report whether a candidateFootprint at a
    // candidate canvas-space position would overlap any existing cluster (see Home.fs's
    // wouldOverlapAny). Returns the free position plus which Inner slot should hold the
    // auto-attached Modifier - valid only for a cluster about to be created fresh (a freshly
    // spawned LoreCluster always starts at ClusterRotation = 0), not for orienting relative to an
    // already-rotated existing cluster.
    let findExtractionSpot
        (wouldOverlap: float -> float * float -> bool)
        (cellSize: float)
        (footprint: float)
        (sourcePos: float * float)
        : ((float * float) * ClusterPosition) option =

        let sx, sy = sourcePos

        let tryOffset (dx, dy) =
            let candidate = (sx + float dx * cellSize, sy + float dy * cellSize)
            if wouldOverlap footprint candidate then None else Some(candidate, directionFor (dx, dy))

        [ 1 .. maxRing ]
        |> List.tryPick (fun ring ->
            cardinalOffsets ring
            |> List.tryPick tryOffset
            |> Option.orElseWith (fun () -> perimeterOffsets ring |> List.tryPick tryOffset))
