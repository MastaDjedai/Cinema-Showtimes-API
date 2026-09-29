namespace CinemaShowtimesApi.Errors;

public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public string Title { get; }
    public string ErrorCode { get; }

    protected AppException(int statusCode, string title, string errorCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Title = title;
        ErrorCode = errorCode;
    }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message, string errorCode = "not_found")
        : base(StatusCodes.Status404NotFound, "Not Found", errorCode, message)
    {
    }
}

public sealed class ConflictException : AppException
{
    public ConflictException(string message, string errorCode = "conflict")
        : base(StatusCodes.Status409Conflict, "Conflict", errorCode, message)
    {
    }
}

public sealed class BusinessRuleException : AppException
{
    public BusinessRuleException(string message, string errorCode = "business_rule_violation")
        : base(StatusCodes.Status422UnprocessableEntity, "Unprocessable Entity", errorCode, message)
    {
    }
}
