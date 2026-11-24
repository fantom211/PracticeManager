# PracticeManager — личный кабинет студента

Прототип информационной системы управления производственной практикой студента.
Состоит из ASP.NET Core 8 Minimal API (PostgreSQL, EF Core, OpenXML) и
Next.js 16 App Router интерфейса на русском языке.

Авторизация намеренно отсутствует: приложение всегда работает от имени одного
демонстрационного студента.

## Требования

Для запуска через Docker (рекомендуется для демонстрации):

- Docker Desktop (Windows/macOS) или Docker Engine с Docker Compose v2;
- свободные порты `3000` и `5080` (можно изменить в `.env`).

Для локальной разработки без Docker:

- .NET SDK 8+ (проверено на SDK 10.0.403 с рантаймом 8.0.33);
- Node.js 20.9+ (проверено на Node 22 и 24);
- PostgreSQL 16+;
- Python 3.10+ — только для пересборки/проверки DOCX-шаблонов (`python-docx`).

## Быстрый старт через Docker

```bash
# 1. Склонируйте репозиторий и перейдите в папку проекта
git clone https://github.com/fantom211/PracticeManager.git PracticeManager
cd PracticeManager

# 2. Соберите и запустите все сервисы
docker compose up --build -d
```

Откройте **http://localhost:3000** — приложение сразу откроется на `/practice`.

При первом запуске Docker скачает образы, соберёт backend и frontend, создаст
PostgreSQL, применит миграции EF Core и заполнит демо-данные. Это занимает
несколько минут; повторный запуск намного быстрее.

```bash
# Статус контейнеров
docker compose ps

# Логи (например, backend)
docker compose logs -f backend

# Остановить, сохранив данные
docker compose down

# Полный сброс: удалить БД и загруженные файлы
docker compose down -v
```

PostgreSQL наружу не публикуется — он доступен только внутри Docker-сети.
Если нужно подключиться к БД:

```bash
docker compose exec db psql -U postgres -d practice_portal
```

### Порты

По умолчанию frontend — `3000`, backend — `5080`. Если порт занят, создайте
`.env` из `.env.example` и измените `FRONTEND_PORT` или `BACKEND_PORT`:

```bash
cp .env.example .env          # Linux/macOS
Copy-Item .env.example .env   # Windows PowerShell
```

Пароль PostgreSQL также задаётся в `.env` (`POSTGRES_PASSWORD`). Если меняете его
после первого запуска, удалите volume `pgdata` (`docker compose down -v`) или
смените пароль внутри контейнера.

## Локальная разработка без Docker

```bash
# 1. PostgreSQL 16+ на localhost:5432, БД practice_portal
# 2. Скопируйте пример локальных настроек и укажите свой пароль
cd backend/PracticeManager.Api
cp appsettings.Development.example.json appsettings.Development.json

# 3. Backend — http://localhost:5080
dotnet run

# 4. Frontend — http://localhost:3000 (в отдельном терминале)
cd frontend
npm install
npm run dev
```

`appsettings.Development.json` добавлен в `.gitignore` и не попадает в репозиторий.
Backend при старте применяет миграции EF Core и заполняет демо-данные, включая
регистрацию шаблонов и генерацию стартовых версий документов.

Frontend проксирует `/api/:path*` на backend. Адрес backend задаётся переменной
`BACKEND_URL` (по умолчанию `http://localhost:5080`); в Docker при сборке
подставляется `http://backend:8080`. Поэтому CORS в браузере не требуется
(но backend всё равно разрешает `http://localhost:3000`).

### Адреса

| Сервис | URL |
|---|---|
| Frontend | http://localhost:3000 (`/` → `/practice`) |
| Backend API | http://localhost:5080/api |
| Health check | http://localhost:5080/health |
| Swagger UI | http://localhost:5080/swagger (только при локальном запуске в Development) |
| PostgreSQL в Docker | `db:5432`, наружу не публикуется |

### Переменные окружения

