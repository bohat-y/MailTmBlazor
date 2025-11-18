using System.Net;

namespace MailTmBlazor.Infrastructure.MailTm;

public sealed class MailTmApiException : Exception
{
    public MailTmApiException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
