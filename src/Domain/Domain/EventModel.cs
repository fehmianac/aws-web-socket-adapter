namespace Domain.Domain;

public class EventModel<T> : EventModel
{
    public T Data { get; set; } = default!;
}

public class EventModel
{
    public string EventName { get; set; }= default!;
}