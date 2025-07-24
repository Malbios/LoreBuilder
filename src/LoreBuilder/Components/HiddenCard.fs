namespace LoreBuilder.Components

open Bolero
open Bolero.Html
open LoreBuilder.Model
open Microsoft.AspNetCore.Components

type HiddenCard() =
    inherit Component()
        
    override _.CssScope = CssScopes.HiddenCard
    
    [<Parameter>]
    member val Data: LoreBuilder.Model.Card = Card.empty with get, set
    
    [<Parameter>]
    member val Size: int = 0 with get, set

    override this.Render() =
        
        let cardVisuals = CardVisuals.fromCardType this.Data.Type
        
        div {
            attr.style $"position: relative; background-color: {cardVisuals.ThemeColor}; width: {this.Size}px; height: {this.Size}px;"
            
            comp<CardBadge> {
                "Visuals" => cardVisuals
            }
        }
