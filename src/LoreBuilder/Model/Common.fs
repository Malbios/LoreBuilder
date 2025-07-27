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
    | And of 'T list
    | Or of 'T list
