# Revision Task: Practice Portal Prototype — Document Management, Templates, Preview, Comments, and an Additional Test Practice

## 0. Role and baseline

You are a senior full-stack engineer and technical lead. The student-facing prototype described in `BUILD_PROMPT.md` is already implemented and working in:

```text
C:\dev\opencode\PracticeManager
```

Read `BUILD_PROMPT.md` first. It remains the baseline specification for the stack, design system, DOCX templates, placeholder dictionary, and overall scope. This document is a **change request** and overrides the baseline only where it says so explicitly.

Do **not** rebuild the application from scratch and do **not** re-run the original full build prompt. Modify the existing code in place. Preserve all existing GUIDs, routes, statuses, visual tokens, and working DOCX generation unless a change below requires otherwise.

Use the existing project skills where relevant:

- `aspnet-student-api` for backend changes;
- `nextjs-student-ui` for UI changes;
- `docx-templates` if any template metadata changes;
- `demo-seed` for the new practice and comments;
- `verify-prototype` for final verification.

Work autonomously. Do not ask questions unless a required file is missing or the environment cannot run the stack.

---

## 1. Objective

Implement these five changes without breaking the current prototype:

1. **Delete returned documents.** Let the student delete an uploaded document whose teacher review result is `Returned` (returned for revision). Do not allow deletion of `Submitted`, `Accepted`, `Draft`, or `NotUploaded` documents.
2. **Add a separate document-templates tab/page.** Let the student browse all active DOCX templates and download them directly.
3. **Fix the document preview window.** The current preview is clipped and only about half of the document is visible. Make the preview fully readable, scrollable, and responsive.
4. **Add expandable test teacher comments.** Seed realistic full teacher comments and show them in a compact form that can be expanded in a dialog and read completely.
5. **Add one more production practice for testing.** Seed an additional `Производственная практика` with completely different demo data, its own documents, tasks, diary, comments, notifications, and assessment.

Everything else stays student-only. Still no authentication, no login screen, no teacher/admin/organization cabinet.

---

## 2. Non-negotiable constraints

- Keep the existing stack: ASP.NET Core 8 (`net8.0`) Minimal API + EF Core/Npgsql + PostgreSQL 16, Next.js App Router + TypeScript strict + Tailwind + TanStack Query + `lucide-react` + `docx-preview`.
- Keep the Russian UI language. Keep code identifiers, API paths, database names, and file names in English.
- Keep dates formatted as `dd.MM.yyyy` and date-time as `dd.MM.yyyy HH:mm` with `ru-RU`.
- Keep the existing palette, spacing, card style, sidebar, status pills, and typography from `BUILD_PROMPT.md`.
- Keep document versions immutable. The delete operation must be a **soft delete**, not a physical erasure of version rows/files.
- Keep existing stable GUIDs for the original student, practices, documents, templates, and versions. Add new stable GUIDs only for new entities.
- Seed changes must be idempotent. If the database already contains data, add only the missing rows; do not wipe or recreate the database unless the user explicitly asks.
- Do not generate DOCX in the browser. Template downloads must stream the real `.docx` files from `templates/`.
- Run `dotnet build`, `dotnet test`, `npm run build`, and `npm run lint` after the changes. Fix all new errors and warnings introduced by this revision.

---

## 3. Backend changes

### 3.1. Domain model and migration

Add an EF Core migration for these changes.

#### 3.1.1. Soft delete for documents

Add to `Document`:

```csharp
public DateTimeOffset? DeletedAt { get; set; }
```

Behavior:

- `null` means the document is visible.
- Non-null means the document is deleted from all student-facing reads.
- Keep `DocumentVersion` rows and files on disk. Soft delete is intentional: version history remains auditable.
- Add a global query filter or explicit `DeletedAt == null` predicate to every student-facing document query. Prefer an explicit predicate in the document feature queries so the behavior is obvious.
- `GET /api/documents/{id}` for a deleted document must return `404`.
- `GET /api/documents/{id}/comments` for a deleted document must also return `404`.
- Deleted documents must not appear in search results, filters, counts, progress statistics, or practice summaries.
- Notifications that mention a deleted document may keep their historical text, but their link must fall back to `/documents` instead of opening a dead document URL.

