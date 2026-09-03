namespace PharmacyAvailability.Domain.Common.Exceptions;

public class InvalidPriceException : DomainException
{
    public decimal Price { get; }

    public InvalidPriceException(decimal price)
        : base($"Price cannot be negative. Supplied price: {price}.")
    {
        Price = price;
    }
}
