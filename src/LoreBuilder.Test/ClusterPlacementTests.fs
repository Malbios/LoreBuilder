namespace LoreBuilder.Test

open Xunit
open Faqt
open Faqt.Operators
open LoreBuilder.Model

[<Trait("Category", "Standard")>]
module ``ClusterPlacement Tests`` =

    let cellSize = 550.0
    let footprint = 470.0
    let source = (1000.0, 1000.0)

    type ``findExtractionSpot with free space``() =

        [<Fact>]
        let ``picks the first ring's first cardinal (below source), facing back with Inner_Top`` () =
            // Arrange
            let neverOverlaps _ _ = false

            // Act
            let result = ClusterPlacement.findExtractionSpot neverOverlaps cellSize footprint source

            // Assert
            let sx, sy = source
            %result.Should().Be(Some((sx, sy + cellSize), ClusterPosition.Inner_Top))

    type ``findExtractionSpot with the nearest cardinal occupied``() =

        [<Fact>]
        let ``falls back to the next cardinal offset`` () =
            // Arrange
            let sx, sy = source
            let below = (sx, sy + cellSize)
            let overlapsOnlyBelow _ (x, y) = (x, y) = below

            // Act
            let result = ClusterPlacement.findExtractionSpot overlapsOnlyBelow cellSize footprint source

            // Assert - the 2nd cardinal tried is directly above the source, facing back with Inner_Bottom
            %result.Should().Be(Some((sx, sy - cellSize), ClusterPosition.Inner_Bottom))

    type ``findExtractionSpot walled in on all 4 cardinal sides``() =

        [<Fact>]
        let ``falls back to a diagonal ring-1 cell with a real (non-arbitrary) direction`` () =
            // Arrange
            let sx, sy = source

            let cardinals =
                Set.ofList [ (sx, sy + cellSize); (sx, sy - cellSize); (sx - cellSize, sy); (sx + cellSize, sy) ]

            let overlapsOnlyCardinals _ candidate = cardinals.Contains candidate

            // Act
            let result = ClusterPlacement.findExtractionSpot overlapsOnlyCardinals cellSize footprint source

            // Assert - first diagonal perimeter cell tried at ring 1 is the top-left corner, an
            // exact tie between axes, so it falls to the documented Inner_Bottom tiebreak.
            %result.Should().Be(Some((sx - cellSize, sy - cellSize), ClusterPosition.Inner_Bottom))

    type ``findExtractionSpot with no free spot anywhere``() =

        [<Fact>]
        let ``returns None rather than looping forever`` () =
            // Arrange
            let alwaysOverlaps _ _ = true

            // Act
            let result = ClusterPlacement.findExtractionSpot alwaysOverlaps cellSize footprint source

            // Assert
            %result.Should().Be(None)
