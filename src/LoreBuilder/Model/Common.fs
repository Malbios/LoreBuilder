namespace LoreBuilder.Model

open System

[<RequireQualifiedAccess>]
module Version =
    let current = Version(0, 0, 1)

[<RequireQualifiedAccess>]
type LoreBuilderError =
    | Unexpected of message: string * inner: exn

[<RequireQualifiedAccess>]
type NavigationType =
    | Local
    | External

[<RequireQualifiedAccess>]
type ThemeMode =
    | Light
    | Dark

[<RequireQualifiedAccess>]
type UserSettings = {
    Theme: ThemeMode option
}
