namespace CRM.Automation.Core.Models;

/// <summary>
/// Chuẩn hóa định dạng phản hồi JSON tương thích 100% với CheckPoint Framework
/// </summary>
public class BOProcessResult
{
    public bool Ok { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public string? ErrorCode { get; set; }

    public static BOProcessResult Success(object? data = null, string message = "Success") 
        => new() { Ok = true, Data = data, Message = message };

    public static BOProcessResult Succeed(object? data = null, string message = "Success") 
        => Success(data, message);

    public static BOProcessResult Fail(string message, string? errorCode = null) 
        => new() { Ok = false, Message = message, ErrorCode = errorCode };

    public static BOProcessResult Failure(string errorCode, string message) 
        => new() { Ok = false, Message = message, ErrorCode = errorCode };
}
