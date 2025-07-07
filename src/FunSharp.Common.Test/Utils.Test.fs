namespace FunSharp.Common.Test

open Xunit
open Faqt
open Faqt.Operators
open FunSharp.Common

[<Trait("Category", "Standard")>]
module ``Utils Tests`` =

    type tee() =
        
        [<Fact>]
        let ``calls f and then returns x`` () =
        
            // Arrange
            let mutable fWasCalled = false
            let f _ = fWasCalled <- true
            let x = 12
            
            // Act
            let result = Utils.tee f x
            
            // Assert
            %fWasCalled.Should().BeTrue()
            %result.Should().Be(x)
