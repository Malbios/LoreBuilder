namespace LoreBuilder

open System
open System.Net.Http
open Microsoft.AspNetCore.Components.WebAssembly.Hosting
open Blazored.LocalStorage
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Radzen

module MyApp =

    [<EntryPoint>]
    let Main args =
        
        let builder = WebAssemblyHostBuilder.CreateDefault(args)
        
        builder.Logging.SetMinimumLevel(LogLevel.Trace) |> ignore
        
        builder.Services.AddScoped<HttpClient>(fun _ ->
            new HttpClient(BaseAddress = Uri builder.HostEnvironment.BaseAddress)
        ) |> ignore
        
        builder.Services.AddBlazoredLocalStorage() |> ignore
        builder.Services.AddRadzenComponents() |> ignore

        builder.RootComponents.Add<Main.ClientApplication>("#main")

        builder.Build().RunAsync() |> ignore
        
        0
