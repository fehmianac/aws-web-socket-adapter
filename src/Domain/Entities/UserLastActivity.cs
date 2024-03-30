using System.Text.Json.Serialization;
using Domain.Extensions;

namespace Domain.Entities;

public class UserLastActivity
{
    public string Id { get; set; } = default!;
    public DateTime Time { get; set; }
    [JsonPropertyName("ttl")] public long Ttl => Time.AddMonths(3).ToUnixTimeSeconds();
}