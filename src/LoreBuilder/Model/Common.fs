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
