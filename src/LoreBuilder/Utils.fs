namespace LoreBuilder

open System
open System.Reflection
open System.IO
open Bolero
open Bolero.Html
open FunSharp.Common
open LoreBuilder.Model
open Newtonsoft.Json
open Newtonsoft.Json.Linq

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

    let private readEmbeddedJson file =
        
        let asm = Assembly.GetExecutingAssembly()
        use stream = asm.GetManifestResourceStream(file)
        use reader = new StreamReader(stream)
        reader.ReadToEnd()
        
    let private cueFromJson side (obj: JObject) =
        
        let sideData = obj.GetValue(side)
        
        match sideData.Type with
        | JTokenType.String ->
            let s = sideData.ToObject<string>()
            if s.EndsWith(".svg") then
                Cue.Icon s
            else
                Cue.Simple s
        | JTokenType.Object -> sideData.ToObject<Cue>()
        | _ -> failwith $"unexpected JTokenType: {sideData.Type}"
        
    let private cuesFromJson direction (obj: JObject) =
        
        let cuesData = obj.GetValue(direction).ToObject<JObject>()
        
        {
            Bottom = cuesData |> cueFromJson "Bottom" |> Some
            Left = cuesData |> cueFromJson "Left" |> Some
            Top = cuesData |> cueFromJson "Top" |> Some
            Right = cuesData |> cueFromJson "Right" |> Some
        }
    
    let private cardsFromFile cardType file =
        
        try
            readEmbeddedJson file
            |> JsonConvert.DeserializeObject<JObject list>
            |> List.map (fun obj ->
                {
                    Type = cardType
                    PrimarySide = obj |> cuesFromJson "Primary"
                    SecondarySide = obj |> cuesFromJson "Secondary"
                }
            )
            |> Ok
        with ex ->
            ex |> Error
        
    // let factions =
    //     "LoreBuilder.Data.factions.json" |> cardsFromFile CardType.Faction
    // let figures =
    //     "LoreBuilder.Data.figures.json" |> cardsFromFile CardType.Figure
    // let events =
    //     "LoreBuilder.Data.events.json" |> cardsFromFile CardType.Event
    // let locations =
    //     "LoreBuilder.Data.locations.json" |> cardsFromFile CardType.Location
    // let objects =
    //     "LoreBuilder.Data.objects.json" |> cardsFromFile CardType.Object
    // let creatures =
    //     "LoreBuilder.Data.creatures.json" |> cardsFromFile CardType.Creature
    // let materials =
    //     "LoreBuilder.Data.materials.json" |> cardsFromFile CardType.Material
    // let deities =
    //     "LoreBuilder.Data.deities.json" |> cardsFromFile CardType.Deity
    // let emblems =
    //     "LoreBuilder.Data.emblems.json" |> cardsFromFile CardType.Emblem
    // let modifiers =
    //     "LoreBuilder.Data.modifiers.json" |> cardsFromFile CardType.Modifier
        
    let allCards =
        [ Factions.cards ]

    let renderList (nodes: Node list) =
        concat {
            for node in nodes do node
        }
