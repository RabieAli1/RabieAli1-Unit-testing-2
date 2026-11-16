using Moq;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using VerifyXunit;
using Xunit;
namespace Simulation.Checkout.Tests;

public class ReceiptSnapshotTests
{

    // 4.1: Snapshot the receipt of Example C using fixed data only
    [Fact]
    public async Task CheckoutAsync_WhenProcessingExampleC_ShouldMatchReceiptSnapshot()
    {
        //Arrang
        var inventoryMock = new Mock<IInventoryService>();
        var orderRepoMock = new Mock<IOrderRepository>();
        var notificationMock = new Mock<INotificationService>();

        inventoryMock
            .Setup(x => x.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new CheckoutService
            (orderRepoMock.Object,
            inventoryMock.Object,
            notificationMock.Object);

        var order = new Order
        {
            CustomerName = "Omar Hassan",
            CustomerEmail = "omar.hassan@example.com",
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Mechanical Keyboard",
                    Quantity = 3,
                    UnitPrice = 500m
                }
            }

        };


        //Act
        var receipt= await sut.CheckoutAsync(order);

        //Assert
        await Verifier
            .Verify(receipt)
            .ScrubMembers("Orderid", "CreatedAt");
    }
}
