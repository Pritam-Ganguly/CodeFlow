namespace CodeFlow.Web.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public int StatusCode { get; set; }
    public string? OriginalPath { get; set; }

    public string Title => StatusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Access Denied",
        404 => "Page Not Found",
        500 => "Internal Server Error",
        503 => "Service Unavailable",
        _ => "An Error Occurred"
    };

    public string Description => StatusCode switch
    {
        400 => "The request was malformed or contained invalid parameters.",
        401 => "You need to be logged in to access this page.",
        403 => "You don't have permission to access this resource.",
        404 => "The page you're looking for doesn't exist or has been moved.",
        500 => "Something went wrong on our end. We're working to fix it.",
        503 => "The service is temporarily unavailable. Please try again later.",
        _ => "An unexpected error occurred while processing your request."
    };
}
