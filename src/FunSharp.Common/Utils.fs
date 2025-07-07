namespace FunSharp.Common

module Utils =
    
    let tee f x =
        f x
        x
