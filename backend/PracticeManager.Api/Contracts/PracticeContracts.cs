namespace PracticeManager.Api.Contracts;

public record PersonInfo(string ShortName, string FullName, string? Position);

public record OrganizationInfo(string Name, string? Address, string? Inn);

public record StageInfo(string Name, string Status, string StatusLabel);

public record DeadlineInfo(DateOnly Date, string Title, int DaysLeft);

public record AssessmentInfo(string? Grade, string Result, string ResultLabel, DateOnly? Date, string? Comment);

public record PracticeCurrentResponse(
    Guid ParticipantId,
    string PracticeName,
    string PracticeType,
    string ProfessionalModule,
    string SpecialtyCode,
    string SpecialtyName,
    DateOnly DateStart,
    DateOnly DateEnd,
    OrganizationInfo Organization,
    PersonInfo CollegeSupervisor,
    PersonInfo OrganizationSupervisor,
    string Status,
    string StatusLabel,
    int ProgressPercent,
    StageInfo? CurrentStage,
    List<StageInfo> Stages,
    AssessmentInfo? Assessment,
    DeadlineInfo? Deadline,
    int UnreadNotifications
);

public record PracticeHistoryItem(
    Guid ParticipantId,
    string PracticeName,
    string ProfessionalModule,
    DateOnly DateStart,
    DateOnly DateEnd,
    string Organization,
    string Status,
    string StatusLabel,
    string? Grade,
    string ResultLabel
);

public record PracticesResponse(PracticeCurrentResponse? Current, List<PracticeHistoryItem> History);

public record TaskItem(
    Guid Id,
    string Title,
    string? Description,
    DateOnly Deadline,
    string Status,
    string StatusLabel,
    string? TeacherComment,
    bool IsGroup
);

public record DiaryEntryDto(Guid Id, DateOnly Date, string WorkDescription, string? SupervisorMark);

public record UpdateDiaryRequest(List<UpdateDiaryEntry> Entries);

public record UpdateDiaryEntry(DateOnly Date, string WorkDescription, string? SupervisorMark);
