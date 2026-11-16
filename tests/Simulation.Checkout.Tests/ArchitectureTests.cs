using FluentAssertions;
using NetArchTest.Rules;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using Simulation.Checkout.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Simulation.Checkout.Tests
{
    public class ArchitectureTests
    {
        private const string DomainNamespace = "Simulation.Checkout.Domain";
        private const string ApplicationNamespace = "Simulation.Checkout.Application";
        private const string InfrastructureNamespace = "Simulation.Checkout.Infrastructure";

        // 5.1: Domain does not depend on Application or Infrastructure
        [Fact]
        public void Domain_ShouldNotDependOnApplicationOrInfrastructure()
        {
            //Arrange
            var domainAssembly = typeof(Order).Assembly;

            //Act
            var result =Types.InAssembly(domainAssembly)
                .ShouldNot()
                .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace)
                .GetResult();

            //Assert
            result.IsSuccessful.Should().BeTrue();

        }


        // 5.2: Application does not depend on Infrastructure
        [Fact]
        public void Application_ShouldNotDependOnInfrastructure()
        {
            // Arrange
            var applicationAssembly = typeof(CheckoutService).Assembly;

            
            //Act
            var result = Types.InAssembly(applicationAssembly)
                .ShouldNot()
                .HaveDependencyOn(InfrastructureNamespace)
                .GetResult();


            //Assert
            result.IsSuccessful.Should().BeTrue();


        }



        // 5.3: All interfaces start with I
        [Fact]
        public void Interfaces_ShouldStartWithI()
        {
            // Arrange
            var assemblies = new[]
            {
            typeof(Order).Assembly,
            typeof(CheckoutService).Assembly,
            typeof(InMemoryOrderRepository).Assembly
            };

            // Act
            var result = Types.InAssemblies(assemblies)
                .That()
                .AreInterfaces()
                .Should()
                .HaveNameStartingWith("I")
                .GetResult();

            // Assert
            result.IsSuccessful.Should().BeTrue();
        }


        // 5.4: Classes that implement IOrderRepository end with Repository
        [Fact]
        public void ClassesImplementingIOrderRepository_ShouldEndWithRepository()
        {
            //Arrange
            var assemblies = new[]
            {
                typeof(Order).Assembly,
                typeof(CheckoutService).Assembly,
                typeof(InMemoryOrderRepository).Assembly
            };

            //Act

            var result = Types.InAssemblies(assemblies)
                .That()
                .ImplementInterface (typeof(IOrderRepository))
                .And()
                .AreClasses()
                .Should()
                .HaveNameEndingWith("Repository")
                .GetResult();


            // Assert
            result.IsSuccessful.Should().BeTrue();
        }



    }
}
