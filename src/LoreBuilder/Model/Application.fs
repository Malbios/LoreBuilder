namespace LoreBuilder.Model

open LoreBuilder

[<RequireQualifiedAccess>]
module Application =
    
    type State = {
        Page: Page
        Error: string option
        UserSettings: UserSettings
        HoverTestState: HoverTest.State
        DragDropTestState: DragDropTest.State
    }

    module State =
        
        let initial = {
            Page = Page.Root
            Error = None
            UserSettings = {
                Theme = Some ThemeMode.Dark
            }
            HoverTestState = HoverTest.State.initial
            DragDropTestState = DragDropTest.State.initial
        }

    type Message =
        | None
        | SetPage of Page
        | Error of exn
        | ClearError
        | SetThemeMode of ThemeMode
        | HoverTestMsg of HoverTest.Message
        | DragDropTestMsg of DragDropTest.Message
