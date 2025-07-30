namespace LoreBuilder.Builders

open FunSharp.Common
open LoreBuilder.Model

module Cards =
    
    type ComplexCueBuilder(kind: string) =
        
        member _.Yield _ = {
            Header = (Some kind)
            Text = ""
            Expansions = None
        }

        [<CustomOperation("text")>]
        member _.Text(state, value) =
            { state with Text = value }

        [<CustomOperation("expansion")>]
        member _.Expansion(state, value) =
            { state with Expansions = Some (Logical.One value) }

        [<CustomOperation("expansions_any")>]
        member _.ExpansionsAny(state, value) =
            { state with Expansions = Some (Logical.Any value) }

        [<CustomOperation("expansions_all")>]
        member _.ExpansionsAll(state, value) =
            { state with Expansions = Some (Logical.All value) }

        member _.Run(state) =
            Cue.Complex state
            
    type CuesBuilder() =
        
        member _.Yield _ = {
            Top = None
            Right = None
            Bottom = None
            Left = None
        }

        [<CustomOperation("bottom")>]
        member _.Bottom(state, value) =
            { state with Bottom = Some value }

        [<CustomOperation("bottom")>]
        member _.Bottom(state, value) =
            { state with Bottom = Some (Cue.Simple value) }

        [<CustomOperation("left")>]
        member _.Left(state, value) =
            { state with Left = Some value }

        [<CustomOperation("left")>]
        member _.Left(state, value) =
            { state with Left = Some (Cue.Simple value) }

        [<CustomOperation("top")>]
        member _.Top(state, value) =
            { state with Top = Some value }

        [<CustomOperation("top")>]
        member _.Top(state, value) =
            { state with Top = Some (Cue.Simple value) }

        [<CustomOperation("right")>]
        member _.Right(state, value) =
            { state with Right = Some value }

        [<CustomOperation("right")>]
        member _.Right(state, value) =
            { state with Right = Some (Cue.Simple value) }

        member _.Run(state) : Cues =
            state
            
    type CardBuilder(kind: CardType) =
        
        member _.Yield _ = {
            Type = kind
            PrimarySide = Cues.empty
            SecondarySide = Cues.empty
        }
            
        [<CustomOperation("primary")>]
        member _.Primary(state, value) =
            { state with PrimarySide = value }
            
        [<CustomOperation("secondary")>]
        member _.Secondary(state, value) =
            { state with SecondarySide = value }

        member _.Run(state) : Card =
            state

    let icon name = Cue.Icon $"{name}.svg"
    
    let background = ComplexCueBuilder("Background")
    let agenda = ComplexCueBuilder("Agenda")
    let catalyst = ComplexCueBuilder("Catalyst")
    let fallout = ComplexCueBuilder("Fallout")
    let catalyst_or_fallout = ComplexCueBuilder("Catalyst/Fallout")
    let domain = ComplexCueBuilder("Domain")
    let traitCue = ComplexCueBuilder("Trait")
    let deityCue = ComplexCueBuilder("The")
    
    let faction = CardType.Faction
    let figure = CardType.Figure
    let event = CardType.Event
    let location = CardType.Location
    let object = CardType.Object
    let creature = CardType.Creature
    let material = CardType.Material
    let deity = CardType.Deity
    let emblem = CardType.Emblem
    let modifier = CardType.Modifier
    
    let cues = CuesBuilder()
