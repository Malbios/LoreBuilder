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

    
    let private randomCueText () =
        
        [
            "A Writer"
            "A Blademaster"
            "A Storyteller"
            "A Scion"
        ]
        |> pickRandom
        
    let randomCard cardType = {
        Type = cardType
        PrimarySide = {
            Top = Cue.Simple (randomCueText ()) |> Some
            Right = Cue.Simple (randomCueText ()) |> Some
            Bottom = Cue.Simple (randomCueText ()) |> Some
            Left = Cue.Simple (randomCueText ()) |> Some
        }
        SecondarySide = {
            Top = Cue.Simple (randomCueText ()) |> Some
            Right = Cue.Simple (randomCueText ()) |> Some
            Bottom = Cue.Simple (randomCueText ()) |> Some
            Left = Cue.Simple (randomCueText ()) |> Some
        }
    }

    let randomCards =
        
        Union.toList<CardType>()
        |> List.map randomCard
        
    let allCards = [
        Factions.cards; Figures.cards; Events.cards; Locations.cards; Objects.cards; Creatures.cards; Materials.cards; Deities.cards; Emblems.cards; Modifiers.cards
    ]

    let renderList (nodes: Node list) =
        concat {
            for node in nodes do node
        }
