namespace LoreBuilder.Model

open LoreBuilder

[<RequireQualifiedAccess>]
module Application =

    type UserSettings = {
        Theme: ThemeMode option
    }
    
    module UserSettings =
        
        let initial = {
            Theme = Some ThemeMode.Dark
        }
    
    type State = {
        Page: Page
        UserSettings: UserSettings
        HoverTest: HoverTest.State
        StackTest: StackTest.State
    }

    module State =
        
        let initial = {
            Page = Page.Root
            UserSettings = UserSettings.initial
            HoverTest = HoverTest.State.initial
            StackTest = StackTest.State.initial
        }

    type Message =
        | None
        | SetPage of Page
        | SetThemeMode of ThemeMode
        | HoverTestMsg of HoverTest.Message
        | StackTestMsg of StackTest.Message
