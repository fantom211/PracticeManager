using DocumentFormat.OpenXml.Wordprocessing;
using PracticeManager.Api.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace PracticeManager.Api.Domain.Entities;

public class Specialty
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    public List<StudentGroup> Groups { get; set; } = [];
}

public class StudentGroup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public Guid SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; }
    public int AdmissionYear { get; set; }
    public int GraduationYear { get; set; }

    public List<Student> Students { get; set; } = [];
    public List<Practice> Practices { get; set; } = [];
}

public class Student
{
    public Guid Id { get; set; }
    public string LastName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string? MiddleName { get; set; }
    public string FullNameDative { get; set; } = "";
    public string FullNameGenitive { get; set; } = "";
    public Guid GroupId { get; set; }
    public StudentGroup? Group { get; set; }
    public string StudentNumber { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public string FullName => string.Join(" ", new[] { LastName, FirstName, MiddleName }.Where(p => !string.IsNullOrWhiteSpace(p)));
    public string ShortName => $"{LastName} {FirstName.FirstOrDefault()}. {MiddleName?.FirstOrDefault()}.".Trim();

    public List<Notification> Notifications { get; set; } = [];
}

public class Teacher
{
    public Guid Id { get; set; }
    public string LastName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string? MiddleName { get; set; }
    public string? Position { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public string FullName => string.Join(" ", new[] { LastName, FirstName, MiddleName }.Where(p => !string.IsNullOrWhiteSpace(p)));
    public string ShortName => $"{LastName} {FirstName.FirstOrDefault()}. {MiddleName?.FirstOrDefault()}.".Trim();
}

public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Inn { get; set; }
    public string? Ogrn { get; set; }
    public string? LegalAddress { get; set; }
    public string? ActualAddress { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ContactPerson { get; set; }
}

public class PracticeType
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}

public class Practice
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public StudentGroup? Group { get; set; }
    public Guid TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
    public Guid PracticeTypeId { get; set; }
    public PracticeType? PracticeType { get; set; }
    public string Name { get; set; } = "";
    public string ProfessionalModule { get; set; } = "";
    public int Course { get; set; }
    public int Hours { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public ParticipantStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public List<PracticeStage> Stages { get; set; } = [];
    public List<PracticeParticipant> Participants { get; set; } = [];
}

public class PracticeStage
{
    public Guid Id { get; set; }
    public Guid PracticeId { get; set; }
    public Practice? Practice { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateOnly? PlannedStart { get; set; }
    public DateOnly? PlannedEnd { get; set; }
}

public class PracticeParticipant
{
    public Guid Id { get; set; }
    public Guid PracticeId { get; set; }
    public Practice? Practice { get; set; }
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public string? ExternalSupervisorName { get; set; }
    public string? ExternalSupervisorPosition { get; set; }
    public string? ExternalSupervisorPhone { get; set; }
    public ParticipantStatus Status { get; set; }
    public AssessmentResult Result { get; set; }
    public int ProgressPercent { get; set; }
    public Guid? CurrentStageId { get; set; }
    public PracticeStage? CurrentStage { get; set; }

    public List<ParticipantStage> StageProgress { get; set; } = [];
    public List<Domain.Entities.Task> Tasks { get; set; } = [];
    public List<Document> Documents { get; set; } = [];
    public List<DiaryEntry> DiaryEntries { get; set; } = [];
    public Assessment? Assessment { get; set; }
}

public class ParticipantStage
{
    public Guid Id { get; set; }
    public Guid ParticipantId { get; set; }
    public PracticeParticipant? Participant { get; set; }
    public Guid StageId { get; set; }
    public PracticeStage? Stage { get; set; }
    public StageStatus Status { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public class Task
{
    public Guid Id { get; set; }
    public Guid ParticipantId { get; set; }
    public PracticeParticipant? Participant { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateOnly Deadline { get; set; }
    public Enums.TaskStatus Status { get; set; }
    public string? TeacherComment { get; set; }
    public bool IsGroup { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class DocumentType
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}

public class DocumentTemplate
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public Guid DocumentTypeId { get; set; }
    public DocumentType? DocumentType { get; set; }
    public Guid? PracticeTypeId { get; set; }
    public PracticeType? PracticeType { get; set; }
    public string? ProfessionalModule { get; set; }
    public string Name { get; set; } = "";
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string PlaceholdersJson { get; set; } = "[]";
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public string Sha256 { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public class Document
{
    public Guid Id { get; set; }
    public Guid ParticipantId { get; set; }
    public PracticeParticipant? Participant { get; set; }
    public Guid? TemplateId { get; set; }
    public DocumentTemplate? Template { get; set; }
    public Guid DocumentTypeId { get; set; }
    public DocumentType? DocumentType { get; set; }
    public string Name { get; set; } = "";
    public DocumentStatus Status { get; set; }
    public string? Content { get; set; }
    public Guid? CurrentVersionId { get; set; }
    public DocumentVersion? CurrentVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public List<DocumentVersion> Versions { get; set; } = [];
    public List<TeacherComment> Comments { get; set; } = [];
}

public class DocumentVersion
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }
    public int VersionNumber { get; set; }
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = "";
    public DocumentSource Source { get; set; }
    public string? TeacherComment { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class TeacherComment
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int? VersionNumber { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorRole { get; set; } = "Преподаватель";
    public string Kind { get; set; } = "return"; // return | acceptance | clarification
    public string Summary { get; set; } = string.Empty;
    public string FullText { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public Document Document { get; set; } = null!;
}

public class DiaryEntry
{
    public Guid Id { get; set; }
    public Guid ParticipantId { get; set; }
    public PracticeParticipant? Participant { get; set; }
    public DateOnly Date { get; set; }
    public string WorkDescription { get; set; } = "";
    public string? SupervisorMark { get; set; }
    public int SortOrder { get; set; }
}

public class Assessment
{
    public Guid Id { get; set; }
    public Guid ParticipantId { get; set; }
    public PracticeParticipant? Participant { get; set; }
    public Guid? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
    public string? Grade { get; set; }
    public AssessmentResult Result { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset? AssessedAt { get; set; }
}

public class Notification
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public bool IsRead { get; set; }
    public string? Link { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