#### 3.1.2. Teacher comments

Add a new entity `TeacherComment`:

```csharp
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
```

Add a unique index on `(DocumentId, VersionNumber, CreatedAt)` if it does not conflict with the seed data. Store `FullText` as `text`.

Keep the existing `Document.TeacherComment` / `DocumentVersion.TeacherComment` fields for backward compatibility, but from now on treat `TeacherComment.Summary` as the source for the short comment shown in tables and status boxes.

### 3.2. Delete policy

Extend `DocumentStatusPolicy` with an explicit delete rule.

| Current status | Student action | Result |
|---|---|---|
| `Returned` | `DELETE /api/documents/{id}` | Soft-delete; return `204 No Content` |
| `Submitted` | delete | `409` with Russian `ProblemDetails.detail` |
| `Accepted` | delete | `409` |
| `Draft` | delete | `409` |
| `NotUploaded` | delete | `404` or `409`; do not create deleted shells |

Russian error detail example:

```text
Документ на проверке не может быть удалён.
```

Add a computed boolean `canDelete` to document list and detail DTOs. It must be `true` only for visible documents with status `Returned`.

Do not add a student endpoint that sets `Returned` or `Accepted`. Teacher states still exist only in seed data.

### 3.3. API changes

Keep all existing endpoints. Add or change the following.

| Method | Path | Change |
|---|---|---|
| `DELETE` | `/api/documents/{id}` | New. Soft-delete a `Returned` document. `204`. |
| `GET` | `/api/documents/{id}/comments` | New. Returns teacher comments for the document, newest first. |
| `GET` | `/api/document-templates/{id}/download` | New. Streams the active template DOCX. |
| `GET` | `/api/document-templates?participantId=` | Add `downloadUrl` to each item. |
| `GET` | `/api/documents?participantId=` | Add optional participant scoping. Keep unscoped behavior for backward compatibility. |
| `GET` | `/api/practices` | Must include the new seeded practice. |
| `GET` | `/api/documents/{id}` | Add `comments`, `canDelete`, `downloadUrl`, `previewUrl`. |

`GET /api/documents/{id}/comments` response item:

```json
{
  "id": "00000000-0000-0000-0000-400000000001",
  "documentId": "00000000-0000-0000-0000-000000000003",
  "versionNumber": 1,
  "authorName": "Иванов Петр Петрович",
  "authorRole": "Преподаватель",
  "kind": "return",
  "summary": "Необходимо добавить информацию о задачах",
  "fullText": "Здравствуйте, Максим! ...",
  "createdAt": "2027-03-09T12:18:00+03:00"
}
```

`GET /api/document-templates?participantId=` item additions:

```json
{
  "id": "...",
  "code": "report-pm02",
  "name": "Отчёт по практике (ПМ.02)",
  "documentType": "report",
  "professionalModule": "ПМ.02",
  "version": 1,
  "fileName": "report-pm02.docx",
  "sizeBytes": 123456,
  "downloadUrl": "/api/document-templates/{id}/download"
}
```

### 3.4. Template download

Implement `GET /api/document-templates/{id}/download`:

- resolve `DocumentTemplate.FilePath` under `Templates:Root`;
- reject path traversal and missing files with `404`;
- stream with content type `application/vnd.openxmlformats-officedocument.wordprocessingml.document`;
- set a safe RFC 5987 `Content-Disposition` filename using `DocumentTemplate.FileName`;
- do not expose absolute filesystem paths.

### 3.5. Participant scoping

Several screens will need to show data for the selected practice. Add optional `participantId` query parameters where needed:

