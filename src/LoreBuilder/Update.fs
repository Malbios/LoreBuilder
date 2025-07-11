namespace LoreBuilder

open Elmish
open LoreBuilder.Model
open LoreBuilder.Pages
open Microsoft.Extensions.Logging

module Update =
    let update (logger: ILogger) message (model: Application.State) =

        match message with
        
        | Application.Message.SetPage page ->
            { model with Page = page }, Cmd.none

        | Application.Message.SetThemeMode theme ->
            { model with Application.State.UserSettings.Theme = Some theme }, Cmd.none

        | Application.Message.None -> model, Cmd.none

        | Application.Message.HoverTestMsg message ->
            let subModel, cmd = HoverTest.update message model.HoverTest
            { model with HoverTest = subModel }, cmd

        | Application.StackTestMsg message ->
            let subModel, cmd = StackTest.update logger message model.StackTest
            { model with StackTest = subModel }, cmd
