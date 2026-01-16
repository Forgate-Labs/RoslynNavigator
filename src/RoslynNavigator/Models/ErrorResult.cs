namespace RoslynNavigator.Models;

public class ErrorResult
{
    public bool Success { get; set; } = false;
    public required ErrorInfo Error { get; set; }
}

public class ErrorInfo
{
    public required string Code { get; set; }
    public required string Message { get; set; }
}
