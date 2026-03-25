namespace NoobGg.Domain.ValueObjects;

public class Availability
{
    public TimeSlot? Weekdays { get; set; }
    public TimeSlot? Weekends { get; set; }
}

public class TimeSlot
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
}
