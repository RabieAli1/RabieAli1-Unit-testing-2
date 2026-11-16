# Testing Notes

## 1. Test Results

- **Total Tests:** 20 _`dotnet test`_
- **Passing Tests:** 20
- **Failing Tests:** 0

## 2. Code Coverage

`coverage-report/Summary.txt`.

- **Overall Line Coverage:** 90%
- **Domain Coverage:** 100%
- **Application Coverage:** 95.8%

## 3. Where Moq, AutoFixture, and Bogus Were Used

### Moq

Used in `CheckoutServiceTests` and `ReceiptSnapshotTests`.

It was used to create mock objects for:

- `IInventoryService`
- `IOrderRepository`
- `INotificationService`

These mocks helped simulate stock availability and check whether methods were called using `Times.Once` and `Times.Never`.

### AutoFixture

Used in `OrderTestDataHelper` to automatically create valid `Order` objects.

Some values were customized to make sure that:

- Item quantities are greater than 0.
- Unit prices are greater than or equal to 0.

### Bogus

Used in `OrderTestDataHelper` to generate realistic customer names and email addresses.

A fixed seed (`2026`) was used to make the generated data consistent across test runs.

## 4. Pricing Fairness: 999.99 EGP vs. 1,000 EGP

**No, the pricing is unfair.**

A customer who spends 999.99 EGP gets no discount and pays the full amount.

However, a customer who spends 1,000 EGP receives a 100 EGP discount and pays only 900 EGP.

This creates a large price difference between two customers whose spending differs by only 0.01 EGP. This is called a **cliff effect**.

## 5. Bugs Found

**None.**

No bugs were found during the code review.
