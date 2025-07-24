namespace LoreBuilder

open System
open Bolero
open Bolero.Html
open FunSharp.Common
open LoreBuilder.Model

[<RequireQualifiedAccess>]
module Utils =
    
    let private pickRandom (items: string list) =
        
        let rnd = Random()
        let index = rnd.Next(0, List.length items)
        
        items |> List.item index

    
    let private randomSideText () =
        
        [
            "A Writer"
            "A Blademaster"
            "A Storyteller"
            "A Scion"
        ]
        |> pickRandom
        
    let card cardType = {
        Id = Guid.NewGuid()
        Type = cardType
        Front = {
            Top = randomSideText ()
            Right = randomSideText ()
            Bottom = randomSideText ()
            Left = randomSideText ()
        }
        Back = {
            Top = randomSideText ()
            Right = randomSideText ()
            Bottom = randomSideText ()
            Left = randomSideText ()
        }
    }

    let cards =
        
        Union.toList<CardType>()
        |> List.map card

    let renderList (nodes: Node list) =
        concat {
            for node in nodes do node
        }
