namespace LoreBuilder.Pages

open Bolero
open Bolero.Html
open Elmish
open LoreBuilder
open LoreBuilder.Components
open LoreBuilder.Model
open Microsoft.AspNetCore.Components
open Microsoft.Extensions.Logging
open Radzen
open Radzen.Blazor

type LoreClusterTest() =
    inherit Component()
    
    override _.CssScope = CssScopes.LoreBuilder
    
    override _.Render() =
        
        Node.Empty()
