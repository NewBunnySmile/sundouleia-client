namespace Sundouleia.WebAPI;

/// <summary>
/// A small subclass used to create a Authentication failure exception with a reason for the http request
/// </summary>
public class SundouleiaAuthFailureException : Exception
{
    public SundouleiaAuthFailureException(string reason)
    {
        Reason = reason;
    }

    public string Reason { get; }
}
