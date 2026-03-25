namespace NoobGg.Application.Features.Subscriptions.DTOs;

public record PlanResponse(
    string Id,
    string Name,
    string Description,
    string Tier,
    decimal Price,
    string Currency,
    int IntervalMonths,
    List<string> Features,
    int MaxRoomsPerDay,
    int MaxGameProfiles,
    bool IsHighlighted,
    int SortOrder);
