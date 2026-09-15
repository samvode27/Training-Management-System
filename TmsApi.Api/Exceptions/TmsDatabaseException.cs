namespace TmsApi.Api.Exceptions;

public class TmsDatabaseException : Exception
{
    public TmsDatabaseException() : base()
    {
    }

    public TmsDatabaseException(string message) : base(message)
    {
    }

    public TmsDatabaseException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}