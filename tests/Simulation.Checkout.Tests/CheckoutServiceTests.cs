using FluentAssertions;
using Moq;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using Simulation.Checkout.Domain.Exceptions;
using Simulation.Checkout.Tests.TestData;
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


    //Task 2.1
    [Fact]
    public async Task CheckoutAsync_WhenOrderIsValidAndInStock_ShouldSaveOrderAndSendConfirmationOnce()
    {
        //Arrange
        var order = new Order
        {
            CustomerName = "Ahmed Ali",
            CustomerEmail = "ahmed@example.com",
            Items = new List<OrderItem>
            {
                new OrderItem {ProductId = Guid.NewGuid() ,ProductName = "Laptop", Quantity = 1, UnitPrice = 500m },
                new OrderItem {ProductId = Guid.NewGuid() , ProductName = "Mouse", Quantity = 2, UnitPrice = 100m }
            }
        };
        //Act
        var receipt =await _sut.CheckoutAsync(order);

        //Assert
        receipt.Should().NotBeNull();
        _orderRepoMock.Verify(
            r => r.SaveAsync(order, It.IsAny<CancellationToken>()), 
            Times.Once);


        _notificationMock.Verify(
                    n => n.SendOrderConfirmationAsync(receipt, It.IsAny<CancellationToken>()),
                    Times.Once);


    }

    // 2.2: One item is out of stock -> Exception thrown; save and send Times.Never
    [Fact]
    public async Task CheckoutAsync_WhenOneItemIsOutOfStock_ShouldThrowInsufficientInventoryExceptionAndNotSaveOrSend()
    {
        //Arrange
        var outOfStockProductId = Guid.NewGuid();

        _inventoryMock
            .Setup(i => i.IsAvailableAsync(
            outOfStockProductId,
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);


        var order = new Order
        {
            CustomerName = "Ahmed Ali",
            CustomerEmail = "ahmed@example.com",
            Items = new List<OrderItem>
            {
                new OrderItem {ProductId = Guid.NewGuid() ,ProductName = "Laptop", Quantity = 1, UnitPrice = 500m },
                new OrderItem {ProductId = outOfStockProductId , ProductName = "Mouse", Quantity = 2, UnitPrice = 100m }
            }
        };

        //Act
        Func<Task> act = async () => await _sut.CheckoutAsync(order);

        //Assert
        await act.Should().ThrowAsync<InsufficientInventoryException>();

        _orderRepoMock.Verify(
            r=>r.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _notificationMock.Verify(
            n => n.SendOrderConfirmationAsync(It.IsAny<CheckoutReceipt>(), It.IsAny<CancellationToken>()),
            Times.Never);


    }

    // 2.3: Order fails validation (e.g. quantity 0) -> Inventory, save and send all Times.Never
    [Fact]
    public async Task CheckoutAsync_WhenOrderFailsValidation_ShouldNeverCallInventorySaveOrNotification()
    {
        //Arrang
        var order = new Order
        {
            CustomerName = "Ahmed Ali",
            CustomerEmail = "ahmed@example.com",
            Items = new List<OrderItem>
            {
                new OrderItem {ProductId = Guid.NewGuid() ,ProductName = "Laptop", Quantity = 0, UnitPrice = 500m },
            }
        };

        //Act 
        Func <Task> act = async()=> await _sut.CheckoutAsync(order);

        //Assert 
        await act.Should().ThrowAsync<InvalidQuantityException>();
        _inventoryMock.Verify(
            i => i.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _orderRepoMock.Verify(
            o=>o.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Never
            );

        _notificationMock.Verify(
            n => n.SendOrderConfirmationAsync(It.IsAny<CheckoutReceipt>(), It.IsAny<CancellationToken>()),
            Times.Never);

    }

    // 3.1: AutoFixture creates the order (Quantity > 0, Price >= 0) -> Checkout succeeds
    [Fact]
    public async Task CheckoutAsync_WhenOrderIsGeneratedByAutoFixtureWithValidConstraints_ShouldSucceed()

    {
        //Arrang
        var order = OrderTestDataHelper.CreateValidOrderWithAutoFixture();

        //Act
        var receipt =await _sut.CheckoutAsync(order);
        // Assert
        receipt.Should().NotBeNull();
        receipt.Total.Should().BeGreaterThanOrEqualTo(0m);
    }

    // 3.2: Bogus creates a realistic customer (name, email) with a fixed seed -> Receipt has same name & email
    [Fact]
    public async Task CheckoutAsync_WhenCustomerIsGeneratedByBogusWithFixedSeed_ShouldIncludeCustomerDetailsInReceipt()
    {
        //Arrange
        var (customerName, customerEmail) = OrderTestDataHelper.CreateRealisticCustomer(seed: 2026);
        var order = new Order
        {
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            Items = new List<OrderItem>
            {
                new OrderItem {ProductId=Guid.NewGuid(),  ProductName = "Monitor", Quantity = 1, UnitPrice = 300m }
            }
        };

        //Act
        var receipt = await _sut.CheckoutAsync(order);

        //Assert
        receipt.CustomerName.Should().Be(customerName);
        receipt.CustomerEmail.Should().Be(customerEmail);
    }
}
