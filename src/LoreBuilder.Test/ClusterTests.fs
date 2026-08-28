namespace LoreBuilder.Test

open Xunit
open Faqt
open Faqt.Operators
open LoreBuilder.Model

[<Trait("Category", "Standard")>]
module ``Cluster Tests`` =

    type ``ClusterPosition fromIndex``() =

        [<Fact>]
        let ``maps 0 through 8 to the 9 positions in order`` () =
            // Arrange
            let expected = [
                ClusterPosition.Primary
                ClusterPosition.Inner_Bottom
                ClusterPosition.Inner_Left
                ClusterPosition.Inner_Top
                ClusterPosition.Inner_Right
                ClusterPosition.Outer_Bottom
                ClusterPosition.Outer_Left
                ClusterPosition.Outer_Top
                ClusterPosition.Outer_Right
            ]

            // Act
            let actual = [ 0..8 ] |> List.map ClusterPosition.fromIndex

            // Assert
            %actual.Should().Be(expected)

        [<Fact>]
        let ``index outside 0-8 throws`` () =
            // Act
            let act () = ClusterPosition.fromIndex 9 |> ignore

            // Assert
            %act.Should().Throw<exn, _>()

    type ``ClusterPosition toRotation``() =

        [<Fact>]
        let ``Primary and top positions have no rotation`` () =
            %(ClusterPosition.toRotation ClusterPosition.Primary).Should().Be("")
            %(ClusterPosition.toRotation ClusterPosition.Inner_Top).Should().Be("")
            %(ClusterPosition.toRotation ClusterPosition.Outer_Top).Should().Be("")

        [<Fact>]
        let ``right positions rotate 90 degrees`` () =
            %(ClusterPosition.toRotation ClusterPosition.Inner_Right).Should().Be("transform: rotate(90deg);")
            %(ClusterPosition.toRotation ClusterPosition.Outer_Right).Should().Be("transform: rotate(90deg);")

        [<Fact>]
        let ``bottom positions rotate 180 degrees`` () =
            %(ClusterPosition.toRotation ClusterPosition.Inner_Bottom).Should().Be("transform: rotate(180deg);")
            %(ClusterPosition.toRotation ClusterPosition.Outer_Bottom).Should().Be("transform: rotate(180deg);")

        [<Fact>]
        let ``left positions rotate 270 degrees`` () =
            %(ClusterPosition.toRotation ClusterPosition.Inner_Left).Should().Be("transform: rotate(270deg);")
            %(ClusterPosition.toRotation ClusterPosition.Outer_Left).Should().Be("transform: rotate(270deg);")

    type ``ClusterPosition toString``() =

        [<Fact>]
        let ``lowercases the case name`` () =
            %(ClusterPosition.toString ClusterPosition.Inner_Bottom).Should().Be("inner_bottom")
            %(ClusterPosition.toString ClusterPosition.Primary).Should().Be("primary")
