namespace LoreBuilder.Model

open System

[<RequireQualifiedAccess>]
module Version =
    let current = Version(0, 0, 1)

[<RequireQualifiedAccess>]
type ThemeMode =
    | Light
    | Dark
