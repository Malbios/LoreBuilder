namespace LoreBuilder.Model

open System.Collections.Generic
open FunSharp.Common

[<RequireQualifiedAccess>]
type ClusterPosition =
    | Primary
    | Inner_Bottom
    | Outer_Bottom
    | Inner_Left
    | Outer_Left
    | Inner_Top
    | Outer_Top
    | Inner_Right
    | Outer_Right
    
module ClusterPosition =
    
    let fromIndex index =
        
        match index with
        | 0 -> ClusterPosition.Primary
        | 1 -> ClusterPosition.Inner_Bottom
        | 2 -> ClusterPosition.Inner_Left
        | 3 -> ClusterPosition.Inner_Top
        | 4 -> ClusterPosition.Inner_Right
        | 5 -> ClusterPosition.Outer_Bottom
        | 6 -> ClusterPosition.Outer_Left
        | 7 -> ClusterPosition.Outer_Top
        | 8 -> ClusterPosition.Outer_Right
        | _ -> failwith $"unexpected index: {index}"
        
    let toRotation position =
        
        match position with
        | ClusterPosition.Primary
        | ClusterPosition.Inner_Top
        | ClusterPosition.Outer_Top ->
            ""
        | ClusterPosition.Inner_Right
        | ClusterPosition.Outer_Right ->
            "transform: rotate(90deg);"
        | ClusterPosition.Inner_Bottom
        | ClusterPosition.Outer_Bottom ->
            "transform: rotate(180deg);"
        | ClusterPosition.Inner_Left
        | ClusterPosition.Outer_Left ->
            "transform: rotate(270deg);"
            
    let toString position =

        (Union.toString position).ToLower()

// A cell in the growing grid of lore clusters on the Home page - unrelated to ClusterPosition,
// which is about the 9 slots inside a single cluster.
type GridPosition = {
    X: int
    Y: int
}

module GridPosition =

    let origin = { X = 0; Y = 0 }

    let left position = { position with X = position.X - 1 }
    let right position = { position with X = position.X + 1 }
    let up position = { position with Y = position.Y - 1 }
    let down position = { position with Y = position.Y + 1 }

    let neighbors position =
        [ left position; right position; up position; down position ]