- Docker Compose читает `.env` (шаблон — `.env.example`).
- Локальный backend читает `appsettings.Development.json` (шаблон —
  `appsettings.Development.example.json`) или стандартные env-переменные
  `ConnectionStrings__Default`, `Storage__Root`, `Templates__Root`.
- Локальный frontend использует `BACKEND_URL` при сборке/запуске `next.config.ts`.

## Демо-данные

- Студент: **Иванов Максим Сергеевич**, группа **ИС-21**, speciality **09.02.07**, студенческий билет **ИС-21-001**.
- Текущая практика: **Производственная практика (по профилю специальности)**, **ПМ.02**, 01.03.2027 — 14.03.2027, ООО «Альфа», прогресс 60 %, этап «Основной этап», дедлайн 14.03.2027.
- Прошлая практика: **ПМ.01**, 01.10.2026 — 14.10.2026, ООО «Бета», оценка **5 (отлично)**, «Зачтено».
- Задания: 3 (принято / на проверке / в процессе).
- Дневник: 6 записей с 03.03.2027 по 10.03.2027.
- Документы:
  - Отчёт по практике — `submitted`, v1, обновлён 12.03.2027 14:32;
  - Дневник практики — `accepted`, v1, 10.03.2027 16:20;
  - Характеристика от организации — `returned`, v1, комментарий «Необходимо добавить информацию о задачах»;
  - Индивидуальный план — `notUploaded`.
- Уведомления: 3 (2 непрочитанных), одно со ссылкой на возвращённую характеристику.
- Дополнительная тестовая практика (только для проверки интерфейса): **Производственная практика (преддипломная)**, **ПМ.03**, 01.04.2027 — 14.04.2027, ООО «Гамма», оценка **4 (хорошо)**, «Зачтено». У неё собственные задания, дневник, документы, уведомления и комментарии. Исходная практика **ПМ.02** остаётся выбранной по умолчанию.
- Полные комментарии преподавателя: у возвращённой характеристики ПМ.02, принятого дневника ПМ.02, отправленного отчёта ПМ.02, возвращённого дневника ПМ.03 и принятого отчёта ПМ.03. Короткая версия показывается в таблицах, полный текст — в диалоге `Читать полностью`.

## API

Базовый путь `/api`; JSON в camelCase, перечисления строками, ошибки — `ProblemDetails`.

| Метод | Путь | Назначение |
|---|---|---|
| GET | `/api/me` | Профиль студента |
| GET | `/api/practices` | Текущая и прошлые практики |
| GET | `/api/practices/current` | Текущая практика |
| GET | `/api/practices/{participantId}` | Детали практики |
| GET | `/api/practices/{participantId}/tasks` | Задания практики |
| GET | `/api/practices/{participantId}/diary` | Записи дневника |
| PUT | `/api/practices/{participantId}/diary` | Полная замена записей дневника |
| GET | `/api/documents?status=&participantId=` | Список документов с фильтром по статусу и необязательной привязкой к практике |
| GET | `/api/documents/{id}` | Документ с версиями, комментариями и флагом `canDelete` |
| DELETE | `/api/documents/{id}` | Мягкое удаление документа со статусом `Returned` (`204`) |
| GET | `/api/documents/{id}/comments` | Комментарии преподавателя, сначала новые |
| GET | `/api/documents/{id}/download?version=` | Скачивание версии |
| GET | `/api/documents/{id}/preview?version=` | Встроенный предпросмотр |
| POST | `/api/documents/{id}/generate` | Генерация из шаблона (`{"templateId": "..."}`) |
| POST | `/api/documents/{id}/versions` | Загрузка новой версии, `multipart/form-data`, поле `file` |
| PUT | `/api/documents/{id}/content` | Сохранение текста отчёта |
| POST | `/api/documents/{id}/submit` | Отправка на проверку |
| GET | `/api/document-templates` | Активные шаблоны с `fileName`, `sizeBytes`, `downloadUrl` |
| GET | `/api/document-templates/{id}/download` | Прямое скачивание файла шаблона `.docx` |
| GET | `/api/tasks` | Все задания студента |
| GET | `/api/notifications` | Уведомления |
| POST | `/api/notifications/{id}/read` | Отметить прочитанным |
| POST | `/api/notifications/read-all` | Отметить все прочитанными |
| GET | `/health` | Проверка доступности |

