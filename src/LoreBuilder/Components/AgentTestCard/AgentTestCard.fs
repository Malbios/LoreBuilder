namespace LoreBuilder.Components

open Bolero
open Bolero.Html

type AgentTestCard() =
    inherit Component()
    
    override _.CssScope = CssScopes.AgentTestCard

    override _.Render() =
                
        div {
            attr.``class`` "card"
            
            div {
                attr.``class`` "role role-top"
                
                "A Writer"
            }
            
            div {
                attr.``class`` "role role-right"
                
                "A Blademaster"
            }
            
            div {
                attr.``class`` "role role-bottom"
                
                "A Storyteller"
            }
            
            div {
                attr.``class`` "role role-left"
                
                "A Scion"
            }
            
            div {
                attr.``class`` "center-icon"
                
                i { attr.``class`` "fa-solid fa-user fa-2x" }
            }
        }
