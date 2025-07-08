namespace FunSharp.Common

open Microsoft.FSharp.Reflection

[<RequireQualifiedAccess>]
module Union =
   
    let toString (x:'a) =
        let case, _ = FSharpValue.GetUnionFields(x, typeof<'a>)
        case.Name

    let toList<'T> () : 'T list =
        FSharpType.GetUnionCases(typeof<'T>)
        |> Array.map (fun case -> FSharpValue.MakeUnion(case, [||]) :?> 'T)
        |> Array.toList
