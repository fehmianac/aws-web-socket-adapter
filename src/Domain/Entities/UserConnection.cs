namespace Domain.Entities;

public class UserConnection
{
    public string UserId { get; set; } = default!;

    public List<ConnectionInfo> Connections { get; set; } = new();

    public class ConnectionInfo
    {
        public string Id { get; set; } = default!;
        public DateTime Time { get; set; }
    }
}