namespace LoreBuilder

open System
open Bolero
open Bolero.Html
open FunSharp.Common
open LoreBuilder.Model

[<RequireQualifiedAccess>]
module Utils =
    
    let private pickRandom (items: 'T list) =

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
        |> List.filter (fun cardType -> cardType <> CardType.Unknown)
        |> List.map randomCard
        
    // Only one Modifier card exists in Data/Modifiers.fs today, so this is deterministic for now -
    // written generically since more will be added later.
    let randomModifierCard () = Modifiers.cards |> pickRandom

    let allCards = [
        Factions.cards; Figures.cards; Events.cards; Locations.cards; Objects.cards; Creatures.cards; Materials.cards; Deities.cards; Emblems.cards; Modifiers.cards
    ]

    let renderList (nodes: Node list) =
        concat {
            for node in nodes do node
        }