- `GET /api/documents?participantId=...`
- `GET /api/practices/{participantId}/tasks` already exists and must be used by the UI;
- `GET /api/practices/{participantId}/diary` already exists and must be used by the UI;
- notifications may stay global, but each notification must keep its existing document link.

When `participantId` is omitted, keep the current behavior for the default demo student.

### 3.6. Backend tests

Add tests for:

- deleting a `Returned` document sets `DeletedAt` and returns `204`;
- deleting `Submitted`, `Accepted`, or `Draft` returns `409`;
- deleted documents are absent from `GET /api/documents` and detail returns `404`;
- old versions remain present in the database after soft delete;
- `GET /api/documents/{id}/comments` returns seeded comments ordered newest first;
- `GET /api/document-templates/{id}/download` returns the correct content type and sanitized filename;
- `GET /api/documents?participantId=` returns only that participant's documents;
- the additional practice is present in `GET /api/practices` with its own participant ID.

---

## 4. Frontend changes

### 4.1. Document templates tab/page

Add a dedicated student tab/page:

- Sidebar item after `Документы`: **`Шаблоны документов`**, icon `FileDown`.
- Route: **`/templates`**.
- Also add a small segmented control on `/documents`: **`Мои документы` | `Шаблоны документов`**, where the second item navigates to `/templates`. Do not replace the existing status filter tabs on the documents list.

`/templates` must show a card with a table:

| Column | Content |
|---|---|
| `Шаблон` | Template name |
| `Тип документа` | Human-readable document type |
| `ПМ` | Professional module, e.g. `ПМ.02` |
| `Версия` | Version number |
| `Размер` | Size in KB/MB using `Intl.NumberFormat('ru-RU')` |
| `Действие` | Blue `Скачать` button with `Download` icon |

Requirements:

- Load templates from `GET /api/document-templates`.
- Download through `downloadUrl`; do not hard-code local file paths.
- Show loading skeletons, an empty state, and an error state with retry.
- Show only active templates.
- Keep all six existing templates: `report-pm01`, `report-pm02`, `report-pm03`, `diary`, `characteristic`, `individual-plan`.

### 4.2. Delete returned documents

Add delete affordances in two places:

1. `/documents` table row action for documents with `canDelete === true`;
2. `/documents/[id]` header action: danger button **`Удалить документ`** with `Trash2` icon.

Behavior:

- Hide/disable delete for every other status. Never render delete for `Submitted`, `Accepted`, `Draft`, or `NotUploaded`.
- Clicking delete opens a confirmation dialog:

```text
Удалить документ «{documentName}»?
Документ исчезнет из списка и больше не будет доступен для просмотра.
```

- Confirm button: `Удалить`; cancel button: `Отмена`.
- On success: show a Russian toast, invalidate document/practice/notification queries, and if the user is on `/documents/[id]`, navigate back to `/documents`.
- On error: show the server `ProblemDetails.detail` in an inline alert or toast.
- Disable the confirm button while the request is in flight.

### 4.3. Fix the document preview window

The current preview shows only about half of the document. Fix the real layout problem, not just by hiding the overflow.

Implement a shared `DocumentPreviewDialog` component used by both the document detail screen and any other preview entry point.

Required structure:

```tsx
<div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 p-4">
  <section className="flex h-[92vh] w-full max-w-[1400px] flex-col overflow-hidden rounded-2xl bg-white">
    <header className="flex shrink-0 items-center justify-between border-b px-5 py-3" />
    <div data-testid="document-preview-viewport" className="min-h-0 flex-1 overflow-auto bg-slate-100 p-4">
      {/* A4 page wrapper / docx-preview target / PDF iframe */}
    </div>
    <footer className="flex shrink-0 items-center justify-end gap-2 border-t px-5 py-3" />
  </section>
</div>
```

Rules:

- Every ancestor of the scrollable preview area must allow shrinking: use `min-h-0` in flex/grid chains. Remove fixed heights such as `h-64`/`max-h-[400px]` from the scroll parent.
- Keep `overflow-auto` on the preview viewport. Do not use `overflow-hidden` on the element that must scroll.
- Center the A4 page horizontally. Never clip the right edge. If a page is wider than the viewport, horizontal scrolling must remain available.
- Use `ResizeObserver` to compute a fit-to-width scale for the `docx-preview` output (`transform-origin: top center`). Provide `По ширине`, `100%`, `+`, and `−` zoom controls.
- Keep `docx-preview` loaded dynamically with `ssr: false`. Fetch the preview blob from `GET /api/documents/{id}/preview?version=` and render it into the viewport ref.
- For PDF, render an `<iframe>` with `width: 100%; height: 100%` using a blob URL, with a download fallback.
- Add `aria-modal="true"`, an accessible title, close button, `Escape` to close, and visible focus styles.
- The inline preview on `/documents/[id]` must use the same rendering logic or share a `DocumentPreview` component with the dialog. Do not keep two divergent implementations.
- Verify visually at 1280px, 768px, and 375px. The bottom of the document must be reachable by scrolling; nothing may be permanently cut off.

### 4.4. Expandable teacher comments

Seed and render full teacher comments. A short one-line comment is not enough.

Backend seed rules are in section 5.3. Frontend rules:

- On `/documents/[id]`, replace the single red comment box with a `TeacherCommentList`.
- Each collapsed comment card shows:
  - author short name and role;
  - created date `dd.MM.yyyy HH:mm`;
  - kind pill: `Возврат` / `Принято` / `Замечание`;
  - `Summary` clamped to 2–3 lines;
  - button **`Читать полностью`** with `ChevronRight`.
- Clicking opens a `CommentDialog` using the same modal primitive as the preview dialog:
  - title `Комментарий преподавателя`;
  - author, role, date, document name, version;
  - full text with paragraph and bullet formatting preserved;
  - scrollable body;
  - close button, `Escape` support, focus management.
- The red-tinted box for a `Returned` document should show the newest `return` comment's `Summary` and include the `Читать полностью` action.
- Do not use `alert()`, `window.alert`, or `line-clamp` without the expand action.
- Add `aria-expanded` on the expand button.

### 4.5. Practice switching for the second test practice

Add a practice context so the new practice's data does not mix with the original one.

- On `/practice`, add a practice selector/card list if more than one practice exists. Default selection: the original `ПМ.02` current practice, using its existing GUID.
- Add a compact practice context chip in the topbar or practice header showing the selected practice name and professional module.
- Store the selected `participantId` in the URL query string, e.g. `/practice?participantId=...`, `/documents?participantId=...`, so reloads keep the context.
- When a practice is selected:
  - `/practice` shows that participant's stages, deadlines, documents, and assessment;
  - `/documents` filters by `participantId`;
  - document detail links and back navigation preserve `participantId`;
  - tasks and diary screens use the existing participant-specific endpoints.
- The original current practice must still be the default and must still be labeled `В процессе`. The new practice appears in `История практик` as completed.

---

## 5. Seed data changes

Preserve every existing seed value from `BUILD_PROMPT.md`. Add the following without changing existing IDs.

### 5.1. Additional production practice (test-only)

Add a second practice for the same student:

```text
Name: Производственная практика (преддипломная)
Type: Производственная
Professional module: ПМ.03
Hours: 144
Dates: 2027-04-01 — 2027-04-14
Status: Completed
Progress: 100
Organization: ООО «Гамма»
Address: г. Санкт-Петербург, ул. Тестовая, д. 7
INN: 7801234567
Organization supervisor: Сидорова Анна Викторовна
Short: Сидорова А. В.
Position: Руководитель проектов
College supervisor: Петрова Мария Ильинична
Short: Петрова М. И.
Position: Преподаватель
Assessment: 4 (хорошо), Зачтено, 14.04.2027
```

Give it its own `PracticeParticipant`, stable GUIDs, stages, documents, diary entries, tasks, and notifications. Do **not** reuse documents or comments from `ПМ.02`.