Переходы статусов документов: `NotUploaded/Draft/Returned` → генерация/загрузка →
`Draft` → отправка → `Submitted`; `Submitted` → `Accepted` или `Returned`.
Недопустимые переходы возвращают `409 Conflict`.

Удаление доступно только для видимых документов со статусом `Returned`; остальные
статусы дают `409 Conflict` с русским `ProblemDetails.detail`. Операция мягкая:
устанавливается `DeletedAt`, а строки версий и файлы остаются на диске. Удалённые
документы не попадают в списки, фильтры, счётчики, статистику прогресса и детали
(`404`); ссылки в уведомлениях на удалённые документы перенаправляются на `/documents`.

## Экраны студента

- `/practice` — при нескольких практиках показывается селектор; выбранный
  `participantId` сохраняется в URL и передаётся на `/documents`, задания и дневник.
- `/documents` — вкладки `Мои документы` и `Шаблоны документов`, таблица документов
  с действием удаления для `canDelete === true`.
- `/templates` — отдельная страница со списком всех активных DOCX-шаблонов и кнопкой
  `Скачать` (ссылка берётся из `downloadUrl`, файл не генерируется в браузере).
- `/documents/[id]` — полноэкранный предпросмотр с масштабированием, список
  комментариев преподавателя с раскрытием полного текста и кнопка удаления.

## DOCX-шаблоны

В `templates/` лежат шесть файлов и манифестов:

| Шаблон | Назначение |
|---|---|
| `report-pm01.docx`, `report-pm02.docx`, `report-pm03.docx` | Отчёт по ПМ.01 / ПМ.02 / ПМ.03 |
| `diary.docx` | Дневник практики |
| `characteristic.docx` | Характеристика от организации |
| `individual-plan.docx` | Индивидуальный план |

Синтаксис:

- `{{token}}` — простое поле, заменяется значением из `TemplateValueBuilder`. Неизвестный
  токен заменяется пустой строкой и добавляет предупреждение в ответ генерации.
- `{{#diary_rows}}…{{/diary_rows}}` и `{{#task_rows}}…{{/task_rows}}` — повторяющиеся строки
  таблицы; блок заменяется одной строкой на элемент, пустой блок удаляется.
- Форматирование исходников (A4, поля, таблицы, колонтитул с полем `PAGE`) сохраняется.

Пересборка шаблонов из трёх исходных DOCX и проверка:

```bash
python tools/normalize_templates.py
python tools/verify_docx.py
```

Чтобы добавить шаблон: положите `.docx` и `.json` в `templates/`, добавьте запись в
`DemoDataSeeder.SeedTemplatesAsync`, при необходимости расширьте `TemplateValueBuilder`
новым токеном — и повторите проверку.

Все шесть шаблонов доступны на странице `/templates` и через
`GET /api/document-templates/{id}/download` с корректным `Content-Type` и RFC 5987
именем файла. Путь к файлу проверяется на `Templates:Root`, обход каталогов и
отсутствующие файлы дают `404`.

## Проверки

```bash
# Backend (без Docker; DB-зависимые тесты завершаются early-return,
# если PostgreSQL недоступен). .slnx требует SDK 9+, поэтому используем csproj.
cd backend
dotnet build PracticeManager.Api/PracticeManager.Api.csproj -c Debug
dotnet test PracticeManager.Tests/PracticeManager.Tests.csproj
cd ..

# DOCX
python tools/verify_docx.py

# Frontend
cd frontend
npm run build
npm run lint
```

Ручной smoke-test после запуска стека:

1. Откройте `http://localhost:3000` → редирект на `/practice`, по умолчанию выбран
   `ПМ.02`.
