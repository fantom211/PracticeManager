namespace PracticeManager.Api.Contracts;

public record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    bool IsRead,
    string? Link,
    DateTimeOffset CreatedAt
);
