namespace LoreBuilder.Test

open System
open System.Collections.Generic
open Xunit
open Faqt
open Faqt.Operators
open LoreBuilder.Model

[<Trait("Category", "Standard")>]
module ``CanvasTree Tests`` =

    let private rootId = Guid.Empty

    let private addChild (canvases: Dictionary<Guid, CanvasState>) parentId parentClusterId parentPosition childId =
        let parent = canvases[parentId]
        parent.ChildCanvasOf[(parentClusterId, parentPosition)] <- childId

        canvases[childId] <-
            { CanvasState.createRoot childId with
                ParentLink = Some(parentId, parentClusterId, parentPosition) }

    type ``lockedPositionsFor``() =

        [<Fact>]
        let ``returns the positions on this cluster that have a live child canvas`` () =
            // Arrange
            let clusterId = Guid.NewGuid()
            let otherClusterId = Guid.NewGuid()
            let canvas = CanvasState.createRoot rootId
            canvas.ChildCanvasOf[(clusterId, ClusterPosition.Outer_Bottom)] <- Guid.NewGuid()
            canvas.ChildCanvasOf[(clusterId, ClusterPosition.Outer_Left)] <- Guid.NewGuid()
            canvas.ChildCanvasOf[(otherClusterId, ClusterPosition.Outer_Top)] <- Guid.NewGuid()

            // Act
            let actual = CanvasTree.lockedPositionsFor canvas clusterId

            // Assert
            %actual.Should().Be(Set.ofList [ ClusterPosition.Outer_Bottom; ClusterPosition.Outer_Left ])

        [<Fact>]
        let ``returns empty when nothing has been extracted from this cluster`` () =
            // Arrange
            let canvas = CanvasState.createRoot rootId

            // Act
            let actual = CanvasTree.lockedPositionsFor canvas (Guid.NewGuid())

            // Assert
            %actual.Should().Be(Set.empty)

    type ``breadcrumbTrail``() =

        [<Fact>]
        let ``returns just the root when that's the active canvas`` () =
            // Arrange
            let canvases = Dictionary<Guid, CanvasState>()
            canvases[rootId] <- CanvasState.createRoot rootId

            // Act
            let actual = CanvasTree.breadcrumbTrail canvases rootId

            // Assert
            %actual.Should().Be([ rootId ])

        [<Fact>]
        let ``walks ParentLink back to root, root-first`` () =
            // Arrange
            let canvases = Dictionary<Guid, CanvasState>()
            canvases[rootId] <- CanvasState.createRoot rootId
            let aId = Guid.NewGuid()
            let bId = Guid.NewGuid()
            addChild canvases rootId (Guid.NewGuid()) ClusterPosition.Outer_Bottom aId
            addChild canvases aId (Guid.NewGuid()) ClusterPosition.Outer_Top bId

            // Act
            let actual = CanvasTree.breadcrumbTrail canvases bId

            // Assert
            %actual.Should().Be([ rootId; aId; bId ])

        [<Fact>]
        let ``stops early if a canvas in the chain is missing`` () =
            // Arrange
            let canvases = Dictionary<Guid, CanvasState>()
            let missingId = Guid.NewGuid()

            // Act
            let actual = CanvasTree.breadcrumbTrail canvases missingId

            // Assert
            %actual.Should().Be([])

    type ``removeEmptySubCanvas``() =

        [<Fact>]
        let ``is a no-op for the root canvas`` () =
            // Arrange
            let canvases = Dictionary<Guid, CanvasState>()
            canvases[rootId] <- CanvasState.createRoot rootId

            // Act
            let actual = CanvasTree.removeEmptySubCanvas canvases rootId rootId

            // Assert
            %actual.Should().Be(None)
            %canvases.ContainsKey(rootId).Should().BeTrue()

        [<Fact>]
        let ``removes the sub-canvas and unlinks it from its parent's ChildCanvasOf`` () =
            // Arrange
            let canvases = Dictionary<Guid, CanvasState>()
            canvases[rootId] <- CanvasState.createRoot rootId
            let clusterId = Guid.NewGuid()
            let position = ClusterPosition.Outer_Bottom
            let childId = Guid.NewGuid()
            addChild canvases rootId clusterId position childId

            // Act
            let actual = CanvasTree.removeEmptySubCanvas canvases childId (Guid.NewGuid())

            // Assert
            %actual.Should().Be(None)
            %canvases.ContainsKey(childId).Should().BeFalse()
            %canvases[rootId].ChildCanvasOf.ContainsKey((clusterId, position)).Should().BeFalse()

        [<Fact>]
        let ``returns the parent id when the removed canvas was the active one`` () =
            // Arrange
            let canvases = Dictionary<Guid, CanvasState>()
            canvases[rootId] <- CanvasState.createRoot rootId
            let childId = Guid.NewGuid()
            addChild canvases rootId (Guid.NewGuid()) ClusterPosition.Outer_Bottom childId

            // Act
            let actual = CanvasTree.removeEmptySubCanvas canvases childId childId

            // Assert
            %actual.Should().Be(Some rootId)

        [<Fact>]
        let ``does nothing for an id that isn't in canvases`` () =
            // Arrange
            let canvases = Dictionary<Guid, CanvasState>()
            canvases[rootId] <- CanvasState.createRoot rootId

            // Act
            let actual = CanvasTree.removeEmptySubCanvas canvases (Guid.NewGuid()) rootId

            // Assert
            %actual.Should().Be(None)
            %canvases.Count.Should().Be(1)
