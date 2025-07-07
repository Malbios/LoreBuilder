namespace LoreBuilder.Model

open LoreBuilder

[<RequireQualifiedAccess>]
module Application =
    
    type State = {
        Page: Page
        Error: string option
        UserSettings: UserSettings
        TestPageState: TestPage.State
    }

    module State =
        
        let initial = {
            Page = Page.Root
            Error = None
            UserSettings = {
                Theme = Some ThemeMode.Dark
            }
            TestPageState = TestPage.State.initial
        }

    type Message =
        | None
        | SetPage of Page
        | Error of exn
        | ClearError
        | SetThemeMode of ThemeMode
        | TestPageMsg of TestPage.Message
