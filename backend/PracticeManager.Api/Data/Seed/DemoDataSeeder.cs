using Microsoft.EntityFrameworkCore;
using PracticeManager.Api.Domain.Entities;
using PracticeManager.Api.Domain.Enums;
using PracticeManager.Api.Services;
using PracticeManager.Api.Services.DocumentGeneration;

namespace PracticeManager.Api.Data.Seed;

/// <summary>
/// Seeds the demo student and baseline practices once, then idempotently ensures the revision-1
/// rows (teacher comments and the additional ПМ.03 practice). Existing rows and their GUIDs are
/// never reset, deleted or recreated: every step looks entities up by business key first.
/// </summary>
public class DemoDataSeeder(AppDbContext db, IConfiguration configuration, FileStorage fileStorage, DocxTemplateRenderer renderer, ILogger<DemoDataSeeder> logger)
{
    private static readonly Guid StudentId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static readonly Guid RevisionPracticeId = Guid.Parse("00000000-0000-0000-0000-000000000020");
    private static readonly Guid RevisionParticipantId = Guid.Parse("00000000-0000-0000-0000-000000000020");
    private static readonly Guid RevisionOrganizationId = Guid.Parse("00000000-0000-0000-0000-000000000025");
    private static readonly Guid RevisionCollegeTeacherId = Guid.Parse("00000000-0000-0000-0000-000000000026");
    private static readonly Guid RevisionSupervisorTeacherId = Guid.Parse("00000000-0000-0000-0000-000000000027");
    private static readonly Guid RevisionAssessmentId = Guid.Parse("00000000-0000-0000-0000-000000000060");
    private static readonly Guid RevisionReportDocumentId = Guid.Parse("00000000-0000-0000-0000-000000000021");
    private static readonly Guid RevisionDiaryDocumentId = Guid.Parse("00000000-0000-0000-0000-000000000022");
    private static readonly Guid RevisionCharacteristicDocumentId = Guid.Parse("00000000-0000-0000-0000-000000000023");
    private static readonly Guid RevisionPlanDocumentId = Guid.Parse("00000000-0000-0000-0000-000000000024");

    public async System.Threading.Tasks.Task SeedAsync()
    {
        var demoStudentId = Guid.Parse(configuration["Demo:StudentId"] ?? StudentId.ToString());

        if (!await db.Students.AnyAsync(s => s.Id == demoStudentId))
        {
            logger.LogInformation("Seeding baseline demo data...");
            await SeedBaselineAsync(demoStudentId);
        }
        else
        {
            logger.LogInformation("Baseline demo data already present; ensuring revision rows.");
        }

        await EnsureRevisionDataAsync(demoStudentId);
    }

    private async System.Threading.Tasks.Task SeedBaselineAsync(Guid demoStudentId)
    {
        var specialty = new Specialty { Id = Guid.NewGuid(), Code = "09.02.07", Name = "Информационные системы и программирование" };
        var group = new StudentGroup { Id = Guid.NewGuid(), Name = "ИС-21", SpecialtyId = specialty.Id, AdmissionYear = 2024, GraduationYear = 2028 };

        var student = new Student
        {
            Id = demoStudentId,
            LastName = "Иванов",
            FirstName = "Максим",
            MiddleName = "Сергеевич",
            FullNameDative = "Иванову Максиму Сергеевичу",
            FullNameGenitive = "Иванова Максима Сергеевича",
            GroupId = group.Id,
            StudentNumber = "ИС-21-001",
            Phone = "+7 (900) 123-45-67",
            Email = "ivanov@example.com"
        };

        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            LastName = "Иванов",
            FirstName = "Петр",
            MiddleName = "Петрович",
            Position = "Преподаватель"
        };

        var teacherPast = new Teacher
        {
            Id = Guid.NewGuid(),
            LastName = "Сидорова",
            FirstName = "Анна",
            MiddleName = "Викторовна",
            Position = "Преподаватель"
        };

        var practiceTypeProd = new PracticeType { Id = Guid.NewGuid(), Name = "Производственная", Description = "Производственная практика" };

