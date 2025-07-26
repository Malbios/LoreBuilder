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
            Top = { Cue.empty with Text = randomCueText () }
            Right = { Cue.empty with Text = randomCueText () }
            Bottom = { Cue.empty with Text = randomCueText () }
            Left = { Cue.empty with Text = randomCueText () }
        }
        SecondarySide = {
            Top = { Cue.empty with Text = randomCueText () }
            Right = { Cue.empty with Text = randomCueText () }
            Bottom = { Cue.empty with Text = randomCueText () }
            Left = { Cue.empty with Text = randomCueText () }
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
    
    let private cards file cardType =
        
        readEmbeddedJson file
        |> JsonConvert.DeserializeObject<JObject list>
        |> List.map (fun obj ->
            {
                Type = cardType
                PrimarySide = obj.GetValue("Primary").ToObject<Cues>()
                SecondarySide = obj.GetValue("Secondary").ToObject<Cues>()
            }
        )
        
    let factions =
        cards "LoreBuilder.Data.factions.json" CardType.Faction
    let figures =
        cards "LoreBuilder.Data.figures.json" CardType.Figure
    let events =
        cards "LoreBuilder.Data.events.json" CardType.Event
    let locations =
        cards "LoreBuilder.Data.locations.json" CardType.Location
    let objects =
        cards "LoreBuilder.Data.objects.json" CardType.Object
    let creatures =
        cards "LoreBuilder.Data.creatures.json" CardType.Creature
    let materials =
        cards "LoreBuilder.Data.materials.json" CardType.Material
    let deities =
        cards "LoreBuilder.Data.deities.json" CardType.Deity
    let emblems =
        cards "LoreBuilder.Data.emblems.json" CardType.Emblem
    let modifiers =
        cards "LoreBuilder.Data.modifiers.json" CardType.Modifier
        
    let allCards = [ factions; figures; events; locations; objects; creatures; materials; deities; emblems; modifiers ]

    let renderList (nodes: Node list) =
        concat {
            for node in nodes do node
        }
