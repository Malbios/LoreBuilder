namespace FunSharp.Common

open System.Collections.Generic

[<RequireQualifiedAccess>]
module Dictionary =
    
    let inline ofList list : Dictionary<'Key, 'Value> when 'Key: equality =
        
        let dict = Dictionary<'Key, 'Value>()
        for k, v in list do
            dict.Add(k, v)
        dict
