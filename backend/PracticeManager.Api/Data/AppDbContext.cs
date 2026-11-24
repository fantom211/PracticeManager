using Microsoft.EntityFrameworkCore;
using PracticeManager.Api.Domain.Entities;

namespace PracticeManager.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<StudentGroup> StudentGroups => Set<StudentGroup>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<PracticeType> PracticeTypes => Set<PracticeType>();
    public DbSet<Practice> Practices => Set<Practice>();
    public DbSet<PracticeStage> PracticeStages => Set<PracticeStage>();
    public DbSet<PracticeParticipant> PracticeParticipants => Set<PracticeParticipant>();
    public DbSet<ParticipantStage> ParticipantStages => Set<ParticipantStage>();
    public DbSet<Domain.Entities.Task> Tasks => Set<Domain.Entities.Task>();
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<TeacherComment> TeacherComments => Set<TeacherComment>();
    public DbSet<DiaryEntry> DiaryEntries => Set<DiaryEntry>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Specialty>(e =>
        {
            e.ToTable("specialties");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(32).IsRequired();
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<StudentGroup>(e =>
        {
            e.ToTable("student_groups");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(64).IsRequired();
            e.HasOne(x => x.Specialty).WithMany(x => x.Groups).HasForeignKey(x => x.SpecialtyId);
        });

        modelBuilder.Entity<Student>(e =>
        {
            e.ToTable("students");
            e.HasKey(x => x.Id);
            e.Property(x => x.LastName).HasMaxLength(128).IsRequired();
            e.Property(x => x.FirstName).HasMaxLength(128).IsRequired();
            e.Property(x => x.MiddleName).HasMaxLength(128);
            e.Property(x => x.FullNameDative).HasMaxLength(256).IsRequired();
            e.Property(x => x.FullNameGenitive).HasMaxLength(256).IsRequired();
            e.Property(x => x.StudentNumber).HasMaxLength(64).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(32);
            e.Property(x => x.Email).HasMaxLength(256);
            e.HasOne(x => x.Group).WithMany(x => x.Students).HasForeignKey(x => x.GroupId);
            e.Ignore(x => x.FullName);
            e.Ignore(x => x.ShortName);
        });

        modelBuilder.Entity<Teacher>(e =>
        {
            e.ToTable("teachers");
            e.HasKey(x => x.Id);
            e.Property(x => x.LastName).HasMaxLength(128).IsRequired();
            e.Property(x => x.FirstName).HasMaxLength(128).IsRequired();
            e.Property(x => x.MiddleName).HasMaxLength(128);
            e.Property(x => x.Position).HasMaxLength(256);
            e.Property(x => x.Phone).HasMaxLength(32);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Ignore(x => x.FullName);
            e.Ignore(x => x.ShortName);
        });

        modelBuilder.Entity<Organization>(e =>
        {
            e.ToTable("organizations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(512).IsRequired();
            e.Property(x => x.Inn).HasMaxLength(32);
            e.Property(x => x.Ogrn).HasMaxLength(32);
            e.Property(x => x.LegalAddress).HasMaxLength(512);
            e.Property(x => x.ActualAddress).HasMaxLength(512);
            e.Property(x => x.Phone).HasMaxLength(32);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.ContactPerson).HasMaxLength(256);
        });

        modelBuilder.Entity<PracticeType>(e =>
        {
            e.ToTable("practice_types");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
        });

        modelBuilder.Entity<Practice>(e =>
        {
            e.ToTable("practices");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(512).IsRequired();
            e.Property(x => x.ProfessionalModule).HasMaxLength(512).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            e.HasOne(x => x.Group).WithMany(x => x.Practices).HasForeignKey(x => x.GroupId);
            e.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId);
            e.HasOne(x => x.PracticeType).WithMany().HasForeignKey(x => x.PracticeTypeId);
        });

        modelBuilder.Entity<PracticeStage>(e =>
        {
            e.ToTable("practice_stages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.HasOne(x => x.Practice).WithMany(x => x.Stages).HasForeignKey(x => x.PracticeId);
        });

        modelBuilder.Entity<PracticeParticipant>(e =>
        {
            e.ToTable("practice_participants");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.Result).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.ExternalSupervisorName).HasMaxLength(256);
            e.Property(x => x.ExternalSupervisorPosition).HasMaxLength(256);
            e.Property(x => x.ExternalSupervisorPhone).HasMaxLength(32);
            e.HasOne(x => x.Practice).WithMany(x => x.Participants).HasForeignKey(x => x.PracticeId);
            e.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
            e.HasOne(x => x.CurrentStage).WithMany().HasForeignKey(x => x.CurrentStageId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ParticipantStage>(e =>
        {
            e.ToTable("participant_stages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            e.HasOne(x => x.Participant).WithMany(x => x.StageProgress).HasForeignKey(x => x.ParticipantId);
            e.HasOne(x => x.Stage).WithMany().HasForeignKey(x => x.StageId);
        });

        modelBuilder.Entity<Domain.Entities.Task>(e =>
        {
            e.ToTable("tasks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(512).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            e.HasOne(x => x.Participant).WithMany(x => x.Tasks).HasForeignKey(x => x.ParticipantId);
        });

        modelBuilder.Entity<DocumentType>(e =>
        {
            e.ToTable("document_types");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(64).IsRequired();
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<DocumentTemplate>(e =>
        {
            e.ToTable("document_templates");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(128).IsRequired();
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.Property(x => x.FileName).HasMaxLength(256).IsRequired();
            e.Property(x => x.FilePath).HasMaxLength(512).IsRequired();
            e.Property(x => x.PlaceholdersJson).HasColumnType("jsonb");
            e.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
            e.HasOne(x => x.DocumentType).WithMany().HasForeignKey(x => x.DocumentTypeId);
            e.HasOne(x => x.PracticeType).WithMany().HasForeignKey(x => x.PracticeTypeId);
            e.HasIndex(x => new { x.Code, x.Version }).IsUnique();
        });

        modelBuilder.Entity<Document>(e =>
        {
            e.ToTable("documents");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.Content).HasColumnType("text");
            e.HasOne(x => x.Participant).WithMany(x => x.Documents).HasForeignKey(x => x.ParticipantId);
            e.HasOne(x => x.Template).WithMany().HasForeignKey(x => x.TemplateId);
            e.HasOne(x => x.DocumentType).WithMany().HasForeignKey(x => x.DocumentTypeId);
            e.HasOne(x => x.CurrentVersion).WithMany()
                .HasForeignKey(x => x.CurrentVersionId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        });

        modelBuilder.Entity<DocumentVersion>(e =>
        {
            e.ToTable("document_versions");
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).HasMaxLength(256).IsRequired();
            e.Property(x => x.FilePath).HasMaxLength(512).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
            e.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
            e.Property(x => x.Source).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.TeacherComment).HasColumnType("text");
            e.HasOne(x => x.Document).WithMany(x => x.Versions).HasForeignKey(x => x.DocumentId);
            e.HasIndex(x => new { x.DocumentId, x.VersionNumber }).IsUnique();
        });

        modelBuilder.Entity<TeacherComment>(e =>
        {
            e.ToTable("teacher_comments");
            e.HasKey(x => x.Id);
            e.Property(x => x.AuthorName).HasMaxLength(256).IsRequired();
            e.Property(x => x.AuthorRole).HasMaxLength(128).IsRequired();
            e.Property(x => x.Kind).HasMaxLength(32).IsRequired();
            e.Property(x => x.Summary).HasMaxLength(1024).IsRequired();
            e.Property(x => x.FullText).HasColumnType("text").IsRequired();
            e.HasOne(x => x.Document).WithMany(x => x.Comments).HasForeignKey(x => x.DocumentId);
            e.HasIndex(x => new { x.DocumentId, x.VersionNumber, x.CreatedAt }).IsUnique();
        });

        modelBuilder.Entity<DiaryEntry>(e =>
        {
            e.ToTable("diary_entries");
            e.HasKey(x => x.Id);
            e.Property(x => x.WorkDescription).HasColumnType("text").IsRequired();
            e.Property(x => x.SupervisorMark).HasMaxLength(256);
            e.HasOne(x => x.Participant).WithMany(x => x.DiaryEntries).HasForeignKey(x => x.ParticipantId);
        });

        modelBuilder.Entity<Assessment>(e =>
        {
            e.ToTable("assessments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Grade).HasMaxLength(32);
            e.Property(x => x.Result).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.Comment).HasColumnType("text");
            e.HasOne(x => x.Participant).WithOne(x => x.Assessment).HasForeignKey<Assessment>(x => x.ParticipantId);
            e.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId);
            e.HasIndex(x => x.ParticipantId).IsUnique();
        });

        modelBuilder.Entity<Notification>(e =>
        {
            e.ToTable("notifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(512).IsRequired();
            e.Property(x => x.Message).HasColumnType("text").IsRequired();
            e.Property(x => x.Link).HasMaxLength(512);
            e.HasOne(x => x.Student).WithMany(x => x.Notifications).HasForeignKey(x => x.StudentId);
        });
    }
}
