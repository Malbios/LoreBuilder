namespace LoreBuilder

open System
open Bolero
open Bolero.Html
open FunSharp.Common
open LoreBuilder.Model

[<RequireQualifiedAccess>]
module Utils =
    
    let pickRandom (items: 'T list) =

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
        
    // Deterministic only in that CardData's own JSON currently has one Modifier card - written
    // generically since more will be added there over time.
    let randomModifierCard () =
        CardData.pool ()
        |> List.tryFind (fun pool -> pool |> List.tryHead |> Option.exists (fun card -> card.Type = CardType.Modifier))
        |> Option.map pickRandom
        |> Option.defaultValue Card.empty

    // A function, not a plain `let`-bound value - CardData.pool() only has real data in it once
    // CardData.loadAsync has resolved (see Pages/Home.fs's OnInitializedAsync), and a `let` here
    // would evaluate once at module load, before that ever runs, permanently caching an empty list.
    let allCards () = CardData.pool ()

    // One random card per requested type, independently - a "pick one of two locations" slot
    // (Logical.Any [location; location]) asks for this with the same type listed twice, and gets
    // one independent roll per entry back (no distinctness guarantee, same as randomModifierCard
    // above - every type's pool is a single card today, so this necessarily returns the same card
    // twice for a repeated type until more data exists).
    let randomCandidatesFor (types: CardType list) : Card list =
        types
        |> List.map (fun cardType ->
            CardData.pool ()
            |> List.tryFind (fun pool -> pool |> List.tryHead |> Option.exists (fun card -> card.Type = cardType))
            |> Option.map pickRandom
            |> Option.defaultValue Card.empty)

    let renderList (nodes: Node list) =
        concat {
            for node in nodes do node
        }