        var orgAlfa = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "ООО «Альфа»",
            Inn = "7701234567",
            LegalAddress = "г. Москва, ул. Примерная, д. 1",
            ActualAddress = "г. Москва, ул. Примерная, д. 1"
        };

        var orgBeta = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "ООО «Бета»",
            Inn = "7709876543",
            LegalAddress = "г. Москва, ул. Вторая, д. 5",
            ActualAddress = "г. Москва, ул. Вторая, д. 5"
        };

        var documentTypes = new List<DocumentType>
        {
            new() { Id = Guid.NewGuid(), Code = "report", Name = "Отчёт по практике" },
            new() { Id = Guid.NewGuid(), Code = "diary", Name = "Дневник практики" },
            new() { Id = Guid.NewGuid(), Code = "characteristic", Name = "Характеристика от организации" },
            new() { Id = Guid.NewGuid(), Code = "individual-plan", Name = "Индивидуальный план" },
        };

        // --- Current practice: ПМ.02, in progress ---
        var currentPractice = new Practice
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            TeacherId = teacher.Id,
            PracticeTypeId = practiceTypeProd.Id,
            Name = "Производственная практика (по профилю специальности)",
            ProfessionalModule = "ПМ.02 Осуществление интеграции программных модулей",
            Course = 3,
            Hours = 144,
            StartDate = new DateOnly(2027, 3, 1),
            EndDate = new DateOnly(2027, 3, 14),
            Status = ParticipantStatus.InProgress,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var stages = new List<PracticeStage>
        {
            new() { Id = Guid.NewGuid(), PracticeId = currentPractice.Id, Order = 1, Name = "Подготовительный этап" },
            new() { Id = Guid.NewGuid(), PracticeId = currentPractice.Id, Order = 2, Name = "Основной этап" },
            new() { Id = Guid.NewGuid(), PracticeId = currentPractice.Id, Order = 3, Name = "Заключительный этап" },
            new() { Id = Guid.NewGuid(), PracticeId = currentPractice.Id, Order = 4, Name = "Аттестация" },
        };
        currentPractice.Stages = stages;

        var currentParticipant = new PracticeParticipant
        {
            Id = Guid.NewGuid(),
            PracticeId = currentPractice.Id,
            StudentId = student.Id,
            OrganizationId = orgAlfa.Id,
            ExternalSupervisorName = "Петров Сергей Александрович",
            ExternalSupervisorPosition = "Руководитель отдела разработки",
            Status = ParticipantStatus.InProgress,
            Result = AssessmentResult.NotAssessed,
            ProgressPercent = 60,
            CurrentStageId = stages[1].Id
        };

        var stageProgress = new List<ParticipantStage>
        {
            new() { Id = Guid.NewGuid(), ParticipantId = currentParticipant.Id, StageId = stages[0].Id, Status = StageStatus.Completed, CompletedAt = DateTimeOffset.UtcNow.AddDays(-10) },
            new() { Id = Guid.NewGuid(), ParticipantId = currentParticipant.Id, StageId = stages[1].Id, Status = StageStatus.InProgress },
            new() { Id = Guid.NewGuid(), ParticipantId = currentParticipant.Id, StageId = stages[2].Id, Status = StageStatus.NotStarted },
            new() { Id = Guid.NewGuid(), ParticipantId = currentParticipant.Id, StageId = stages[3].Id, Status = StageStatus.NotStarted },
        };

        var tasks = new List<Domain.Entities.Task>
        {
            new()
            {
                Id = Guid.NewGuid(), ParticipantId = currentParticipant.Id,
                Title = "Изучить документацию по проекту", Deadline = new DateOnly(2027, 3, 5),
                Status = Domain.Enums.TaskStatus.Accepted, IsGroup = false, CreatedAt = DateTimeOffset.UtcNow.AddDays(-15)
            },
            new()
            {
                Id = Guid.NewGuid(), ParticipantId = currentParticipant.Id,
                Title = "Разработать модуль авторизации", Deadline = new DateOnly(2027, 3, 10),
                Status = Domain.Enums.TaskStatus.Submitted, IsGroup = false, CreatedAt = DateTimeOffset.UtcNow.AddDays(-15)
            },
            new()
            {
                Id = Guid.NewGuid(), ParticipantId = currentParticipant.Id,
                Title = "Подготовить отчёт по практике", Deadline = new DateOnly(2027, 3, 13),
                Status = Domain.Enums.TaskStatus.InProgress, IsGroup = false, CreatedAt = DateTimeOffset.UtcNow.AddDays(-15)
            },
        };

        var diaryEntries = new List<DiaryEntry>
        {
            Entry(1, "2027-03-03", "Изучение документации по проекту, настройка среды разработки"),
            Entry(2, "2027-03-04", "Разработка модуля авторизации"),
            Entry(3, "2027-03-05", "Написание тестов и проверка API"),
            Entry(4, "2027-03-06", "Подготовка раздела отчёта о выполненных работах"),
            Entry(5, "2027-03-09", "Исправление замечаний руководителя"),
            Entry(6, "2027-03-10", "Оформление дневника практики"),
        };
        DiaryEntry Entry(int order, string date, string work) => new()
        {
            Id = Guid.NewGuid(),
            ParticipantId = currentParticipant.Id,
            Date = DateOnly.Parse(date),
            WorkDescription = work,
            SupervisorMark = "выполнено",
            SortOrder = order
        };

        const string reportContent =
            "В ходе производственной практики по модулю ПМ.02 я участвовал в разработке и интеграции программных " +
            "модулей информационной системы учёта практик. Были изучены требования к взаимодействию модулей, " +
            "разработан модуль авторизации пользователей, выполнено тестирование API и подготовлена техническая документация.\n\n" +
            "Также были выполнены задачи по настройке среды разработки, написанию unit-тестов и устранению замечаний " +
            "руководителя практики. Работа велась с использованием современных инструментов разработки и систем контроля версий.";

        // --- Past practice: ПМ.01, completed ---
        var pastPractice = new Practice
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            TeacherId = teacherPast.Id,
            PracticeTypeId = practiceTypeProd.Id,
            Name = "Производственная практика (по профилю специальности)",
            ProfessionalModule = "ПМ.01 Разработка модулей программного обеспечения для компьютерных систем",
            Course = 3,
            Hours = 144,
            StartDate = new DateOnly(2026, 10, 1),
            EndDate = new DateOnly(2026, 10, 14),
            Status = ParticipantStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-5)
        };

        var pastParticipant = new PracticeParticipant
        {
            Id = Guid.NewGuid(),
            PracticeId = pastPractice.Id,
            StudentId = student.Id,
            OrganizationId = orgBeta.Id,
            ExternalSupervisorName = "Кузнецова Ольга Игоревна",
            ExternalSupervisorPosition = "Руководитель практики от организации",
            Status = ParticipantStatus.Completed,
            Result = AssessmentResult.Passed,
            ProgressPercent = 100
        };

        var pastAssessment = new Assessment
        {
            Id = Guid.NewGuid(),
            ParticipantId = pastParticipant.Id,
            TeacherId = teacherPast.Id,
            Grade = "5 (отлично)",
            Result = AssessmentResult.Passed,
            Comment = "Практика пройдена в полном объёме, замечаний нет.",
            AssessedAt = SeedTime("2026-10-14T15:00:00+03:00")
        };

        // --- Documents for the current practice ---
        var reportType = documentTypes.First(t => t.Code == "report");
        var diaryType = documentTypes.First(t => t.Code == "diary");
        var characteristicType = documentTypes.First(t => t.Code == "characteristic");
        var planType = documentTypes.First(t => t.Code == "individual-plan");

        var reportDocument = new Document
        {
            Id = Guid.NewGuid(), ParticipantId = currentParticipant.Id, DocumentTypeId = reportType.Id,
            Name = "Отчёт по практике", Status = DocumentStatus.Submitted, Content = reportContent,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-20), UpdatedAt = SeedTime("2027-03-12T14:32:00+03:00")
        };

        var diaryDocument = new Document
        {
            Id = Guid.NewGuid(), ParticipantId = currentParticipant.Id, DocumentTypeId = diaryType.Id,
            Name = "Дневник практики", Status = DocumentStatus.Accepted,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-20), UpdatedAt = SeedTime("2027-03-10T16:20:00+03:00")
        };

        const string characteristicText =
            "Студент Иванов М. С. за время прохождения практики в ООО «Альфа» проявил себя как ответственный и " +
            "технически грамотный специалист. Порученные задачи выполнял в установленные сроки, демонстрировал " +
            "самостоятельность при решении технических вопросов и хорошее владение современным стеком разработки.";

        var characteristicDocument = new Document
        {
            Id = Guid.NewGuid(), ParticipantId = currentParticipant.Id, DocumentTypeId = characteristicType.Id,
            Name = "Характеристика от организации", Status = DocumentStatus.Returned, Content = characteristicText,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-20), UpdatedAt = SeedTime("2027-03-09T12:15:00+03:00")
        };

        var planDocument = new Document
        {
            Id = Guid.NewGuid(), ParticipantId = currentParticipant.Id, DocumentTypeId = planType.Id,
            Name = "Индивидуальный план", Status = DocumentStatus.NotUploaded,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-20), UpdatedAt = DateTimeOffset.UtcNow.AddDays(-20)
        };

        var notifications = new List<Notification>
        {
            new()
            {
                Id = Guid.NewGuid(), StudentId = student.Id,
                Title = "Документ «Характеристика от организации» возвращён на доработку",
                Message = "Необходимо добавить информацию о задачах.", IsRead = false,
                Link = $"/documents/{characteristicDocument.Id}",
                CreatedAt = SeedTime("2027-03-09T12:20:00+03:00")
            },
            new()
            {
                Id = Guid.NewGuid(), StudentId = student.Id,
                Title = "Практика началась. Удачной работы!",
                Message = "Производственная практика (по профилю специальности) началась 01.03.2027.", IsRead = false,
                CreatedAt = SeedTime("2027-03-01T09:00:00+03:00")
            },
            new()
            {
                Id = Guid.NewGuid(), StudentId = student.Id,
                Title = "Документ «Дневник практики» принят",
                Message = "Руководитель практики принял дневник практики без замечаний.", IsRead = true,
                Link = $"/documents/{diaryDocument.Id}",
                CreatedAt = SeedTime("2027-03-10T16:25:00+03:00")
            },
        };

        db.AddRange(specialty, group, student, teacher, teacherPast, practiceTypeProd, orgAlfa, orgBeta);
        db.AddRange(documentTypes);
        db.AddRange(currentPractice, pastPractice);
        db.AddRange(stages);
        db.Add(currentParticipant);
        db.AddRange(stageProgress);
        db.AddRange(tasks);
        db.AddRange(diaryEntries);
        db.Add(pastParticipant);
        db.Add(pastAssessment);
        db.AddRange(reportDocument, diaryDocument, characteristicDocument, planDocument);
        db.AddRange(notifications);

        await db.SaveChangesAsync();

        var typeIds = documentTypes.ToDictionary(t => t.Code, t => t.Id);
        await EnsureTemplatesAsync(typeIds);
        await db.SaveChangesAsync();

        await GenerateSeedVersionAsync(reportDocument, 1, null, null, null, reportDocument.UpdatedAt);
        await GenerateSeedVersionAsync(diaryDocument, 1, null, null, null, diaryDocument.UpdatedAt);
        await GenerateSeedVersionAsync(characteristicDocument, 1, null, null, "Необходимо добавить информацию о задачах", characteristicDocument.UpdatedAt);

        await db.SaveChangesAsync();
        logger.LogInformation("Baseline demo data seeded successfully.");
    }

    private async System.Threading.Tasks.Task EnsureRevisionDataAsync(Guid studentId)
    {
        var student = await db.Students.FirstOrDefaultAsync(s => s.Id == studentId);
        if (student == null)
        {
            logger.LogWarning("Demo student {StudentId} not found; revision seed skipped.", studentId);
            return;
        }

        var typeIds = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in new[] { "report", "diary", "characteristic", "individual-plan" })
        {
            typeIds[code] = await EnsureDocumentTypeAsync(code);
        }

        await EnsureTemplatesAsync(typeIds);
        await EnsureRevisionPracticeAsync(student, typeIds);
        await EnsureTeacherCommentsAsync(studentId);
        await SyncLegacyTeacherCommentsAsync();
        await db.SaveChangesAsync();
    }

    private async System.Threading.Tasks.Task<Guid> EnsureDocumentTypeAsync(string code)
    {
        var existing = await db.DocumentTypes.FirstOrDefaultAsync(t => t.Code == code);
        if (existing != null)
        {
            return existing.Id;
        }

        var type = new DocumentType
        {
            Id = Guid.Parse($"00000000-0000-0000-0000-6000000000{code.Length:00}"),
            Code = code,
            Name = code switch
            {
                "report" => "Отчёт по практике",
                "diary" => "Дневник практики",
                "characteristic" => "Характеристика от организации",
                _ => "Индивидуальный план"
            }
        };
        db.DocumentTypes.Add(type);
        await db.SaveChangesAsync();
        return type.Id;
    }

    private async System.Threading.Tasks.Task EnsureTemplatesAsync(IReadOnlyDictionary<string, Guid> typeIds)
    {
        var defs = new[]
        {
            ("report-pm01", "Отчёт по практике (ПМ.01)", "report", "ПМ.01 Разработка модулей программного обеспечения для компьютерных систем", 1),
            ("report-pm02", "Отчёт по практике (ПМ.02)", "report", "ПМ.02 Осуществление интеграции программных модулей", 2),
            ("report-pm03", "Отчёт по практике (ПМ.03)", "report", "ПМ.03 Ревьюирование программных продуктов", 3),
            ("diary", "Дневник практики", "diary", null, 4),
            ("characteristic", "Характеристика от организации", "characteristic", null, 5),
            ("individual-plan", "Индивидуальный план", "individual-plan", null, 6),
        };

        foreach (var (code, name, typeCode, module, index) in defs)
        {
            if (await db.DocumentTemplates.AnyAsync(t => t.Code == code))
            {
                continue;
            }

            var docxPath = Path.Combine(fileStorage.TemplatesRoot, $"{code}.docx");
            var manifestPath = Path.Combine(fileStorage.TemplatesRoot, $"{code}.json");
            if (!File.Exists(docxPath) || !File.Exists(manifestPath))
            {
                logger.LogWarning("Template files for {Code} not found under {Root}; skipping template registration.", code, fileStorage.TemplatesRoot);
                continue;
            }

            var manifestJson = await File.ReadAllTextAsync(manifestPath);
            var manifest = System.Text.Json.JsonSerializer.Deserialize<TemplateManifest>(manifestJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            db.DocumentTemplates.Add(new DocumentTemplate
            {
                Id = Guid.Parse($"00000000-0000-0000-0000-5000000000{index:00}"),
                Code = code,
                DocumentTypeId = typeIds[typeCode],
                ProfessionalModule = module,
                Name = name,
                FileName = $"{code}.docx",
                FilePath = $"{code}.docx",
                PlaceholdersJson = System.Text.Json.JsonSerializer.Serialize(manifest?.Placeholders ?? []),
                Version = manifest?.Version ?? 1,
                IsActive = true,
                Sha256 = manifest?.Sha256 ?? "",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
    }

    private async System.Threading.Tasks.Task EnsureTeacherCommentsAsync(Guid studentId)
    {
        var originalParticipant = await db.PracticeParticipants
            .FirstOrDefaultAsync(p => p.StudentId == studentId && p.Practice!.ProfessionalModule.StartsWith("ПМ.02"));
        if (originalParticipant != null)
        {
            var characteristic = await FindDocumentByTypeAsync(originalParticipant.Id, "characteristic");
            if (characteristic != null)
            {
                await EnsureCommentAsync(
                    Guid.Parse("00000000-0000-0000-0000-400000000001"), characteristic.Id, 1, "return",
                    "Необходимо добавить информацию о задачах", OriginalCharacteristicReturn,
                    SeedTime("2027-03-09T12:18:00+03:00"), "Иванов Петр Петрович");
            }

            var diary = await FindDocumentByTypeAsync(originalParticipant.Id, "diary");
            if (diary != null)
            {
                await EnsureCommentAsync(
                    Guid.Parse("00000000-0000-0000-0000-400000000002"), diary.Id, 1, "acceptance",
                    "Дневник принят, замечаний нет", OriginalDiaryAcceptance,
                    SeedTime("2027-03-10T16:22:00+03:00"), "Иванов Петр Петрович");
            }

            var report = await FindDocumentByTypeAsync(originalParticipant.Id, "report");
            if (report != null)
            {
                await EnsureCommentAsync(
                    Guid.Parse("00000000-0000-0000-0000-400000000003"), report.Id, 1, "clarification",
                    "После проверки обратите внимание на оформление рисунков", OriginalReportClarification,
                    SeedTime("2027-03-12T14:40:00+03:00"), "Иванов Петр Петрович");
            }
        }

        var revisionParticipant = await db.PracticeParticipants.FirstOrDefaultAsync(p => p.Id == RevisionParticipantId);
        if (revisionParticipant != null)
        {
            var diary = await FindDocumentByTypeAsync(revisionParticipant.Id, "diary");
            if (diary != null)
            {
                await EnsureCommentAsync(
                    Guid.Parse("00000000-0000-0000-0000-400000000004"), diary.Id, 2, "return",
                    "Дополните дневник записями за 11–12 апреля", RevisionDiaryReturn,
                    SeedTime("2027-04-12T11:42:00+03:00"), "Петрова Мария Ильинична");
            }

            var report = await FindDocumentByTypeAsync(revisionParticipant.Id, "report");
            if (report != null)
            {
                await EnsureCommentAsync(
                    Guid.Parse("00000000-0000-0000-0000-400000000005"), report.Id, 1, "acceptance",
                    "Отчёт принят, замечаний нет", RevisionReportAcceptance,
                    SeedTime("2027-04-14T15:06:00+03:00"), "Петрова Мария Ильинична");
            }
        }

        await db.SaveChangesAsync();
    }

    private async System.Threading.Tasks.Task EnsureCommentAsync(Guid id, Guid documentId, int versionNumber, string kind,
        string summary, string fullText, DateTimeOffset createdAt, string authorName)
    {
        if (await db.TeacherComments.AnyAsync(c => c.Id == id)
            || await db.TeacherComments.AnyAsync(c => c.DocumentId == documentId && c.VersionNumber == versionNumber && c.CreatedAt == createdAt))
        {
            return;
        }

        db.TeacherComments.Add(new TeacherComment
        {
            Id = id,
            DocumentId = documentId,
            VersionNumber = versionNumber,
            AuthorName = authorName,
            AuthorRole = "Преподаватель",
            Kind = kind,
            Summary = summary,
            FullText = fullText,
            CreatedAt = createdAt
        });
    }

    private async System.Threading.Tasks.Task SyncLegacyTeacherCommentsAsync()
    {
        var grouped = (await db.TeacherComments.ToListAsync())
            .GroupBy(c => c.DocumentId);
        foreach (var group in grouped)
        {
            var newest = group.OrderByDescending(c => c.CreatedAt).First();
            var version = await db.DocumentVersions
                .Where(v => v.DocumentId == group.Key)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync();
            if (version != null && version.TeacherComment != newest.Summary)
            {
                version.TeacherComment = newest.Summary;
            }
        }

        // The accepted ПМ.03 characteristic has no separate comment row; keep its legacy summary.
        var characteristic = await db.Documents.FirstOrDefaultAsync(d => d.Id == RevisionCharacteristicDocumentId);
        if (characteristic != null)
        {
            var version = await db.DocumentVersions
                .Where(v => v.DocumentId == characteristic.Id)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync();
            if (version != null && string.IsNullOrWhiteSpace(version.TeacherComment))
            {
                version.TeacherComment = "Характеристика принята";
            }
        }
    }

    private async System.Threading.Tasks.Task EnsureRevisionPracticeAsync(Student student, IReadOnlyDictionary<string, Guid> typeIds)
    {
        var practiceType = await db.PracticeTypes.FirstOrDefaultAsync(p => p.Name == "Производственная");
        if (practiceType == null)
        {
            practiceType = new PracticeType
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000090"),
                Name = "Производственная",
                Description = "Производственная практика"
            };
            db.PracticeTypes.Add(practiceType);
            await db.SaveChangesAsync();
        }

        var collegeTeacher = await EnsureTeacherAsync("Петрова", "Мария", "Ильинична", "Преподаватель", RevisionCollegeTeacherId);
        await EnsureTeacherAsync("Сидорова", "Анна", "Викторовна", "Руководитель проектов", RevisionSupervisorTeacherId);

        var organization = await db.Organizations.FirstOrDefaultAsync(o => o.Name == "ООО «Гамма»");
        if (organization == null)
        {
            organization = new Organization
            {
                Id = RevisionOrganizationId,
                Name = "ООО «Гамма»",
                Inn = "7801234567",
                LegalAddress = "г. Санкт-Петербург, ул. Тестовая, д. 7",
                ActualAddress = "г. Санкт-Петербург, ул. Тестовая, д. 7"
            };
            db.Organizations.Add(organization);
            await db.SaveChangesAsync();
        }

        var practice = await db.Practices.FirstOrDefaultAsync(p => p.Id == RevisionPracticeId);
        if (practice == null)
        {
            practice = new Practice
            {
                Id = RevisionPracticeId,
                GroupId = student.GroupId,
                TeacherId = collegeTeacher.Id,
                PracticeTypeId = practiceType.Id,
                Name = "Производственная практика (преддипломная)",
                ProfessionalModule = "ПМ.03 Ревьюирование программных продуктов",
                Course = 3,
                Hours = 144,
                StartDate = new DateOnly(2027, 4, 1),
                EndDate = new DateOnly(2027, 4, 14),
                Status = ParticipantStatus.Completed,
                CreatedAt = SeedTime("2027-03-30T09:00:00+03:00")
            };
            db.Practices.Add(practice);
            await db.SaveChangesAsync();
        }

        var stageDefs = new[]
        {
            (1, "Подготовительный этап", "2027-04-02T17:00:00+03:00"),
            (2, "Основной этап", "2027-04-10T17:00:00+03:00"),
            (3, "Заключительный этап", "2027-04-12T17:00:00+03:00"),
            (4, "Аттестация", "2027-04-14T15:00:00+03:00"),
        };
        var stages = new List<PracticeStage>();
        foreach (var (order, name, _) in stageDefs)
        {
            var stage = await db.PracticeStages.FirstOrDefaultAsync(s => s.PracticeId == practice.Id && s.Order == order);
            if (stage == null)
            {
                stage = new PracticeStage
                {
                    Id = Guid.Parse($"00000000-0000-0000-0000-00000000003{order}"),
                    PracticeId = practice.Id,
                    Order = order,
                    Name = name
                };
                db.PracticeStages.Add(stage);
            }
            stages.Add(stage);
        }
        await db.SaveChangesAsync();

        var participant = await db.PracticeParticipants.FirstOrDefaultAsync(p => p.Id == RevisionParticipantId);
        if (participant == null)
        {
            participant = new PracticeParticipant
            {
                Id = RevisionParticipantId,
                PracticeId = practice.Id,
                StudentId = student.Id,
                OrganizationId = organization.Id,
                ExternalSupervisorName = "Сидорова Анна Викторовна",
                ExternalSupervisorPosition = "Руководитель проектов",
                Status = ParticipantStatus.Completed,
                Result = AssessmentResult.Passed,
                ProgressPercent = 100,
                CurrentStageId = stages[^1].Id
            };
            db.PracticeParticipants.Add(participant);
            await db.SaveChangesAsync();
        }

        foreach (var (order, _, completedAt) in stageDefs)
        {
            var stage = stages.First(s => s.Order == order);
            var progress = await db.ParticipantStages.FirstOrDefaultAsync(ps => ps.ParticipantId == participant.Id && ps.StageId == stage.Id);
            if (progress == null)
            {
                db.ParticipantStages.Add(new ParticipantStage
                {
                    Id = Guid.Parse($"00000000-0000-0000-0000-00000000004{order}"),
                    ParticipantId = participant.Id,
                    StageId = stage.Id,
                    Status = StageStatus.Completed,
                    CompletedAt = SeedTime(completedAt)
                });
            }
        }

        if (!await db.Assessments.AnyAsync(a => a.ParticipantId == participant.Id))
        {
            db.Assessments.Add(new Assessment
            {
                Id = RevisionAssessmentId,
                ParticipantId = participant.Id,
                TeacherId = collegeTeacher.Id,
                Grade = "4 (хорошо)",
                Result = AssessmentResult.Passed,
                Comment = "Практика пройдена в полном объёме, задания выполнены в срок.",
                AssessedAt = SeedTime("2027-04-14T15:05:00+03:00")
            });
        }

        var taskDefs = new[]
        {
            ("Собрать тестовые данные для отчёта", new DateOnly(2027, 4, 5), Domain.Enums.TaskStatus.Accepted),
            ("Проверить генерацию DOCX по шаблону ПМ.03", new DateOnly(2027, 4, 10), Domain.Enums.TaskStatus.Accepted),
            ("Подготовить материалы к защите", new DateOnly(2027, 4, 13), Domain.Enums.TaskStatus.Submitted),
        };
        for (var i = 0; i < taskDefs.Length; i++)
        {
            var (title, deadline, status) = taskDefs[i];
            if (await db.Tasks.AnyAsync(t => t.ParticipantId == participant.Id && t.Title == title))
            {
                continue;
            }
            db.Tasks.Add(new Domain.Entities.Task
            {
                Id = Guid.Parse($"00000000-0000-0000-0000-00000000007{i + 1}"),
                ParticipantId = participant.Id,
                Title = title,
                Deadline = deadline,
                Status = status,
                IsGroup = false,
                CreatedAt = SeedTime("2027-03-30T09:00:00+03:00")
            });
        }

        var diaryDefs = new[]
        {
            ("2027-04-02", "Сбор тестовых данных и проверка доступа к проекту"),
            ("2027-04-05", "Подготовка тестовых наборов для генерации отчёта"),
            ("2027-04-08", "Проверка шаблона ПМ.03 и исправление плейсхолдеров"),
            ("2027-04-10", "Формирование итогового отчёта"),
        };
        for (var i = 0; i < diaryDefs.Length; i++)
        {
            var (date, work) = diaryDefs[i];
            var parsed = DateOnly.Parse(date);
            if (await db.DiaryEntries.AnyAsync(e => e.ParticipantId == participant.Id && e.Date == parsed))
            {
                continue;
            }
            db.DiaryEntries.Add(new DiaryEntry
            {
                Id = Guid.Parse($"00000000-0000-0000-0000-00000000008{i + 1}"),
                ParticipantId = participant.Id,
                Date = parsed,
                WorkDescription = work,
                SupervisorMark = "выполнено",
                SortOrder = i + 1
            });
        }

        const string revisionReportContent =
            "В ходе преддипломной практики по модулю ПМ.03 я выполнял ревьюирование программных продуктов: " +
            "проверял качество кода, формировал отчёты по найденным замечаниям и готовил рекомендации " +
            "по устранению дефектов. Итоговый отчёт сформирован на основе шаблона ПМ.03.";
        const string revisionCharacteristicContent =
            "Студент Иванов М. С. в период преддипломной практики в ООО «Гамма» зарекомендовал себя " +
            "внимательным и ответственным специалистом. Выполненные задачи по ревьюированию и проверке " +
            "документации приняты руководителем организации без замечаний.";

        var documentDefs = new[]
        {
            (RevisionReportDocumentId, "Отчёт по практике (ПМ.03)", "report", DocumentStatus.Accepted, revisionReportContent, "2027-04-14T15:10:00+03:00"),
            (RevisionDiaryDocumentId, "Дневник практики (ПМ.03)", "diary", DocumentStatus.Returned, (string?)null, "2027-04-12T11:40:00+03:00"),
            (RevisionCharacteristicDocumentId, "Характеристика от организации (ПМ.03)", "characteristic", DocumentStatus.Accepted, revisionCharacteristicContent, "2027-04-13T10:05:00+03:00"),
            (RevisionPlanDocumentId, "Индивидуальный план (ПМ.03)", "individual-plan", DocumentStatus.Draft, (string?)null, "2027-04-05T09:30:00+03:00"),
        };
        foreach (var (id, name, typeCode, status, content, updatedAt) in documentDefs)
        {
            var document = await db.Documents.FirstOrDefaultAsync(d => d.ParticipantId == participant.Id && d.DocumentTypeId == typeIds[typeCode]);
            if (document == null)
            {
                db.Documents.Add(new Document
                {
                    Id = id,
                    ParticipantId = participant.Id,
                    DocumentTypeId = typeIds[typeCode],
                    Name = name,
                    Status = status,
                    Content = content,
                    CreatedAt = SeedTime("2027-03-30T09:00:00+03:00"),
                    UpdatedAt = SeedTime(updatedAt)
                });
            }
            else
            {
                // Test-only practice: keep its demo values stable across repeated seed runs.
                // DeletedAt is deliberately never touched here.
                document.Name = name;
                document.Status = status;
                document.Content = content;
                document.UpdatedAt = SeedTime(updatedAt);
            }
        }
        await db.SaveChangesAsync();

        await GenerateSeedVersionAsync(await FindDocumentByTypeAsync(participant.Id, "report") ?? await db.Documents.FirstAsync(d => d.Id == RevisionReportDocumentId),
            1, Guid.Parse("00000000-0000-0000-0000-000000000121"), "report-pm03", "Отчёт принят, замечаний нет", SeedTime("2027-04-14T15:10:00+03:00"));
        await GenerateSeedVersionAsync(await db.Documents.FirstAsync(d => d.Id == RevisionDiaryDocumentId),
            1, Guid.Parse("00000000-0000-0000-0000-000000000122"), null, null, SeedTime("2027-04-10T17:00:00+03:00"));
        await GenerateSeedVersionAsync(await db.Documents.FirstAsync(d => d.Id == RevisionDiaryDocumentId),
            2, Guid.Parse("00000000-0000-0000-0000-000000000123"), null, "Дополните дневник записями за 11–12 апреля", SeedTime("2027-04-12T11:40:00+03:00"));
        await GenerateSeedVersionAsync(await db.Documents.FirstAsync(d => d.Id == RevisionCharacteristicDocumentId),
            1, Guid.Parse("00000000-0000-0000-0000-000000000124"), null, "Характеристика принята", SeedTime("2027-04-13T10:05:00+03:00"));
        await GenerateSeedVersionAsync(await db.Documents.FirstAsync(d => d.Id == RevisionPlanDocumentId),
            1, Guid.Parse("00000000-0000-0000-0000-000000000125"), null, null, SeedTime("2027-04-05T09:30:00+03:00"));
        await db.SaveChangesAsync();

        var notificationDefs = new[]
        {
            (Guid.Parse("00000000-0000-0000-0000-000000000051"),
                "Документ «Дневник практики (ПМ.03)» возвращён на доработку",
                "Дополните дневник записями за 11–12 апреля.", false,
                $"/documents/{RevisionDiaryDocumentId}", "2027-04-12T11:45:00+03:00"),
            (Guid.Parse("00000000-0000-0000-0000-000000000052"),
                "Практика ПМ.03 завершена, оценка 4",
                "Производственная практика (преддипломная) завершена. Оценка: 4 (хорошо).", true,
                (string?)null, "2027-04-14T15:15:00+03:00"),
        };
        foreach (var (id, title, message, isRead, link, createdAt) in notificationDefs)
        {
            if (await db.Notifications.AnyAsync(n => n.Id == id || (n.StudentId == student.Id && n.Title == title)))
            {
                continue;
            }
            db.Notifications.Add(new Notification
            {
                Id = id,
                StudentId = student.Id,
                Title = title,
                Message = message,
                IsRead = isRead,
                Link = link,
                CreatedAt = SeedTime(createdAt)
            });
        }
        await db.SaveChangesAsync();
    }

    private async System.Threading.Tasks.Task<Teacher> EnsureTeacherAsync(string lastName, string firstName, string middleName, string position, Guid fallbackId)
    {
        var teacher = await db.Teachers.FirstOrDefaultAsync(t => t.LastName == lastName && t.FirstName == firstName && t.MiddleName == middleName);
        if (teacher != null)
        {
            return teacher;
        }

        teacher = new Teacher
        {
            Id = fallbackId,
            LastName = lastName,
            FirstName = firstName,
            MiddleName = middleName,
            Position = position
        };
        db.Teachers.Add(teacher);
        await db.SaveChangesAsync();
        return teacher;
    }

    private async Task<Document?> FindDocumentByTypeAsync(Guid participantId, string typeCode)
    {
        return await db.Documents.FirstOrDefaultAsync(d => d.ParticipantId == participantId && d.DocumentType!.Code == typeCode);
    }

    private async System.Threading.Tasks.Task GenerateSeedVersionAsync(Document document, int versionNumber, Guid? versionId,
        string? templateCode, string? teacherComment, DateTimeOffset createdAt)
    {
        if (await db.DocumentVersions.AnyAsync(v => v.DocumentId == document.Id && v.VersionNumber == versionNumber))
        {
            if (teacherComment != null)
            {
                var existing = await db.DocumentVersions.FirstAsync(v => v.DocumentId == document.Id && v.VersionNumber == versionNumber);
                existing.TeacherComment = teacherComment;
            }
            return;
        }

        var participant = await LoadParticipantAsync(document.ParticipantId);
        var template = await ResolveTemplateAsync(document, participant, templateCode);
        if (template == null)
        {
            logger.LogWarning("No active template found for document {DocumentId}; leaving seed version without a file.", document.Id);
            return;
        }

        if (!fileStorage.TryResolveTemplatePath(template.FilePath, out var templatePath) || !File.Exists(templatePath))
        {
            logger.LogWarning("Template file missing at {Path}; leaving seed document {DocumentId} without a file.", templatePath, document.Id);
            return;
        }

        var templateBytes = await File.ReadAllBytesAsync(templatePath);
        var values = TemplateValueBuilder.Build(participant, document);
        var result = renderer.Render(templateBytes, values, new List<RepeatingBlockData>
        {
            new("diary_rows", TemplateValueBuilder.BuildDiaryRows(participant.DiaryEntries.ToList())),
            new("task_rows", TemplateValueBuilder.BuildTaskRows(participant.Tasks.ToList()))
        });

        var (relativePath, size, sha) = fileStorage.SaveVersion(document.Id, versionNumber, result.Content, "docx");
        var version = new DocumentVersion
        {
            Id = versionId ?? Guid.NewGuid(),
            DocumentId = document.Id,
            VersionNumber = versionNumber,
            FileName = $"{document.DocumentType!.Code}-{participant.Student!.StudentNumber.ToLowerInvariant()}-v{versionNumber}.docx",
            FilePath = relativePath,
            ContentType = FileStorage.DocxContentType,
            SizeBytes = size,
            Sha256 = sha,
            Source = DocumentSource.Seed,
            TeacherComment = teacherComment,
            CreatedAt = createdAt
        };

        db.DocumentVersions.Add(version);
        document.TemplateId = template.Id;
        document.CurrentVersionId = version.Id;

        if (result.Warnings.Count > 0)
        {
            logger.LogWarning("Seed generation for {DocumentId} produced warnings: {Warnings}", document.Id, string.Join("; ", result.Warnings));
        }
    }

    private async Task<PracticeParticipant> LoadParticipantAsync(Guid participantId)
    {
        return await db.PracticeParticipants
            .Include(p => p.Practice).ThenInclude(pr => pr!.Teacher)
            .Include(p => p.Practice).ThenInclude(pr => pr!.PracticeType)
            .Include(p => p.Student).ThenInclude(s => s!.Group).ThenInclude(g => g!.Specialty)
            .Include(p => p.Organization)
            .Include(p => p.Assessment)
            .Include(p => p.DiaryEntries)
            .Include(p => p.Tasks)
            .FirstAsync(p => p.Id == participantId);
    }

    private async Task<Domain.Entities.DocumentTemplate?> ResolveTemplateAsync(Document document, PracticeParticipant participant, string? templateCode)
    {
        if (templateCode != null)
        {
            return await db.DocumentTemplates.FirstOrDefaultAsync(t => t.Code == templateCode && t.IsActive);
        }

        document.DocumentType ??= await db.DocumentTypes.FirstAsync(t => t.Id == document.DocumentTypeId);
        var query = db.DocumentTemplates.Where(t => t.IsActive && t.DocumentTypeId == document.DocumentTypeId);
        if (document.DocumentType.Code == "report")
        {
            var byModule = await query.FirstOrDefaultAsync(t => t.ProfessionalModule == participant.Practice!.ProfessionalModule);
            if (byModule != null)
            {
                return byModule;
            }
        }
        return await query.FirstOrDefaultAsync();
    }

    /// <summary>
    /// Npgsql writes <c>timestamptz</c> only from UTC DateTimeOffset values. Parse the
    /// human-readable seed timestamps in MSK and normalise them before saving.
    /// </summary>
    private static DateTimeOffset SeedTime(string value) =>
        DateTimeOffset.Parse(value, System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime();

    private const string OriginalCharacteristicReturn =
        "Здравствуйте, Максим!\n\n" +
        "Рассмотрел характеристику от организации ООО «Альфа». Документ возвращён на доработку: " +
        "в текущей версии не раскрыт перечень порученных задач, поэтому оценить вклад студента в проект невозможно.\n\n" +
        "Что нужно исправить:\n" +
        "1. Добавьте список задач, выполнявшихся на практике, с указанием сроков и результатов.\n" +
        "2. Отдельно отметьте интеграцию модуля авторизации и результаты тестирования API.\n" +
        "3. Укажите даты начала и окончания практики и добавьте подпись руководителя организации.\n\n" +
        "После внесения изменений загрузите новую версию документа.";

    private const string OriginalDiaryAcceptance =
        "Здравствуйте, Максим!\n\n" +
        "Дневник практики принят, замечаний нет. Записи охватывают весь период практики, " +
        "даты расположены в хронологическом порядке, описание работ конкретно и соответствует заданиям.\n\n" +
        "1. Все записи за 03–10 марта заполнены без пропусков.\n" +
        "2. Отметки руководителя проставлены и соответствуют выполненным работам.\n\n" +
        "Продолжайте вести дневник так же аккуратно.";

    private const string OriginalReportClarification =
        "Здравствуйте, Максим!\n\n" +
        "Отчёт находится на проверке. Содержание в целом соответствует плану практики, " +
        "но после проверки обратите внимание на оформление рисунков.\n\n" +
        "1. Пронумеруйте рисунки по порядку: Рисунок 1, Рисунок 2 и так далее.\n" +
        "2. Добавьте подписи под каждым рисунком и ссылки на них в основном тексте.\n\n" +
        "Статус документа не изменяется: замечания носят рекомендательный характер.";

    private const string RevisionDiaryReturn =
        "Здравствуйте, Максим!\n\n" +
        "Дневник практики (ПМ.03) возвращён на доработку. Не хватает записей за 11–12 апреля: " +
        "заключительный этап и подготовка материалов к защите не отражены.\n\n" +
        "1. Добавьте запись за 11 апреля о проверке итогового отчёта и подготовке презентации.\n" +
        "2. Добавьте запись за 12 апреля об оформлении материалов и согласовании с руководителем.\n" +
        "3. Проверьте хронологический порядок записей и проставьте отметки руководителя.\n\n" +
        "Загрузите исправленную версию до 13 апреля.";

    private const string RevisionReportAcceptance =
        "Здравствуйте, Максим!\n\n" +
        "Отчёт по практике (ПМ.03) принят, замечаний нет. Проверен DOCX-файл, сгенерированный " +
        "из шаблона report-pm03.docx: плейсхолдеры заполнены, таблицы и нумерация страниц сохранены.\n\n" +
        "1. Содержание отчёта соответствует теме и объёму преддипломной практики.\n" +
        "2. Оценка «4 (хорошо)» занесена в ведомость.\n\n" +
        "Документ доступен только для просмотра.";

    private record TemplateManifest(string? Code, string? Name, string? DocumentType, string? ProfessionalModule, int Version, List<string>? Placeholders, string? Sha256);
}
