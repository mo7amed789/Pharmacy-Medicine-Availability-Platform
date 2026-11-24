namespace PharmacyAvailability.Domain.Common.Exceptions;

public class InsufficientCreditException : DomainException
{
    public InsufficientCreditException(string message) : base(message) { }
}

public class QueryLimitExceededException : DomainException
{
    public QueryLimitExceededException(string message) : base(message) { }
}
