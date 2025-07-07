namespace LoreBuilder

open System
open Elmish
open FunSharp.Common
open LoreBuilder.Model

module Update =
    
    let update (_: NavigationType -> string -> unit) _ message (model: Application.State) =

        match message with
        | Application.Message.SetPage page ->
            let model, cmd =
                match page with
                | Page.Root -> { model with Page = page }, Cmd.ofMsg (Application.Message.SetPage Page.TestPage)
                | _ -> { model with Page = page }, Cmd.none

            { model with Error = None }, cmd
            
        | Application.Message.Error(HttpException error) ->
            { model with Error = error |> HttpError.getMessage |> Some }, Cmd.none
            
        | Application.Message.Error exn ->
            Console.WriteLine "an error occurred"
            
            { model with Error = Some exn.Message }, Cmd.none

        | Application.Message.ClearError -> { model with Error = None }, Cmd.none

        | Application.Message.SetThemeMode theme ->
            let settings = { model.UserSettings with Theme = Some theme }

            { model with UserSettings = settings }, Cmd.none
            
        | Application.Message.TestPageMsg msg ->
            let subModel, cmd = TestPage.update msg model.TestPageState
            
            { model with TestPageState = subModel }, cmd

        | Application.Message.None -> model, Cmd.none