Stages, all `Completed`:

1. `Подготовительный этап`
2. `Основной этап`
3. `Заключительный этап`
4. `Аттестация`

Tasks:

| Title | Deadline | Status |
|---|---|---|
| `Собрать тестовые данные для отчёта` | 2027-04-05 | `Accepted` |
| `Проверить генерацию DOCX по шаблону ПМ.03` | 2027-04-10 | `Accepted` |
| `Подготовить материалы к защите` | 2027-04-13 | `Submitted` |

Documents:

| Name | Type | Status | Version | Updated | Comment summary |
|---|---|---|---|---|---|
| `Отчёт по практике (ПМ.03)` | report | Accepted | 1 | 14.04.2027 15:10 | `Отчёт принят, замечаний нет` |
| `Дневник практики (ПМ.03)` | diary | Returned | 2 | 12.04.2027 11:40 | `Дополните дневник записями за 11–12 апреля` |
| `Характеристика от организации (ПМ.03)` | characteristic | Accepted | 1 | 13.04.2027 10:05 | `Характеристика принята` |
| `Индивидуальный план (ПМ.03)` | individual-plan | Draft | 1 | 05.04.2027 09:30 | — |

Diary entries for the new practice:

```text
2027-04-02 — Сбор тестовых данных и проверка доступа к проекту
2027-04-05 — Подготовка тестовых наборов для генерации отчёта
2027-04-08 — Проверка шаблона ПМ.03 и исправление плейсхолдеров
2027-04-10 — Формирование итогового отчёта
```

Notifications for the new practice:

```text
Unread: Документ "Дневник практики (ПМ.03)" возвращён на доработку — 12.04.2027 11:45
Read: Практика ПМ.03 завершена, оценка 4 — 14.04.2027 15:15
```

### 5.2. New GUID convention

Use stable GUIDs with a clear pattern so future debugging is easy. Example range:

```text
Practice/participant: 00000000-0000-0000-0000-000000000020
Documents:           00000000-0000-0000-0000-000000000021..24
Comments:            00000000-0000-0000-0000-400000000001..08
```

Do not change GUIDs from the original seed.

### 5.3. Full teacher comments

Seed at least five `TeacherComment` rows. Use multi-paragraph `FullText` with bullet-like lines so the expand dialog is meaningful.

Required comments:

1. Original practice, `Характеристика от организации`, `return`, version 1  
   - Summary: `Необходимо добавить информацию о задачах`
   - Full text must explain: missing task list, need to mention integration of authentication module, need to add dates and supervisor signature, and ask for a new version.

2. Original practice, `Дневник практики`, `acceptance`, version 1  
   - Summary: `Дневник принят, замечаний нет`
   - Full text must confirm completeness and correct date order.

3. Original practice, `Отчёт по практике`, `clarification`, version 1  
   - Summary: `После проверки обратите внимание на оформление рисунков`
   - Full text must mention figure numbering and captions. This document stays `Submitted`; do not add a teacher state change.

4. New practice, `Дневник практики (ПМ.03)`, `return`, version 2  
   - Summary: `Дополните дневник записями за 11–12 апреля`
   - Full text must contain at least three numbered requirements.

5. New practice, `Отчёт по практике (ПМ.03)`, `acceptance`, version 1  
   - Summary: `Отчёт принят, замечаний нет`
   - Full text must mention that the DOCX generated from `report-pm03.docx` was checked.

Author for all comments: `Иванов Петр Петрович` for original practice comments and `Петрова Мария Ильинична` for new practice comments. Role: `Преподаватель`.

Update `Document.TeacherComment` summary fields to match the newest seeded comment for that document.

---

## 6. README and documentation updates

Update `README.md` with:

- the new `DELETE /api/documents/{id}` endpoint and soft-delete behavior;
- the new template download endpoint;
- the new comments endpoint;
- the new `/templates` page;
- the practice selector behavior;
- the fact that the second production practice is test-only data;
- a note that deleted documents remain in the database as soft-deleted records;
- revised smoke-test steps covering delete, template download, preview expansion, comment expansion, and practice switching.

