using FluentAssertions;
using Moq;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using Simulation.Checkout.Domain.Exceptions;
using Xunit;

namespace Simulation.Checkout.Tests;

public class CheckoutServiceTests
{
    private readonly Mock<IInventoryService> _inventoryMock;
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<INotificationService> _notificationMock;
    private readonly CheckoutService _sut;

    public CheckoutServiceTests()
    {
        _inventoryMock = new Mock<IInventoryService>();
        _orderRepoMock = new Mock<IOrderRepository>();
        _notificationMock = new Mock<INotificationService>();


        _inventoryMock
            .Setup(x => x.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _sut = new CheckoutService(
             _orderRepoMock.Object,
           _inventoryMock.Object,
            _notificationMock.Object);
    }



    //task 1.1

    [Fact]
    public async Task CheckoutAsync_WhenOrderHasNoItems_ShouldThrowEmptyOrderException()
    {
        // Arrange
        var order = new Order
        {
            CustomerName = "Rabie Ali",
            CustomerEmail = "Rabie@gmail.com",
            Items = new List<OrderItem>()
        };

        //Act
        Func<Task> act = async () => await _sut.CheckoutAsync(order);

        //Assert 
        await act.Should().ThrowAsync<EmptyOrderException>();


    }


    //task 1.2
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CheckoutAsync_InvalidQuantity_ShouldIncludeProductName(int quantity)
    {
        //Arrange
        string productName = "Keyboard Mechanical";
        var order = new Order
        {
            Items = new List<OrderItem>()
            {
                new OrderItem { ProductId = Guid.NewGuid() , ProductName = productName,Quantity=quantity,UnitPrice=100m}
            },
            CustomerName = "Rabie Ali",
            CustomerEmail = "Rabie@gmail.com"
        };

        //Act
        Func<Task> act = async () => await _sut.CheckoutAsync(order);

        //Assert
        var exception = await act.Should().ThrowAsync<InvalidQuantityException>();
        exception.Which.Message.Should().Contain(productName);

    }


    //task 1.3
    [Fact]
    public async Task CheckoutAsync_InvalidPrice_ShouldThrowException()
    {

        //Arrange
        var order = new Order
        {
            Items = new List<OrderItem>()
            {
                new OrderItem { ProductId = Guid.NewGuid() , ProductName = "Mouse",Quantity=1,UnitPrice=-0.01m}
            },
            CustomerName = "Rabie Ali",
            CustomerEmail = "Rabie@gmail.com"
        };

        //Act
        Func<Task> act = async () => await _sut.CheckoutAsync(order);

        //Assert 
        await act.Should().ThrowAsync<InvalidPriceException>();
    }


    //Task 1.4
    [Fact]
    public async Task CheckoutAsync_WhenItemPriceIsZero_ShouldSucceed()
    {
        //Arrange
        var order = new Order
        {
            Items = new List<OrderItem>()
            {
                new OrderItem { ProductId = Guid.NewGuid() , ProductName = "Mouse",Quantity=1,UnitPrice=0m}
            },
            CustomerName = "Rabie Ali",
            CustomerEmail = "Rabie@gmail.com"
        };


        //Act
        var receipt = await _sut.CheckoutAsync(order);

        //Assert
        receipt.Should().NotBeNull();
        receipt.Total.Should().Be(0m);
    }


    //Task 1.5
    public static TheoryData<decimal, decimal> DiscountTestData => new()
    {
        { 999.99m, 0m },
        { 1000m, 100m },
        { 1500m, 100m }
    };
    [Theory]
    [MemberData(nameof(DiscountTestData))]
    public async Task CheckoutAsync_WhenSubtotalReachesThresholds_ShouldApplyCorrectDiscount(decimal subtotal, decimal expectedDiscount)
    {
        //Arrange
        var order = new Order
        {
            Items = new List<OrderItem>()
            {
                new OrderItem { ProductId = Guid.NewGuid() , ProductName = "Mouse",Quantity=1,UnitPrice=subtotal}
            },
            CustomerName = "Rabie Ali",
            CustomerEmail = "Rabie@gmail.com"
        };

        //Act 
        var receipt = await _sut.CheckoutAsync(order);

        //Assert
        receipt.Discount.Should().Be(expectedDiscount);
    }



    //Task  1.6: Example A and C (Calculations test)
    [Fact]
    public async Task CheckoutAsync_WhenProcessingExampleA_ShouldCalculateCorrectTotals()
    {
        // Arrange: Example A: 2 x 150 + 1 x 400 = 700 (Discount: 0, Total: 700) 
        var order = new Order
        {
            Items = new List<OrderItem>()
            {
                new OrderItem {ProductId = Guid.NewGuid() , ProductName = "Item 1", Quantity = 2, UnitPrice = 150m },
                new OrderItem {ProductId = Guid.NewGuid() , ProductName = "Item 2", Quantity = 1, UnitPrice = 400m }
            },
            CustomerName = "Rabie Ali",
            CustomerEmail = "Rabie@gmail.com"
        };

        //Act
        var receipt = await _sut.CheckoutAsync(order);

        //Assert
        receipt.Subtotal.Should().Be(700m);
        receipt.Discount.Should().Be(0m);
        receipt.Total.Should().Be(700m);
    }



    [Fact]
    public async Task CheckoutAsync_WhenProcessingExampleC_ShouldCalculateCorrectTotalsWithCap()
    {
        // Arrange: Example C: 3 x 500 = 1500 (Discount: 100 capped, Total: 1400)
        var order = new Order
        {
            Items = new List<OrderItem>()
            {
                new OrderItem {ProductId = Guid.NewGuid() , ProductName = "Item ", Quantity = 3, UnitPrice = 500m }
            },
            CustomerName = "Rabie Ali",
            CustomerEmail = "Rabie@gmail.com"
        };

        //Act
        var receipt = await _sut.CheckoutAsync(order);


        //Assert
        receipt.Subtotal.Should().Be(1500m);
        receipt.Discount.Should().Be(100m);
        receipt.Total.Should().Be(1400m);
    }



}
