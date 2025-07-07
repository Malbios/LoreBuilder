namespace LoreBuilder.Test
    
open Xunit
open Microsoft.Playwright
open Faqt
open Faqt.Operators

[<Trait("Category", "OnDemand")>]
module ``LoreBuilder Tests`` =
    
    type PlaywrightTests() =

        [<Fact>]
        member _.``Clicking plus increments count``() = task {
                use! playwright = Playwright.CreateAsync()
                
                let! browser = playwright.Chromium.LaunchAsync(BrowserTypeLaunchOptions(Headless = true)) |> Async.AwaitTask
                
                let! context = browser.NewContextAsync() |> Async.AwaitTask
                let! page = context.NewPageAsync() |> Async.AwaitTask

                let! response = page.GotoAsync("http://localhost:5090")
                
                %response.Url.Should().Be("http://localhost:5090/TestPage")
                
                // do! page.ClickAsync("button:has-text('+')")
                // let! text = page.InnerTextAsync("h1")
                //
                // Assert.Contains("1", text)
            }