Do not remove existing troubleshooting or reset instructions.

---

## 7. Tests and verification

### 7.1. Automated checks

All must pass:

```bash
cd backend
dotnet build PracticeManager.slnx -c Debug
dotnet test

cd ../frontend
npm run build
npm run lint
```

### 7.2. End-to-end smoke test

If browser automation is available, walk through this sequence and save screenshots under `docs/screenshots/revision-1/`:

1. Open `http://localhost:3000` → redirected to `/practice`; original `ПМ.02` is selected by default.
2. Open `Шаблоны документов` → all six templates are listed → download `report-pm02.docx` → file opens as a valid DOCX.
3. Open the returned `Характеристика от организации` → short comment is visible → click `Читать полностью` → full text dialog opens and scrolls.
4. Open document preview → the whole A4 page is reachable; right edge and bottom are not clipped at 1280px, 768px, and 375px.
5. Click `Удалить документ` on the returned characteristic → confirm → document disappears from `/documents` → direct URL returns a not-found state.
6. Switch to the new `ПМ.03` practice → its own documents, tasks, diary, and assessment are shown; no `ПМ.02` data leaks into the view.
7. Open the returned `Дневник практики (ПМ.03)` → expand the long return comment → read all requirements.
8. Open `/tasks`, `/notifications`, `/profile`, `/progress` → no console errors.

### 7.3. Regression checks

Confirm that all original features still work:

- generate a report from a template;
- upload a DOCX/PDF as a new version;
- old versions remain downloadable;
- submit a draft changes status to `На проверке`;
- invalid status transitions still return `409`;
- DOCX templates still contain no real personal data from the source samples;
- generated DOCX files contain no `{{` tokens;
- footer `PAGE` field and page setup are preserved.

---

## 8. Definition of Done

- [ ] `DELETE /api/documents/{id}` soft-deletes only `Returned` documents.
- [ ] Deleted documents are hidden everywhere and detail returns `404`.
- [ ] Other statuses cannot be deleted and return `409`.
- [ ] `/templates` exists in the sidebar and downloads all active DOCX templates.
- [ ] Template download returns the correct content type and filename.
- [ ] Document preview is fully visible and scrollable at all three breakpoints.
- [ ] Zoom controls and `Escape` close work in the preview dialog.
- [ ] `TeacherComment` rows are seeded with full multi-paragraph text.
- [ ] Comment cards expand into a readable dialog.
- [ ] A second production practice with independent test data is seeded and selectable.
- [ ] Original demo data and GUIDs are unchanged.
- [ ] Backend and frontend builds/tests pass.
- [ ] README is updated.
- [ ] No authentication or staff UI was added.

---

## 9. Execution order

1. Read `BUILD_PROMPT.md` and inspect the current implementation. Run baseline builds before editing.
2. Backend: add migration fields/entity, delete policy, comments endpoint, template download endpoint, participant scoping, and DTO changes.
3. Backend: add idempotent seed rows for comments and the second practice.
4. Backend: add/adjust unit and integration tests.
5. Frontend: add `/templates` page, sidebar link, and documents-page tab link.
6. Frontend: add delete UI with confirmation and query invalidation.
7. Frontend: refactor preview into a shared full-height dialog with fit-to-width zoom.
8. Frontend: add teacher comment list and expandable comment dialog.
9. Frontend: add practice selector and participant-scoped navigation.
10. Run full stack, perform the smoke test, fix all issues, and update README.
11. Report the results.

---

## 10. Final report format

When finished, report in chat:

- what changed, grouped by the five requested features;
- exact file paths added/modified;
- exact commands used for build/test/run;
- test and build results;
- screenshot paths for the verified screens;
- confirmation that original GUIDs and demo data were preserved;
- known limitations, especially the soft-delete behavior;
- anything intentionally left out of scope.
