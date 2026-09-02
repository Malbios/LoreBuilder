namespace LoreBuilder.Model

open System

[<RequireQualifiedAccess>]
module Version =
    let current = Version(0, 0, 1)

[<RequireQualifiedAccess>]
type ThemeMode =
    | Light
    | Dark
    
[<RequireQualifiedAccess>]
type Logical<'T> =
    | One of 'T
    | Any of 'T list
    | All of 'T list

[<RequireQualifiedAccess>]
module Logical =

    // All is treated the same as Any here: there's currently only one physical attachment
    // point per Logical requirement (e.g. a single outer cluster slot), so "requires all of
    // these" can't be distinguished from "accepts any of these" until multiple attachment
    // points exist.
    let accepts logical value =
        match logical with
        | Logical.One expected -> value = expected
        | Logical.Any expected
        | Logical.All expected -> List.contains value expected

    // The card type(s) accepted at a given 0-based attachment-point index, or None if no
    // attachment point exists there. All models one mandatory, position-locked slot per list
    // item (index i only exists if the list has at least i+1 items, and only accepts that
    // specific item's type) - unlike One/Any, which always describe a single slot (index 0)
    // accepting any of their listed type(s), since "one of" / "any of" naturally means
    // "satisfied by any single match", not several independent slots.
    let slotTypes index logical =
        match logical, index with
        | Logical.One expected, 0 -> Some [ expected ]
        | Logical.Any expected, 0 -> Some expected
        | Logical.All items, i -> items |> List.tryItem i |> Option.map List.singleton
        | _ -> None

    let acceptsAt index logical value =
        slotTypes index logical |> Option.exists (List.contains value)

    // Every value this Logical would accept at a single attachment point (unlike slotTypes,
    // which position-locks All to one item per index for multiple physical slots) - the
    // candidate list offered when there's only one slot to fill, e.g. LoreCluster.fs's
    // innerRequiredType. All is treated the same as Any here, for the same reason accepts does.
    let candidates logical =
        match logical with
        | Logical.One expected -> [ expected ]
        | Logical.Any expected
        | Logical.All expected -> expected

[<RequireQualifiedAccess>]
type RotationDirection =
    | Clockwise
    | CounterClockwise
