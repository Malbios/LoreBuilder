namespace LoreBuilder

open System.Net.Http
open System.Text.Json
open System.Threading.Tasks
open FunSharp.Common
open LoreBuilder.Model
open Microsoft.Extensions.Logging

// Runtime-loaded replacement for the old Data/*.fs modules - one JSON array of Card per CardType,
// served as an ordinary static file under wwwroot/data/cards, so adding a card is just editing
// that file and refreshing, no rebuild needed. Kept dependency-light (HttpClient + a private
// mutable cache) rather than routed through Elmish's own State/Message loop, matching this app's
// existing precedent of most interactive state living outside Application.State (see CLAUDE.md).
module CardData =

    let private options =
        let o = JsonSerializerOptions()
        JsonSerializer.configure o
        o

    // Pure and unit-testable on its own - no HttpClient/DOM involved, just JSON in, Card list out.
    let parseCards (json: string) : Card list =
        System.Text.Json.JsonSerializer.Deserialize<Card list>(json, options)

    let private cache: Card list list option ref = ref None

    // Test-only seam for UtilsTests.fs - this codebase has no InternalsVisibleTo precedent, so a
    // small, clearly-labeled public function matches its existing style better than fighting
    // assembly visibility for one test.
    let seedForTests (cards: Card list list) : unit =
        cache.Value <- Some cards

    let private urlFor (cardType: CardType) =
        $"data/cards/{(Union.toString cardType).ToLowerInvariant()}.json"

    // Idempotent - safe to call from every page that needs card data (Pages/Home.fs and the
    // separate Pages/LoreClusterTest.fs dev harness both do) without double-fetching once one of
    // them has already loaded it.
    let loadAsync (http: HttpClient) (logger: ILogger) : Task =
        task {
            if cache.Value.IsNone then
                let types = Union.toList<CardType> () |> List.filter (fun t -> t <> CardType.Unknown)

                let! results =
                    types
                    |> List.map (fun cardType ->
                        task {
                            try
                                let! json = http.GetStringAsync(urlFor cardType)
                                return parseCards json
                            with ex ->
                                logger.LogWarning(ex, "Failed to load card data for {CardType}", cardType)
                                return []
                        })
                    |> Task.WhenAll

                cache.Value <- Some(List.ofArray results)
        }
        :> Task

    // Empty per-type sublists (a failed fetch, see loadAsync above) are dropped here rather than
    // left in - every existing consumer (Pages/Home.fs's and Pages/LoreClusterTest.fs's own
    // `List.head cards`, Utils.pickRandom) assumes every sublist it sees has at least one card,
    // which was always true when card data came from compiled Data/*.fs modules but no longer is
    // now that a card type's data can simply fail to load.
    let pool () : Card list list =
        cache.Value |> Option.defaultValue [] |> List.filter (List.isEmpty >> not)
