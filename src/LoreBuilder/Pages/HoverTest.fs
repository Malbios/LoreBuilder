namespace LoreBuilder.Pages

open Bolero
open Bolero.Html
open Elmish
open FunSharp.Common
open FunSharp.Components
open LoreBuilder.Model
open Radzen
open Radzen.Blazor

type HoverTest() =
    inherit ElmishComponent<HoverTest.State, HoverTest.Message>()
    
    override _.CssScope = CssScopes.LoreBuilder
    
    override this.ShouldRender(oldModel, newModel) =
        
        oldModel.HoverText <> newModel.HoverText

    override this.View model dispatch =
        
        comp<RadzenStack> {
            "Orientation" => Orientation.Vertical
            
            comp<RadzenStack> {
                "Orientation" => Orientation.Horizontal

                comp<HoverArea> {
                    "OnMouseOver" => fun () -> dispatch (HoverTest.Message.SetHoverText "Item 1")
                    "OnMouseOut" => fun () -> dispatch HoverTest.Message.ClearHoverText
                }

                comp<HoverArea> {
                    "OnMouseOver" => fun () -> dispatch (HoverTest.Message.SetHoverText "Item 2")
                    "OnMouseOut" => fun () -> dispatch HoverTest.Message.ClearHoverText
                }

                comp<HoverArea> {
                    "OnMouseOver" => fun () -> dispatch (HoverTest.Message.SetHoverText "Item 3")
                    "OnMouseOut" => fun () -> dispatch HoverTest.Message.ClearHoverText
                }
            }

            div {
                attr.style "text-align: center;"
                
                cond (System.String.IsNullOrWhiteSpace model.HoverText)
                <| function
                    | true -> p { "<hover over a tile>" }
                    | false -> p { model.HoverText }
            }
        }

[<RequireQualifiedAccess>]
module HoverTest =
    
    let update message (model: HoverTest.State) =
        
        match message with
        
        | HoverTest.Message.SetHoverText text ->
            { model with HoverText = text }, Cmd.none
            
        | HoverTest.Message.ClearHoverText ->
            { model with HoverText = String.empty }, Cmd.none
