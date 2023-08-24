namespace Domain.Domain.Event;

public class OnlineStatusChangedEvent
{
    public string UserId { get; set; } = default!;
    public bool IsOnline { get; set; }
}