using AutoFixture;
using Bogus;
using Simulation.Checkout.Domain;

namespace Simulation.Checkout.Tests.TestData;

public static class OrderTestDataHelper
{
    // 3.1: AutoFixture helper with customized OrderItem rules (Quantity > 0, UnitPrice >= 0)
    public static Order CreateValidOrderWithAutoFixture()
    {
        var fixture = new Fixture();

        fixture.Customize<OrderItem>(composer => composer
            .With(item => item.Quantity, () => Random.Shared.Next(1, 10))       // Quantity > 0
            .With(item => item.UnitPrice, () => Random.Shared.Next(0, 500)));   // Price >= 0

        return fixture.Create<Order>();
    }

    // 3.2: Bogus helper with fixed seed for deterministic realistic customer data
    public static (string Name, string Email) CreateRealisticCustomer(int seed = 2026)
    {
        var faker = new Faker() { Random = new Randomizer(seed) };

        string name = faker.Name.FullName();
        string email = faker.Internet.Email();

        return (name, email);
    }
}