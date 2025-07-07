namespace FunSharp.Common

open Microsoft.FSharp.Reflection

[<RequireQualifiedAccess>]
module Union =
   
    let toString (x:'a) =
        let case, _ = FSharpValue.GetUnionFields(x, typeof<'a>)
        case.Name
