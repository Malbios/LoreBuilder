namespace FunSharp.Common.Test

open Xunit
open Faqt
open Faqt.Operators
open FunSharp.Common

[<Trait("Category", "Standard")>]
module ``List Tests`` =

    type trySkip() =
        
        [<Fact>]
        let ``negative number throws exception`` () =
        
            // Arrange
            let list = [1]
            
            // Act
            let act () = list |> List.trySkip -1
            
            // Assert
            %act.Should().Throw<exn,_>().Whose.Message.Should().Contain("cannot be negative").And.Contain("-1")
        
        [<Fact>]
        let ``zero returns same list`` () =
        
            // Arrange
            let list = [1]
            
            // Act
            let result = list |> List.trySkip 0
            
            // Assert
            %result.Should().Be(list)
        
        [<Fact>]
        let ``empty list returns empty list`` () =
        
            // Arrange
            let list = List.empty
            
            // Act
            let result = list |> List.trySkip 1
            
            // Assert
            %result.Should().BeEmpty()
        
        [<Fact>]
        let ``list with less items than skipped returns empty list`` () =
        
            // Arrange
            let list = [1;2;3;4]
            
            // Act
            let result = list |> List.trySkip 5
            
            // Assert
            %result.Should().BeEmpty()
        
        [<Fact>]
        let ``list with exactly same amount of items as skipped returns empty list`` () =
        
            // Arrange
            let list = [1;2;3]
            
            // Act
            let result = list |> List.trySkip 3
            
            // Assert
            %result.Should().BeEmpty()
        
        [<Fact>]
        let ``list with more items than skipped returns list without skipped items`` () =
        
            // Arrange
            let list = [1;2;3;4;5;6]
            
            // Act
            let result = list |> List.trySkip 3
            
            // Assert
            %result.Should().Be([4;5;6])
