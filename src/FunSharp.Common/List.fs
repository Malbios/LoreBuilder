namespace FunSharp.Common

[<RequireQualifiedAccess>]
module List =
    
    let trySkip<'T> (amount: int)  (list: 'T list) =
        
        if amount < 0 then
            failwith $"{nameof amount} cannot be negative: {amount}"
            
        if list.Length < amount then
            List.empty<'T>
        else
            list |> List.skip amount
            
    let join separator items =
        
        items
        |> List.mapi (fun i x -> if i > 0 then [ separator; x ] else [x])
        |> List.concat
