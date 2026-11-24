namespace PracticeManager.Api.Contracts;

public record MeResponse(
    Guid Id,
    string FullName,
    string ShortName,
    string Group,
    string SpecialtyCode,
    string SpecialtyName,
    int Course,
    string StudentNumber,
    string? Phone,
    string? Email
);
