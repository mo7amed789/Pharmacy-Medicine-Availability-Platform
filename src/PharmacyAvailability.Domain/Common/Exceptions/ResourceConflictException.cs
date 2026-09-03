namespace PharmacyAvailability.Domain.Common.Exceptions;

public class ResourceConflictException : DomainException
{
    public ResourceConflictException(string message) : base(message)
    {
    }
}
