namespace Domain.Entities;

public class OnlineStatus
{
    public string UserId { get; set; } = default!;
    public DateTime ConnectedDate { get; set; } = DateTime.UtcNow;
}