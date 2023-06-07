namespace Adapter.Models.Response;

public class OnlineStatusResponseModel
{
    public string UserId { get; set; } = "default!";
    public bool IsOnline { get; set; }
    public DateTime LastActivity { get; set; }
}