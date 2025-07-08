namespace LoreBuilder.Components

open Bolero
open Bolero.Html

type FactionTestCard() =
    inherit Component()
    
    override _.CssScope = CssScopes.FactionTestCard

    override _.Render() =
        
        div {
            attr.``class`` "card"
            
            div {
                attr.``class`` "label top"
                
                "Entourage"
            }
            
            div {
                attr.``class`` "label bottom"
                
                "Circle"
            }
            
            div {
                attr.``class`` "label left"
                
                "Syndicate"
            }
            
            div {
                attr.``class`` "label right"
                
                "Command"
            }
            
            div {
                attr.``class`` "center"
                
                div {
                    attr.``class`` "circle"
                }
                
                div {
                    attr.``class`` "label"
                    
                    "Faction"
                }
            }
        }