2. Откройте `Шаблоны документов` → шесть шаблонов → скачайте `report-pm02.docx` и
   убедитесь, что файл открывается как DOCX.
3. Откройте возвращённую `Характеристику от организации` → нажмите `Читать полностью`
   → проверьте полный текст комментария.
4. Откройте предпросмотр → нижняя и правая части A4 доступны при ширине 1280, 768 и
   375 px; работают `По ширине`, `100%`, `+`, `−` и закрытие по `Escape`.
5. Удалите возвращённую характеристику → подтвердите → документ исчезает из списка,
   прямой URL показывает not-found; версии остаются в БД как мягко удалённые.
6. Переключитесь на `ПМ.03` → проверьте собственные документы, задания, дневник и
   оценку; данные `ПМ.02` не смешиваются.
7. Откройте возвращённый дневник ПМ.03 → раскройте длинный комментарий с тремя
   требованиями.
8. Проверьте `/tasks`, `/notifications`, `/profile`, `/progress` на отсутствие ошибок
   в консоли. Скриншоты сохраняются в `docs/screenshots/revision-1/`.

Регрессия: генерация отчёта, загрузка новой версии, скачивание старой версии,
отправка черновика, `409` для недопустимых переходов, отсутствие `{{` в сгенерированных
DOCX и сохранение поля `PAGE` в колонтитуле.

## Известные ограничения и допущения

- Нет аутентификации, ролей, кабинетов администратора/преподавателя/организации,
  договоров, импорта Excel, e-mail, PDF-генерации, аудита.
- PDF принимается только как загруженная версия; генерируются всегда DOCX.
- Даты практики в демо-данных относятся к марту 2027 года — дедлайн вычисляется
  относительно текущей даты и может быть отрицательным.
- Хранилище файлов — локальная папка `backend/PracticeManager.Api/Storage/documents`
  при запуске без Docker; в Docker используется named volume `storage`
  (`/app/Storage`). Версии неизменяемы, новые версии получают incremented номер. Мягкое удаление
  документа не физически удаляет версии и файлы: они остаются для аудита, но скрыты
  от всех студенческих запросов.
- Максимальный размер загружаемого файла — 10 МБ, проверяются magic bytes.
- Готовые сгенерированные DEMO-файлы лежат в
  `backend/PracticeManager.Api/Storage/documents/sample-*/v1.docx`.

## Устранение неполадок

- **Порт 3000 или 5080 занят.** Измените `FRONTEND_PORT` / `BACKEND_PORT` в `.env`
  и выполните `docker compose up -d`. Frontend проксирует API внутри Docker-сети,
  поэтому менять `next.config.ts` не нужно.
- **Изменения в коде не применились.** Пересоберите образы:
  `docker compose up --build -d`.
- **Backend не стартует / DB not ready.** Проверьте `docker compose ps` и
  `docker compose logs db backend`. Backend повторяет подключение 10 раз с
  интервалом 3 с; frontend не запустится, пока healthcheck backend не станет
  успешным.
- **Frontend открывается, но API отвечает ошибкой.** Проверьте
  `http://localhost:5080/health` и `docker compose logs backend`. В Docker адрес
  backend задаётся при сборке frontend (`BACKEND_URL=http://backend:8080`).
- **Нет интернета при первой сборке.** Docker должен скачать образы, NuGet-пакеты
  и npm-зависимости; полностью офлайн-сборка не поддерживается.
- **Ошибки миграций.** Убедитесь, что БД доступна; сидер добавляет только
  недостающие строки и не удаляет существующие.
- **Нужно вернуть демо-данные в исходное состояние.**
  `docker compose down -v && docker compose up --build -d` — удаляет БД и
  загруженные файлы, затем сид создаёт их заново.
- **`dotnet ef` не находится.** Установите
  `dotnet tool install --global dotnet-ef --version 8.*`.

### Полный сброс

```bash
docker compose down -v
docker compose up --build -d
```

Danger: `docker compose down -v` удаляет все данные PostgreSQL и загруженные версии.
