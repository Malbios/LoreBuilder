namespace FunSharp.Common

open System.Net
open System.Net.Http

type HttpErrorDetails = 
     | PlainBody of string
     | NoDetails

type HttpError = {
    Method: HttpMethod
    Url: string
    StatusCode: HttpStatusCode
    Details: HttpErrorDetails
}

exception HttpException of HttpError

[<RequireQualifiedAccess>]
module HttpError =
        
    let getMessage httpError =
        
        let formatDetails message = $"Details: '{message}'"
        
        let details = match httpError.Details with
                      | PlainBody str -> formatDetails str
                      | NoDetails -> String.empty
                      
        $"{httpError.Method.ToString().ToUpper()} {httpError.Url} failed with {httpError.StatusCode |> int}({httpError.StatusCode})! {details}"
