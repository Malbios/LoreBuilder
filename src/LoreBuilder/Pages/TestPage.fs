namespace LoreBuilder

open Bolero
open Bolero.Html
open Elmish
open Radzen
open Radzen.Blazor
open FunSharp.Common
open LoreBuilder.Components
open LoreBuilder.Model

[<RequireQualifiedAccess>]
module TestPage =
    
    let update message (model: TestPage.State) =
        
        match message with
        | TestPage.Message.SetHoverText text -> { model with HoverText = text }, Cmd.none
        | TestPage.Message.ClearHoverText -> { model with HoverText = String.empty }, Cmd.none
            
    let view (model: TestPage.State) dispatch : Node =

        div {
            attr.``class`` "center-wrapper"

            comp<RadzenStack> {
                "Orientation" => Orientation.Vertical
                
                comp<RadzenStack> {
                    "Orientation" => Orientation.Horizontal

                    comp<HoverArea> {
                        "OnMouseOver" => fun () -> dispatch (TestPage.Message.SetHoverText "Item 1")
                        "OnMouseOut" => fun () -> dispatch TestPage.Message.ClearHoverText
                    }

                    comp<HoverArea> {
                        "OnMouseOver" => fun () -> dispatch (TestPage.Message.SetHoverText "Item 2")
                        "OnMouseOut" => fun () -> dispatch TestPage.Message.ClearHoverText
                    }

                    comp<HoverArea> {
                        "OnMouseOver" => fun () -> dispatch (TestPage.Message.SetHoverText "Item 3")
                        "OnMouseOut" => fun () -> dispatch TestPage.Message.ClearHoverText
                    }
                }

                div {
                    attr.``class`` "center-text"
                    
                    cond (System.String.IsNullOrWhiteSpace model.HoverText) <| function
                        | true -> p { "<hover over a tile>" }
                        | false -> p { model.HoverText }
                }
                
                // <div class="role-square">
                //   <div class="role role-top">A WRITER</div>
                //   <div class="role role-right">A BLADEMASTER</div>
                //   <div class="role role-bottom">A STORYTELLER</div>
                //   <div class="role role-left">A SCION</div>
                //   <div class="center-icon">👤</div>
                // </div>
                
                div {
                    attr.``class`` "role-square"
                    
                    div {
                        attr.``class`` "role role-top"
                        
                        "A WRITER"
                    }
                    
                    div {
                        attr.``class`` "role role-right"
                        
                        "A BLADEMASTER"
                    }
                    
                    div {
                        attr.``class`` "role role-bottom"
                        
                        "A STORYTELLER"
                    }
                    
                    div {
                        attr.``class`` "role role-left"
                        
                        "A SCION"
                    }
                    
                    div {
                        attr.``class`` "center-icon"
                        
                        i { attr.``class`` "fa-solid fa-user fa-2x" }
                    }
                }
            }
        }
