namespace PracticeManager.Api.Services;

/// <summary>
/// There is no authentication in this prototype. Every request acts as this fixed demo student.
/// </summary>
public class CurrentStudentProvider(IConfiguration configuration)
{
    public Guid StudentId { get; } = Guid.Parse(configuration["Demo:StudentId"] ?? "00000000-0000-0000-0000-000000000001");
}
