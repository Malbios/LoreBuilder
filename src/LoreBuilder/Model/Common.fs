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

[<RequireQualifiedAccess>]
type RotationDirection =
    | Clockwise
    | CounterClockwise
