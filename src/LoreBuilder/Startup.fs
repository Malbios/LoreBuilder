namespace LoreBuilder

open System
open System.Net.Http
open Microsoft.AspNetCore.Components.WebAssembly.Hosting
open Blazored.LocalStorage
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Plk.Blazor.DragDrop
open Radzen

module MyApp =

    [<EntryPoint>]
    let Main args =
        
        let builder = WebAssemblyHostBuilder.CreateDefault(args)
        
        builder.Logging.SetMinimumLevel(LogLevel.Trace) |> ignore
        
        let baseAddress = Uri builder.HostEnvironment.BaseAddress
        
        let services = builder.Services
        
        services
            .AddScoped<HttpClient>(fun _ -> new HttpClient(BaseAddress = baseAddress))
            .AddBlazoredLocalStorage()
            .AddRadzenComponents()
            .AddBlazorDragDrop()
        |> ignore

        builder.RootComponents.Add<Main.ClientApplication>("#main")

        builder.Build().RunAsync() |> ignore
        
        0
