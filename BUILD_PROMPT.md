# Прототип «ИС практик»: MCP, skills, правила DOCX и готовый промпт

> Краткое резюме: для стека ASP.NET Core 8 + Next.js + PostgreSQL достаточно 3–4 MCP-серверов
> (Microsoft Learn, Context7, Playwright, DBHub) и 5–6 проектных skills. Исходные DOCX — это
> заполненные отчёты по ПМ.01/02/03, а не шаблоны с плейсхолдерами: их нужно нормализовать,
> сохранив форматирование, и добавить токены `{{...}}`. Ниже — правила, словарь токенов и
> самодостаточный промпт на английском для реализации студенческой части.

---

## 1. Что изучено

**ТЗ** (`Техническое_задание(1).docx`) описывает ИС учёта учебных и производственных практик
колледжа. Роли: учебная часть/администратор, преподаватель, студент, руководитель от организации
(дополнительно). Стек зафиксирован: frontend Next.js, backend учебной части и преподавателей —
TypeScript + PostgreSQL, backend студентов — ASP.NET Core 8.0, СУБД PostgreSQL. В первой версии
допустимо не делать кабинет организации.

**Студенческий модуль по ТЗ:** текущая и прошлые практики; вид/название практики, ПМ,
специальность, сроки, место, руководители, этап и общий статус; задания (групповые и
индивидуальные); документы (заполнение в интерфейсе, скачивание шаблона, загрузка DOCX/PDF);
отправка на проверку; статусы и замечания; история версий; уведомления; итоговый результат
аттестации. Статусы документа и статусы прохождения практики — разные сущности.

**Мокапы интерфейса** встроены в ТЗ (рисунки 2–10). Для студенческой части ключевые —
рисунок 7 («Моя практика») и рисунок 8 («Дневник практики»). Стиль: светлый фон, белые карточки
со скруглением, синий акцент, цветные статус-пилюли, боковое меню, таблицы с лёгкими
разделителями, шрифт без засечек.

**Три DOCX-отчёта** (`ОТЧЕТ ПМ.0.1/0.2/0.3`) — заполненные примеры по ПМ.01, ПМ.02, ПМ.03.
Структура каждого: титульный лист → содержание → задание → аттестационный лист → дневник →
характеристика объекта практики → отчёт о проделанной работе → характеристика обучающегося →
отзыв руководителя. Это основа для шаблонов отчёта, дневника, характеристики и индивидуального
плана.

---

## 2. MCP-серверы для ASP.NET Core 8 + Next.js

MCP не обязателен: файлы, shell, сборка и браузер уже доступны агенту. MCP добавляет ценность
там, где нужны внешние знания или сервис. Не включайте всё сразу — инструменты MCP расходуют
контекст.

| MCP | Тип | Зачем именно в этом проекте | Приоритет |
|---|---|---|---|
| **Microsoft Learn** | remote, без ключа | Официальные доки ASP.NET Core 8, EF Core 8, Minimal API, OpenAPI, `IExceptionHandler`, Npgsql. Убирает выдуманные API | Высокий |
| **Context7** | remote, API key | Версионные доки Next.js App Router, Tailwind, `docx-preview`, EF Core, OpenXml SDK | Высокий |
| **Playwright** | local, `npx` | Прогон студенческих экранов, accessibility-снимки, проверка консоли, E2E | Средний |
| **DBHub** | local, `npx` | Просмотр схемы и данных PostgreSQL, отладка EF Core. Только read-only пользователь | Средний |
| **GitHub** | remote, OAuth | PR, issues, CI, если проект в GitHub | По необходимости |
| **Azure** | remote, OAuth | Только если деплой в Azure | По необходимости |

Что не ставить: `filesystem` (дублирует встроенные инструменты), `git` (обычный git в shell
проще), `memory`/`sequential-thinking` (мало пользы, много контекста), архивный
`@modelcontextprotocol/server-postgres` (заменён DBHub).

### 2.1. Конфигурация OpenCode V2

В OpenCode V2 серверы лежат в `mcp.servers`, а не напрямую в `mcp`. Поле `enabled` заменено на
`disabled`. Секреты — через `{env:NAME}`.

```jsonc
{
  "$schema": "https://opencode.ai/config.json",
  "mcp": {
    "servers": {
      "microsoft-learn": {
        "type": "remote",
        "url": "https://learn.microsoft.com/api/mcp",
        "oauth": false
      },
      "context7": {
        "type": "remote",
        "url": "https://mcp.context7.com/mcp",
        "oauth": false,
        "headers": {
          "CONTEXT7_API_KEY": "{env:CONTEXT7_API_KEY}"
        }
      },
      "playwright": {
        "type": "local",
        "command": ["npx", "-y", "@playwright/mcp@latest", "--headless", "--isolated"]
      },
      "dbhub": {
        "type": "local",
        "command": [
          "npx", "-y", "@bytebase/dbhub@latest",
          "--transport", "stdio",
          "--dsn", "postgres://postgres:postgres@localhost:5432/practice_portal"
        ],
        "disabled": true
      }
    }
  }
}
```

### 2.2. Команды добавления

```bash
opencode mcp add microsoft-learn --global --url https://learn.microsoft.com/api/mcp
opencode mcp add context7 --global --url https://mcp.context7.com/mcp --header 'CONTEXT7_API_KEY={env:CONTEXT7_API_KEY}'
opencode mcp add playwright --global -- npx -y @playwright/mcp@latest --headless --isolated
opencode mcp add dbhub --global -- npx -y @bytebase/dbhub@latest --transport stdio --dsn "postgres://postgres:postgres@localhost:5432/practice_portal"
opencode mcp list
```

Замечания:

- Без `--global` сервер записывается в конфигурацию текущего проекта; с `--global` — во все
  проекты пользователя.
- В этом окружении все серверы уже добавлены глобально. `dbhub` выключен (`disabled: true`),
  пока не запущен PostgreSQL; после `docker compose up -d db` уберите `disabled`.
- В PowerShell команда `opencode mcp add playwright -- npx -y ...` может не принять флаги после
  `--`. В этом случае добавьте сервер вручную в `mcp.servers` (см. конфиг выше).
- Microsoft Learn не требует авторизации, поэтому `oauth: false` избавляет от лишней попытки OAuth.
- Context7 работает и без ключа, но с ключом выше лимиты. Ключ — на context7.com.
- Playwright MCP требует Node.js 20+. В environment стоит Node 24.
- DBHub поддерживает PostgreSQL, SQL Server, MySQL, SQLite и др.; для локальной отладки дайте
  ему read-only пользователя БД. DSN `postgres:postgres` в примере — только для локальной
  демо-БД; в общих средах создайте отдельного read-only пользователя. Это guardrail, а не
  граница безопасности.
- Для кодовых агентов Microsoft теперь рекомендует Playwright CLI + skills вместо MCP: меньше
  токенов на схемы инструментов. Если хватает встроенного `browser`-скилла, Playwright MCP можно
  не включать; MCP оправдан для долгих сессий с сохранением состояния браузера.
- Проверка: `opencode mcp list` должен показать `connected`. Если статус `needs authentication`,
  откройте `/mcps` и войдите.

---

## 3. Skills для проекта

Skill в OpenCode — это `SKILL.md` в `.opencode/skills/<id>/` с frontmatter `name` и
`description`. ID берётся из имени папки. Скиллы подгружаются по необходимости, а не целиком в
каждый промпт. Встроенные `browser`, `opencode`, `report` уже доступны — не дублируйте их.

Пять основных скиллов уже созданы в `C:\dev\opencode\PracticeManager\.opencode\skills\`.
Опциональные `api-contract` и `ef-migrations` не добавлены.

| Skill ID | Когда загружать | Что внутри |
|---|---|---|
| `aspnet-student-api` | Любая работа с backend | `net8.0`, Minimal API, EF Core/Npgsql, DTO, ProblemDetails, переходы статусов, файловое хранилище, OpenAPI |
| `nextjs-student-ui` | Любая работа с frontend | App Router, дизайн-токены из ТЗ, компоненты, статус-пилюли, ru-RU, адаптивность |
| `docx-templates` | Создание/правка шаблонов и рендера | Синтаксис `{{...}}`, повторяющиеся строки таблиц, OpenXML, чек-лист валидации, манифест |
| `demo-seed` | Демо-данные и сценарии | Студент, практика, документы, задания, уведомления, даты из мокапов |
| `verify-prototype` | Приёмка | Команды запуска, Playwright-обход, проверка DOCX, чек-лист |
| `api-contract` (опц.) | Синхронизация типов | OpenAPI → `openapi-typescript`, правило «DTO меняются вместе с TS-типами» |
| `ef-migrations` (опц.) | Миграции | Именование, `dotnet ef`, откат, seed |

Структура:

```text
.opencode/skills/
├── aspnet-student-api/
│   └── SKILL.md
├── nextjs-student-ui/
│   └── SKILL.md
├── docx-templates/
│   ├── SKILL.md
│   └── references/
│       └── placeholders.md
├── demo-seed/
│   └── SKILL.md
└── verify-prototype/
    └── SKILL.md
```

Пример `docx-templates/SKILL.md`:

```markdown
---
name: DOCX Templates
description: Create, validate, and render DOCX templates with {{placeholders}} for the practice portal
---

# DOCX template workflow

1. Start from the approved DOCX sample; never rebuild the layout from scratch.
2. Keep page size, margins, styles, fonts, headers, footers, and the PAGE field unchanged.
3. Replace only variable text with `{{snake_case}}` tokens. Keep fixed college text.
4. Put each token in one paragraph. The renderer must also handle tokens split across runs.
5. Repeat table rows with a `{{#block}}` row, a template row, and a `{{/block}}` row.
6. No nested blocks, macros, tracked changes, comments, or external links.
7. Add a `<name>.json` manifest with code, document type, PM, version, and placeholders.
8. Generate a document with demo data, open it in Word/LibreOffice, and compare with the sample.
9. Run `tools/verify_docx.py` and require zero unresolved `{{` tokens.

See `references/placeholders.md` for the full token dictionary.
```

---

## 4. Анализ DOCX и правила создания шаблонов

### 4.1. Что лежит в исходниках

| Файл | Роль | Профессиональный модуль |
|---|---|---|
| `Техническое_задание(1).docx` | Само ТЗ, а не шаблон студенческого документа | — |
| `ОТЧЕТ ПМ.0.1 Данил (1).docx` | Заполненный пример полного отчёта | ПМ.01 |
| `ОТЧЕТ ПМ.0.2 Данил (2).docx` | Заполненный пример полного отчёта | ПМ.02 |
| `ОТЧЕТ ПМ.0.3 Данил (3).docx` | Заполненный пример полного отчёта | ПМ.03 |

Все три отчёта — не пустые бланки: в них реальные ФИО, группа, организация, даты, дневник и
текст отчёта. Перед использованием их нужно обезличить и превратить в шаблоны.

### 4.2. Структура отчёта (общая для трёх файлов)

1. Титульный лист: шапка колледжа (полное наименование + юридический адрес), слово «ОТЧЕТ»,
   специальность, профессиональный модуль, исполнитель и группа, оценка, руководитель,
   место и даты практики, город и год.
2. `СОДЕРЖАНИЕ`.
3. `ЗАДАНИЕ` на практику: студент, группа, программа подготовки, вид практики, срок, перечень
   практических заданий, подписи руководителей от предприятия и колледжа.
4. `АТТЕСТАЦИОННЫЙ ЛИСТ`: таблица профессиональных компетенций (ПК) и общих компетенций (ОК)
   с пустой колонкой оценки.
5. `ДНЕВНИК`: таблица «Дата | Выполняемая работа | Отметка руководителя практики от предприятия».
6. `ХАРАКТЕРИСТИКА ОБЪЕКТА ПРАКТИКИ`: текст об организации.
7. `ОТЧЕТ О ПРОДЕЛАННОЙ РАБОТЕ`: основной текст с рисунками.
8. `ХАРАКТЕРИСТИКА` обучающегося.
9. `ОТЗЫВ РУКОВОДИТЕЛЯ ПРАКТИКИ`.

### 4.3. Измеренное форматирование

| Параметр | Значение в исходниках |
|---|---|
| Формат страницы | A4, `11910 × 16850` twips (≈210 × 297 мм) |
| Поля | слева `1275` twips ≈ 2,25 см; справа `708` ≈ 1,25 см; сверху `1080–1120` ≈ 1,9–2,0 см; снизу `1300–1400` ≈ 2,3–2,47 см |
| Базовый шрифт | тема `minorFont` = Calibri, базовый размер 11 пт (`sz=22`) |
| Основной текст | стиль Body Text, 14 пт (`sz=28`), выравнивание по ширине, первая строка 1,25 см (`706` twips), отступ слева ≈0,74 см (`421`) |
| Межстрочный интервал | ≈1,5 (`line=357`, `lineRule=auto`) |
| Заголовок 1 | 15,5 пт (`sz=31`), полужирный, по центру |
| Заголовок 2 | 14 пт (`sz=28`), полужирный, по центру |
| Заголовок 3 | 12 пт, полужирный |
| Таблицы | 9,5–12 пт, интервал ≈1,03, по ширине; тонкие границы; шапка с заливкой |
| Колонтитул | нижний, по центру, поле `PAGE`; 11–12 пт |
| Нумерация | титульный лист без номера, далее со 2-й страницы |

Важно: в исходниках нет явного `Times New Roman` на уровне runs — используется тема Calibri.
Не «нормализуйте» шрифт под ГОСТ 14 пт Times New Roman, если заказчик не просил: приоритет —
сохранить вид согласованного образца. Если визуально в образце Times New Roman, а в XML тема
Calibri, значит шрифт подставляется при открытии; сохраняйте фактический вид, а не только XML.

### 4.4. Проблемы исходников, которые нужно решить в шаблонах

- Реальные персональные данные (ФИО, группа, организация, руководители, даты).
- Нет плейсхолдеров: все значения вписаны обычным текстом.
- Текст часто разбит на множество `w:r` runs; токен может оказаться разрезан между runs.
- Разное прямое форматирование абзацев в разных файлах (то есть стилей недостаточно).
- Пустые ячейки оценок в аттестационном листе.
- Дубли нумерации рисунков в одном из отчётов.
- Дневник — таблица с переменным числом строк; её нельзя оставлять статичной.

### 4.5. Правила создания DOCX-шаблонов

1. **Всегда отталкивайтесь от согласованного DOCX-образца.** Не собирайте документ заново из
   HTML или PDF. Сохраняйте `sectPr`, стили, тему, колонтитулы, таблицы и разрывы страниц.
2. **Меняйте только переменный текст.** Шапка колледжа, юридический адрес, названия разделов,
   компетенции ПК/ОК и подписи остаются фиксированными.
3. **Обезличьте все данные.** Замените реальные ФИО, группу, организацию, руководителей, даты,
   номера и тексты на токены. Проверьте не только тело, но и таблицы, колонтитулы и подписи.
4. **Синтаксис токена:** `{{snake_case}}`, только ASCII, нижний регистр, без пробелов внутри
   скобок, регистрозависимо. Примеры: `{{student_full_name}}`, `{{date_start}}`.
5. **Один токен — один абзац.** Не разрывайте токен вручную между абзацами. Рендерер обязан
   обрабатывать токены, разрезанные между runs, но автор шаблона должен этого избегать.
6. **Повторяющиеся строки таблицы:** отдельная строка-маркер `{{#diary_rows}}`, затем строка-
   образец с токенами, затем строка-маркер `{{/diary_rows}}`. Рендерер удаляет маркеры и
   размножает строку-образец по элементам массива. Вложенные блоки не поддерживаются.
7. **Условные блоки** `{{#if_field}}...{{/if_field}}` — опционально. Для прототипа достаточно
   подставлять пустую строку или `—`, если значение не задано.
8. **Никаких макросов, tracked changes, комментариев, паролей, внешних ссылок и OLE-объектов.**
   Только чистый DOCX.
9. **Колонтитулы:** поле `PAGE` сохраняется. Титульный лист — без номера, нумерация со 2-й
   страницы. Не добавляйте текст в колонтитул, кроме разрешённых глобальных токенов.
10. **Таблицы:** шапка повторяется на новой странице (`w:tblHeader`), строки не разрываются
    (`w:cantSplit`), ширина 100 %, границы тонкие, заливка шапки светлая.
11. **Шрифты и размеры:** сохраняйте измеренные значения. Не добавляйте цветной текст, эмодзи и
    декоративные шрифты. Допустима только светлая заливка шапки таблицы.
12. **Даты:** `dd.MM.yyyy`; период — `dd.MM.yyyy – dd.MM.yyyy`. Числа — с пробелом между
    тысячами, десятичная запятая. Часы — `144 ч`. Оценка — `5 (отлично)`.
13. **Имена файлов:** ASCII kebab-case, например `report-pm02.docx`, `diary.docx`,
    `individual-plan.docx`. Рядом — манифест `<name>.json`.
14. **Манифест:** код, название, тип документа, ПМ, версия, список токенов, SHA-256. Пример:

    ```json
    {
      "code": "report-pm02",
      "name": "Отчёт по практике (ПМ.02)",
      "documentType": "report",
      "professionalModule": "ПМ.02",
      "version": 1,
      "placeholders": ["student_full_name", "student_group", "date_start", "date_end"],
      "sha256": "9f2c...e41a"
    }
    ```

15. **Версионирование:** активный шаблон не редактируется на месте. Новая версия — новый файл +
    новый манифест + запись в БД. Старые версии остаются доступными.
16. **Размер:** ≤5 МБ на шаблон. Изображения сжимаются; внешние картинки не допускаются.
17. **Валидация:** шаблон открывается в Word и LibreOffice без «восстановить документ»;
    все токены есть в словаре; у каждого `{{#...}}` есть парный `{{/...}}`; обязательные разделы
    присутствуют; таблицы имеют шапку.
18. **Проверка генерацией:** сгенерируйте документ на демо-данных, откройте его, сравните с
    образцом. В сгенерированном файле не должно остаться `{{` и реальных данных из образца.
19. **Скрипт нормализации:** правки DOCX выполняйте скриптом (`tools/normalize_templates.py`
    или C#-консоль), а не вручную в бинарном файле. Скрипт храните в репозитории.
20. **Не подменяйте DOCX на HTML/PDF.** Генерация — только серверная, через OpenXML SDK.

### 4.6. Словарь плейсхолдеров

ТЗ приводит русские примеры токенов (`{{ФИО_студента}}`), но они иллюстративные. В шаблонах
используется ASCII `snake_case`: это исключает проблемы с кодировкой, регистром и переносами в
Word. Смысл полей сохраняется.

| Токен | Пример значения | Источник |
|---|---|---|
| `college_name` | Московский международный колледж цифровых технологий «Академия ТОП» | константа |
| `college_address` | 121170, г. Москва, ... | константа |
| `city` | Калининград | константа/практика |
| `year` | 2027 | практика |
| `student_full_name` | Иванов Максим Сергеевич | Student |
| `student_short_name` | Иванов М. С. | Student |
| `student_dative_full_name` | Иванову Максиму Сергеевичу | Student |
| `student_genitive_full_name` | Иванова Максима Сергеевича | Student |
| `student_group` | ИС-21 | StudentGroup |
| `student_number` | ИС-21-001 | Student |
| `student_course` | 3 | Student/Practice |
| `student_phone` | +7 (900) 123-45-67 | Student |
| `student_email` | ivanov@example.com | Student |
| `specialty_code` | 09.02.07 | Specialty |
| `specialty_name` | Информационные системы и программирование | Specialty |
| `practice_type` | Производственная | PracticeType |
| `practice_name` | Производственная практика (по профилю специальности) | Practice |
| `professional_module` | ПМ.02 Осуществление интеграции программных модулей | Practice |
| `practice_hours` | 144 | Practice |
| `date_start` | 01.03.2027 | Practice |
| `date_end` | 14.03.2027 | Practice |
| `practice_period` | 01.03.2027 – 14.03.2027 | вычисляемое |
| `organization_name` | ООО «Альфа» | Organization |
| `organization_address` | г. Москва, ул. Примерная, д. 1 | Organization |
| `organization_inn` | 7701234567 | Organization |
| `organization_supervisor` | Петров С. А. | Participant |
| `organization_supervisor_full_name` | Петров Сергей Александрович | Participant |
| `organization_supervisor_position` | Руководитель отдела разработки | Participant |
| `college_supervisor` | Иванов П. П. | Teacher |
| `college_supervisor_full_name` | Иванов Петр Петрович | Teacher |
| `college_supervisor_position` | Преподаватель | Teacher |
| `assessment_grade` | 5 (отлично) или — | Assessment |
| `assessment_result` | Зачтено или — | Assessment |
| `assessment_date` | 14.03.2027 или — | Assessment |
| `assessment_comment` | текст или пусто | Assessment |
| `report_content` | многоабзацный текст | Document.Content |
| `characteristic_text` | текст характеристики | Participant/Document |
| `review_text` | текст отзыва | Participant/Document |
| `diary_date` | 03.03.2027 | DiaryEntry |
| `diary_work` | Изучение документации... | DiaryEntry |
| `diary_mark` | выполнено | DiaryEntry |
| `task_title` | Разработать модуль авторизации | Task |
| `task_description` | описание | Task |
| `task_deadline` | 10.03.2027 | Task |
| `task_status` | В процессе | Task |

Блоки: `{{#diary_rows}} ... {{/diary_rows}}` и `{{#task_rows}} ... {{/task_rows}}`.

### 4.7. Синтаксис повторяющихся строк

```text
┌─────────────────────────────────────────────────────────────┐
│ Дата              │ Выполняемая работа      │ Отметка      │  ← шапка
├─────────────────────────────────────────────────────────────┤
│ {{#diary_rows}}   │                         │              │  ← маркер начала
│ {{diary_date}}    │ {{diary_work}}          │ {{diary_mark}}│ ← строка-образец
│ {{/diary_rows}}   │                         │              │  ← маркер конца
└─────────────────────────────────────────────────────────────┘
```

Рендерер: находит строку с `{{#diary_rows}}` и строку с `{{/diary_rows}}`, берёт строки между
ними как образец, для каждого элемента массива клонирует образец, подставляет `diary_date`,
`diary_work`, `diary_mark`, вставляет перед маркером конца, затем удаляет маркеры и образец.
Если массив пуст — удаляет весь блок. Вложенность не поддерживается.

### 4.8. Чек-лист валидации шаблона

- [ ] Файл открывается в Word и LibreOffice без восстановления.
- [ ] Нет реальных ФИО, группы, организации, дат и текстов из образца.
- [ ] Все `{{...}}` входят в словарь; нет опечаток и пробелов внутри скобок.
- [ ] У каждого `{{#block}}` есть `{{/block}}`; маркеры стоят в отдельных строках.
- [ ] Все обязательные разделы на месте; порядок сохранён.
- [ ] Поля, шрифты, интервалы, колонтитул и `PAGE` не изменились.
- [ ] Таблицы: шапка повторяется, строки не разрываются, границы и заливка сохранены.
- [ ] Нет макросов, комментариев, tracked changes, внешних ссылок.
- [ ] Размер ≤5 МБ; изображения оптимизированы.
- [ ] Манифест заполнен; версия и SHA-256 указаны.
- [ ] Генерация на демо-данных проходит; в выходном DOCX нет `{{`.
- [ ] Выходной документ визуально совпадает с образцом при открытии.

### 4.9. Соответствие исходников и шаблонов

| Исходник | Шаблон | Тип документа | Что заменяется |
|---|---|---|---|
| `ОТЧЕТ ПМ.0.1` | `report-pm01.docx` | report | титул, задание, дневник, текст отчёта, характеристика, отзыв, оценка |
| `ОТЧЕТ ПМ.0.2` | `report-pm02.docx` | report | то же для ПМ.02 |
| `ОТЧЕТ ПМ.0.3` | `report-pm03.docx` | report | то же для ПМ.03 |
| дневник из любого отчёта | `diary.docx` | diary | шапка, студент, группа, период, строки дневника, подписи |
| характеристика из любого отчёта | `characteristic.docx` | characteristic | ФИО, группа, период, текст характеристики, подписи |
| раздел «Задание» | `individual-plan.docx` | individual-plan | ФИО, группа, период, строки заданий, подписи |

---

## 5. Готовый промпт (English)

<!-- PROMPT START -->

# Build Task: Student Practice Portal Prototype (ASP.NET Core 8 + Next.js)

## 0. Your role

You are a senior full-stack engineer and technical lead. Build a complete, runnable prototype in
the current working directory. Work autonomously: read the provided documents, make reasonable
decisions where the specification is silent, implement, run, test, and report. Do not ask
questions unless a required source file is missing or the environment cannot run the stack.

## 1. Objective

Build the **student-facing part** of the "Practice Management Information System" (ИС практик)
described in the technical specification (TZ). The prototype MUST:

- run locally with PostgreSQL;
- have **no authentication** and no login screen;
- open directly into the student cabinet for one seeded demo student;
- show the current and past practices, tasks, documents, notifications, deadlines, and assessment;
- generate real DOCX documents from the provided DOCX templates, replacing placeholders;
- let the student upload DOCX/PDF versions and submit documents for review;
- preserve immutable version history and document statuses;
- match the visual design of the TZ mockups.

This is **not** the full system. Admin, teacher, and organization cabinets, contracts, Excel import,
email, PDF generation, audit logs, and authentication are out of scope.

## 2. Source of truth and inputs

Read these files before writing code. They are in the same folder as this document (or in
`docs/source/` if already copied):

- the TZ: the DOCX that contains `ТЕХНИЧЕСКОЕ ЗАДАНИЕ` and `Система учета практик`;
- three report samples: the DOCX files that contain `ОТЧЕТ` and `ПМ.01`, `ПМ.02`, `ПМ.03`.

Identify them by content, not by exact filename. Copy them unchanged to `docs/source/` (relative to
the project root). If any file is missing, stop and report exactly what is missing.

Priority of sources:

1. This document for scope, stack, API, UI, and acceptance.
2. The TZ for domain requirements and visual intent.
3. The DOCX samples for document structure and formatting.

The TZ is the authoritative specification. It is not a student document template; do not generate
student documents from it.

## 3. Non-negotiable constraints

- **Backend:** ASP.NET Core 8.0 (`net8.0`), C# 12, Minimal APIs, EF Core 8, Npgsql, OpenXML SDK
  (`DocumentFormat.OpenXml`), Swashbuckle.
- **Frontend:** Next.js with App Router, TypeScript strict, Tailwind CSS, TanStack Query,
  `lucide-react`, `docx-preview`.
- **Database:** PostgreSQL 16, started with `docker compose`.
- **No authentication, no authorization, no login page, no JWT, no cookies, no roles.** A single
  demo student is always the current user.
- **UI language:** Russian. Code, identifiers, API paths, DB tables/columns: English.
- **Dates in UI:** `dd.MM.yyyy`; date-time `dd.MM.yyyy HH:mm`; locale `ru-RU`.
- **Files:** stored on disk; only metadata and relative paths in PostgreSQL.
- **Versions:** immutable; never overwrite a previous version.
- **DOCX generation:** server-side with OpenXML. Do not generate DOCX in the browser and do not
  replace templates with HTML/PDF.
- **Visual design:** follow the TZ mockups. Do not invent a dark theme, a different palette, or a
  different layout.
- **Runtime:** Windows, installed .NET SDK and Node.js. No paid services, cloud accounts, or
  external APIs.

If the installed SDK templates do not offer `net8.0`, create the project with the default TFM and
change `<TargetFramework>` to `net8.0` manually. Do not add a `global.json` that requires an SDK
version that is not installed. Build with the installed SDK while targeting `net8.0`.

## 4. Repository layout

The project lives in `C:\dev\opencode\PracticeManager` and is already scaffolded:

- `backend/` contains the `net8.0` Minimal API project `PracticeManager.Api` and the
  `PracticeManager.slnx` solution.
- `frontend/` contains a Next.js App Router app with TypeScript and Tailwind.
- `.opencode/skills/` contains five project skills; the needed MCP servers are installed globally.

Target layout (create the files that are not present yet):

```text
PracticeManager/
├── README.md
├── .gitignore
├── .env.example
├── docker-compose.yml
├── .opencode/
│   └── skills/
│       ├── aspnet-student-api/SKILL.md
│       ├── nextjs-student-ui/SKILL.md
│       ├── docx-templates/
│       │   ├── SKILL.md
│       │   └── references/placeholders.md
│       ├── demo-seed/SKILL.md
│       └── verify-prototype/SKILL.md
├── backend/
│   ├── PracticeManager.slnx
│   ├── PracticeManager.Api/
│   │   ├── PracticeManager.Api.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── Properties/launchSettings.json
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   └── Seed/DemoDataSeeder.cs
│   │   ├── Domain/
│   │   │   ├── Entities/
│   │   │   └── Enums/
│   │   ├── Contracts/
│   │   ├── Features/
│   │   │   ├── Profile/
│   │   │   ├── Practices/
│   │   │   ├── Documents/
│   │   │   ├── Tasks/
│   │   │   ├── Notifications/
│   │   │   └── Templates/
│   │   ├── Services/
│   │   │   ├── CurrentStudentProvider.cs
│   │   │   ├── FileStorage.cs
│   │   │   └── DocumentGeneration/
│   │   │       ├── DocxTemplateRenderer.cs
│   │   │       ├── PlaceholderReplacer.cs
│   │   │       └── RepeatingBlockRenderer.cs
│   │   ├── Middleware/ExceptionHandlingMiddleware.cs
│   │   └── Storage/documents/.gitkeep
│   └── PracticeManager.Tests/
│       ├── PracticeManager.Tests.csproj
│       ├── DocxTemplateRendererTests.cs
│       └── DocumentStatusTests.cs
├── frontend/
│   ├── package.json
│   ├── next.config.ts
│   ├── tsconfig.json
│   ├── src/
│   │   ├── app/
│   │   │   ├── layout.tsx
│   │   │   ├── page.tsx
│   │   │   ├── globals.css
│   │   │   └── (student)/
│   │   │       ├── layout.tsx
│   │   │       ├── practice/page.tsx
│   │   │       ├── documents/page.tsx
│   │   │       ├── documents/[id]/page.tsx
│   │   │       ├── documents/[id]/fill/page.tsx
│   │   │       ├── tasks/page.tsx
│   │   │       ├── notifications/page.tsx
│   │   │       ├── profile/page.tsx
│   │   │       └── progress/page.tsx
│   │   ├── components/
│   │   │   ├── AppShell.tsx
│   │   │   ├── Sidebar.tsx
│   │   │   ├── Topbar.tsx
│   │   │   ├── StatusBadge.tsx
│   │   │   ├── Card.tsx
│   │   │   ├── Button.tsx
│   │   │   ├── PracticeCard.tsx
│   │   │   ├── DocumentTable.tsx
│   │   │   ├── DocumentPreview.tsx
│   │   │   ├── FileDropzone.tsx
│   │   │   ├── DeadlineCard.tsx
│   │   │   ├── NotificationsCard.tsx
│   │   │   ├── EmptyState.tsx
│   │   │   └── Skeleton.tsx
│   │   └── lib/
│   │       ├── api.ts
│   │       ├── types.ts
│   │       ├── format.ts
│   │       └── statuses.ts
│   └── public/
├── templates/
│   ├── report-pm01.docx + .json
│   ├── report-pm02.docx + .json
│   ├── report-pm03.docx + .json
│   ├── diary.docx + .json
│   ├── characteristic.docx + .json
│   └── individual-plan.docx + .json
├── tools/
│   ├── normalize_templates.py
│   └── verify_docx.py
└── docs/
    └── source/
        ├── technical-specification.docx
        ├── report-pm01.docx
        ├── report-pm02.docx
        └── report-pm03.docx
```

## 5. Demo data (must match the mockups)

Seed exactly this data on first run. The names and dates are chosen to match the student mockup and
the report samples; where the mockups use illustrative data that differs from the samples, follow
this list for consistency with the templates.

**Student**

- Full name: `Иванов Максим Сергеевич`
- Short name: `Иванов М. С.`
- Dative: `Иванову Максиму Сергеевичу`
- Genitive: `Иванова Максима Сергеевича`
- Group: `ИС-21`
- Specialty: `09.02.07` — `Информационные системы и программирование`
- Course: `3`
- Student number: `ИС-21-001`
- Phone: `+7 (900) 123-45-67`
- Email: `ivanov@example.com`

**Current practice**

- Name: `Производственная практика (по профилю специальности)`
- Type: `Производственная`
- Professional module: `ПМ.02 Осуществление интеграции программных модулей`
- Hours: `144`
- Dates: `2027-03-01` — `2027-03-14`
- Status: `InProgress`
- Progress: `60`
- Organization: `ООО «Альфа»`, address `г. Москва, ул. Примерная, д. 1`, INN `7701234567`
- Organization supervisor: `Петров Сергей Александрович`, short `Петров С. А.`,
  position `Руководитель отдела разработки`
- College supervisor: `Иванов Петр Петрович`, short `Иванов П. П.`, position `Преподаватель`

**Past practice**

- `ПМ.01`, `01.10.2026` — `14.10.2026`, organization `ООО «Бета»`, status `Completed`,
  assessment `5 (отлично)`, result `Зачтено`.

**Stages of the current practice**

1. `Подготовительный этап` — `Completed`
2. `Основной этап` — `InProgress`
3. `Заключительный этап` — `NotStarted`
4. `Аттестация` — `NotStarted`

**Tasks**

1. `Изучить документацию по проекту` — deadline `2027-03-05` — `Accepted`
2. `Разработать модуль авторизации` — deadline `2027-03-10` — `Submitted`
3. `Подготовить отчёт по практике` — deadline `2027-03-13` — `InProgress`

**Documents**

| Name | Type | Status | Version | Updated | Teacher comment |
|---|---|---|---|---|---|
| Отчёт по практике | report | Submitted | 1 | 12.03.2027 14:32 | — |
| Дневник практики | diary | Accepted | 1 | 10.03.2027 16:20 | — |
| Характеристика от организации | characteristic | Returned | 1 | 09.03.2027 12:15 | Необходимо добавить информацию о задачах |
| Индивидуальный план | individual-plan | NotUploaded | — | — | — |

**Diary entries**

- `2027-03-03` — `Изучение документации по проекту, настройка среды разработки`
- `2027-03-04` — `Разработка модуля авторизации`
- `2027-03-05` — `Написание тестов и проверка API`
- `2027-03-06` — `Подготовка раздела отчёта о выполненных работах`
- `2027-03-09` — `Исправление замечаний руководителя`
- `2027-03-10` — `Оформление дневника практики`

**Notifications**

1. Unread: `Документ "Характеристика от организации" возвращён на доработку` — `09.03.2027 12:20`
2. Unread: `Практика началась. Удачной работы!` — `01.03.2027 09:00`
3. Read: `Документ "Дневник практики" принят` — `10.03.2027 16:25`

**Assessment:** none for the current practice; the past practice has grade `5`, result `Зачтено`.

Seed `report_content` and `characteristic_text` with neutral demo text about the student's practice
on the portal. Do **not** copy the sample report text about housing or any other unrelated domain,
and do not ship the real personal data from the samples.

Seed the document files for the first three documents by calling the same renderer used by the API.
This proves the templates work and makes previews available immediately. If a template is missing,
log a warning and leave the document without a file; do not crash the app.

## 6. Backend requirements

### 6.1. Domain model

Use EF Core entities with `Guid` primary keys, `DateOnly` for dates, `DateTimeOffset` for timestamps,
and `string` for JSONB payloads.

- `Specialty(Id, Code, Name)`
- `StudentGroup(Id, Name, SpecialtyId, AdmissionYear, GraduationYear)`
- `Student(Id, LastName, FirstName, MiddleName, FullNameDative, FullNameGenitive, GroupId,
  StudentNumber, Phone, Email)`
- `Teacher(Id, LastName, FirstName, MiddleName, Position, Phone, Email)`
- `Organization(Id, Name, Inn, Ogrn, LegalAddress, ActualAddress, Phone, Email, ContactPerson)`
- `PracticeType(Id, Name, Description)`
- `Practice(Id, GroupId, TeacherId, PracticeTypeId, Name, ProfessionalModule, Course, Hours,
  StartDate, EndDate, Status, CreatedAt)`
- `PracticeStage(Id, PracticeId, Order, Name, Description, PlannedStart, PlannedEnd)`
- `PracticeParticipant(Id, PracticeId, StudentId, OrganizationId, ExternalSupervisorName,
  ExternalSupervisorPhone, Status, Result, ProgressPercent, CurrentStageId)`
- `ParticipantStage(Id, ParticipantId, StageId, Status, CompletedAt)`
- `Task(Id, ParticipantId, Title, Description, Deadline, Status, TeacherComment, IsGroup,
  CreatedAt)`
- `DocumentType(Id, Code, Name, Description)`
- `DocumentTemplate(Id, Code, DocumentTypeId, PracticeTypeId, ProfessionalModule, Name, FileName,
  FilePath, PlaceholdersJson, Version, IsActive, Sha256, CreatedAt)`
- `Document(Id, ParticipantId, TemplateId, DocumentTypeId, Name, Status, Content,
  CurrentVersionId, CreatedAt, UpdatedAt)`
- `DocumentVersion(Id, DocumentId, VersionNumber, FileName, FilePath, ContentType, SizeBytes,
  Sha256, Source, TeacherComment, CreatedAt)`
- `DiaryEntry(Id, ParticipantId, Date, WorkDescription, SupervisorMark, SortOrder)`
- `Assessment(Id, ParticipantId, TeacherId, Grade, Result, Comment, AssessedAt)`
- `Notification(Id, StudentId, Title, Message, IsRead, Link, CreatedAt)`

Enums (store and serialize as camelCase strings):

- `DocumentStatus`: `NotUploaded`, `Draft`, `Submitted`, `Returned`, `Accepted`
- `ParticipantStatus`: `Assigned`, `NotStarted`, `InProgress`, `Completed`, `Failed`
- `TaskStatus`: `Assigned`, `InProgress`, `Submitted`, `Accepted`, `Returned`
- `StageStatus`: `NotStarted`, `InProgress`, `Completed`
- `AssessmentResult`: `NotAssessed`, `Passed`, `Failed`
- `DocumentSource`: `Generated`, `Uploaded`, `Seed`

Constraints:

- unique `Assessment(ParticipantId)`;
- unique `DocumentVersion(DocumentId, VersionNumber)`;
- unique `DocumentType(Code)`;
- unique `DocumentTemplate(Code, Version)`;
- `Document.CurrentVersionId` must reference a version of the same document (enforce in code).

Configure `jsonb` for `DocumentTemplate.PlaceholdersJson` and `text` for long content.

### 6.2. Status transitions

Implement a single `DocumentStatusPolicy` service:

| From | To | Trigger |
|---|---|---|
| `NotUploaded` | `Draft` | generate or upload |
| `Draft` | `Submitted` | student submits |
| `Draft` | `Draft` | regenerate or upload a new version |
| `Returned` | `Draft` | generate or upload a corrected version |
| `Submitted` | `Returned` | teacher (seed only, no endpoint) |
| `Submitted` | `Accepted` | teacher (seed only, no endpoint) |
| `Accepted` | — | read-only |

Reject any other transition with HTTP `409` and a Russian `ProblemDetails.detail`. There is no
student endpoint to set `Returned` or `Accepted`; those states exist only in seed data.

`Submit` requires a current version file. `Generate` and `Upload` are rejected for `Submitted` and
`Accepted`.

### 6.3. API

Base path `/api`. JSON uses camelCase and string enums. Return `ProblemDetails` for errors.

| Method | Path | Purpose |
|---|---|---|
| GET | `/api/me` | Student profile, group, specialty |
| GET | `/api/practices` | Current and past practices |
| GET | `/api/practices/current` | Current practice detail |
| GET | `/api/practices/{participantId}` | Practice detail |
| GET | `/api/practices/{participantId}/tasks` | Tasks |
| GET | `/api/practices/{participantId}/diary` | Diary entries |
| PUT | `/api/practices/{participantId}/diary` | Replace diary entries |
| GET | `/api/documents` | Documents, optional `?status=` |
| GET | `/api/documents/{id}` | Document detail with versions |
| GET | `/api/documents/{id}/download?version=` | Download DOCX/PDF |
| GET | `/api/documents/{id}/preview?version=` | Inline preview |
| POST | `/api/documents/{id}/generate` | Generate from template |
| POST | `/api/documents/{id}/versions` | Upload a new version (multipart) |
| PUT | `/api/documents/{id}/content` | Save report content |
| POST | `/api/documents/{id}/submit` | Submit for review |
| GET | `/api/document-templates?participantId=` | Available templates |
| GET | `/api/tasks` | All tasks of the current student |
| GET | `/api/notifications` | Notifications |
| POST | `/api/notifications/{id}/read` | Mark one as read |
| POST | `/api/notifications/read-all` | Mark all as read |
| GET | `/health` | Health check |

Key response shape for `GET /api/practices/current`:

```json
{
  "participantId": "00000000-0000-0000-0000-000000000001",
  "practiceName": "Производственная практика (по профилю специальности)",
  "practiceType": "Производственная",
  "professionalModule": "ПМ.02 Осуществление интеграции программных модулей",
  "specialtyCode": "09.02.07",
  "specialtyName": "Информационные системы и программирование",
  "dateStart": "2027-03-01",
  "dateEnd": "2027-03-14",
  "organization": { "name": "ООО «Альфа»", "address": "г. Москва, ул. Примерная, д. 1", "inn": "7701234567" },
  "collegeSupervisor": { "shortName": "Иванов П. П.", "fullName": "Иванов Петр Петрович", "position": "Преподаватель" },
  "organizationSupervisor": { "shortName": "Петров С. А.", "fullName": "Петров Сергей Александрович", "position": "Руководитель отдела разработки" },
  "status": "inProgress",
  "statusLabel": "В процессе",
  "progressPercent": 60,
  "currentStage": { "name": "Основной этап", "status": "inProgress" },
  "stages": [
    { "name": "Подготовительный этап", "status": "completed" },
    { "name": "Основной этап", "status": "inProgress" },
    { "name": "Заключительный этап", "status": "notStarted" },
    { "name": "Аттестация", "status": "notStarted" }
  ],
  "assessment": null,
  "deadline": { "date": "2027-03-14", "title": "Окончание практики", "daysLeft": 5 },
  "unreadNotifications": 2
}
```

`GET /api/documents` item:

```json
{
  "id": "...",
  "name": "Отчёт по практике",
  "typeCode": "report",
  "typeName": "Отчёт по практике",
  "status": "submitted",
  "statusLabel": "На проверке",
  "updatedAt": "2027-03-12T14:32:00+03:00",
  "teacherComment": null,
  "currentVersionNumber": 1,
  "canGenerate": false,
  "canUpload": false,
  "canSubmit": false
}
```

`GET /api/documents/{id}` adds `versions` (number, source, size, createdAt, teacherComment,
downloadUrl) and `previewUrl`.

`POST /api/documents/{id}/generate` body:

```json
{ "templateId": "optional-guid" }
```

If `templateId` is omitted, resolve the active template by document type and professional module.
Create a new `DocumentVersion` with `Source=Generated`, `VersionNumber = max + 1`, set
`Document.CurrentVersionId`, set `Status=Draft`, save the file, and return the document detail.

`POST /api/documents/{id}/versions` uses `multipart/form-data` with a single `file` field. Accept
only DOCX and PDF up to 10 MB. Validate magic bytes, not just the extension. Create a new immutable
version and set `Status=Draft`.

`PUT /api/documents/{id}/content` body:

```json
{ "content": "Многоабзацный текст отчёта..." }
```

Only for `report` documents and only in `Draft`, `Returned`, or `NotUploaded`.

`GET /api/documents/{id}/download` and `/preview` stream the requested version (latest by default)
with the correct content type and an RFC 5987 `Content-Disposition` filename.

### 6.4. DOCX generation engine

Implement a reusable `DocxTemplateRenderer` with these guarantees:

1. Load the template into a `WordprocessingDocument` from a `MemoryStream`; never mutate the
   template file.
2. Traverse body paragraphs, table cell paragraphs, headers, and footers.
3. Replace `{{token}}` even when the token is split across multiple `w:r` runs. Join the text of
   the paragraph, find matches, write the replacement into the first affected run, clear the rest,
   and preserve the first run's `RunProperties`.
4. Support multi-line values: split `\n` into `<w:br/>` within the same paragraph. Split `\n\n`
   into new paragraphs if needed.
5. Support repeating table blocks:
   - a row containing `{{#block}}` marks the start;
   - rows between the markers are the template;
   - a row containing `{{/block}}` marks the end;
   - clone the template rows for each item, replace item tokens, remove markers and the template;
   - if the array is empty, remove the whole block.
6. Replace unresolved tokens with an empty string and collect warnings; return them in the
   generation response and log them. Do not leave `{{` in the output.
7. Preserve page size, margins, styles, fonts, headers, footers, and the `PAGE` field.
8. Save to a new stream, write to disk under a GUID directory, compute SHA-256, and return metadata.

Template tokens are ASCII `snake_case` inside `{{...}}`. The full dictionary is in section 8.

Use `DocumentFormat.OpenXml` 3.x. Add unit tests for: simple replacement, split-run replacement,
table cell replacement, repeating rows, empty block removal, footer `PAGE` preservation, and
unresolved-token warnings.

### 6.5. File storage and validation

- Root: `Storage/documents/{documentId}/v{version}.{ext}`.
- Store relative paths in DB, never absolute paths.
- Sanitize original filenames; keep the original name in DB for display.
- Validate DOCX: ZIP signature and `[Content_Types].xml` entry. Validate PDF: `%PDF-` header.
- Max 10 MB; return `413` above the limit and `415` for unsupported types.
- Prevent path traversal; never serve arbitrary paths.
- No directory listing.

### 6.6. Errors, logging, OpenAPI

- Global exception handler returning `ProblemDetails` with `traceId`.
- Validation errors: `400` with `errors` dictionary; user-facing `detail` in Russian.
- Status conflicts: `409`.
- Not found: `404`.
- Log generation warnings, status transitions, uploads, and failures with `ILogger`.
- Swagger UI at `/swagger`, OpenAPI JSON at `/swagger/v1/swagger.json`.
- CORS: allow `http://localhost:3000`.
- Health check at `/health`.

### 6.7. Configuration and startup

- `ConnectionStrings:Default` = `Host=localhost;Port=5432;Database=practice_portal;Username=postgres;Password=postgres`.
- `Storage:Root` = `Storage/documents`.
- `Templates:Root` = `../templates`.
- `Demo:StudentId` = a fixed GUID used by `CurrentStudentProvider`.
- The student service owns its tables in the shared PostgreSQL database. There are no other
  services in this prototype, so no inter-service calls or synchronization are required.
- On startup: retry DB connection, apply migrations, seed if empty, generate seed document files.
- `launchSettings.json`: `http://localhost:5080`.

`docker-compose.yml`:

```yaml
services:
  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: practice_portal
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d practice_portal"]
      interval: 5s
      timeout: 5s
      retries: 10

volumes:
  pgdata:
```

## 7. DOCX templates to create

Use the three report samples as the base. Create six templates in `templates/` with manifests:

| Template | Type | Based on | Repeating blocks |
|---|---|---|---|
| `report-pm01.docx` | report | `ОТЧЕТ ПМ.0.1` | `diary_rows` |
| `report-pm02.docx` | report | `ОТЧЕТ ПМ.0.2` | `diary_rows` |
| `report-pm03.docx` | report | `ОТЧЕТ ПМ.0.3` | `diary_rows` |
| `diary.docx` | diary | diary section of a report | `diary_rows` |
| `characteristic.docx` | characteristic | characteristic section | — |
| `individual-plan.docx` | individual-plan | assignment section | `task_rows` |

Rules:

1. Start from the source DOCX; preserve page setup, styles, theme fonts, headers, footers, tables,
   and the `PAGE` field.
2. Replace all real values with the tokens from section 8. Check the cover, tables, captions, and
   signature lines. Replace the entire body of `ОТЧЕТ О ПРОДЕЛАННОЙ РАБОТЕ` with
   `{{report_content}}`, the body of `ХАРАКТЕРИСТИКА` with `{{characteristic_text}}`, and the body
   of `ОТЗЫВ РУКОВОДИТЕЛЯ ПРАКТИКИ` with `{{review_text}}`.
3. Use `{{#diary_rows}}` / `{{/diary_rows}}` for the diary table and `{{#task_rows}}` /
   `{{/task_rows}}` for the individual plan.
4. For `individual-plan.docx`, build a table with columns `№`, `Задание`, `Срок`, `Статус` and use
   the `task_rows` block; keep the college header and signature lines.
5. Do not use macros, tracked changes, comments, external links, or password protection.
6. Write a one-off normalization script in `tools/normalize_templates.py` (Python 3 standard
   library: `zipfile` + `xml.etree`) that performs the replacements and writes valid DOCX files.
   Keep the script in the repository. Do not hand-edit binary DOCX files.
7. Create a manifest next to each template with `code`, `name`, `documentType`,
   `professionalModule`, `version`, `placeholders`, and `sha256`.
8. Validate with `tools/verify_docx.py`: no `{{` remains after generation, the expected demo
   strings are present, the footer still contains `PAGE`, page size and margins are unchanged, and
   the diary table row count equals the number of entries plus the header.

## 8. Placeholder dictionary

Use exactly these tokens:

```text
college_name, college_address, city, year,
student_full_name, student_short_name, student_dative_full_name, student_genitive_full_name,
student_group, student_number, student_course, student_phone, student_email,
specialty_code, specialty_name,
practice_type, practice_name, professional_module, practice_hours,
date_start, date_end, practice_period,
organization_name, organization_address, organization_inn,
organization_supervisor, organization_supervisor_full_name, organization_supervisor_position,
college_supervisor, college_supervisor_full_name, college_supervisor_position,
assessment_grade, assessment_result, assessment_date, assessment_comment,
report_content, characteristic_text, review_text,
diary_date, diary_work, diary_mark,
task_title, task_description, task_deadline, task_status
```

Blocks: `diary_rows`, `task_rows`. Syntax: `{{token}}`, `{{#block}}`, `{{/block}}`.

Formatting rules for values:

- dates `dd.MM.yyyy`; period `dd.MM.yyyy – dd.MM.yyyy`;
- missing assessment values → `—`;
- missing optional text → empty string;
- multi-line text uses `\n` and is rendered as line breaks.

## 9. Frontend requirements

### 9.1. Design system (derived from the TZ mockups)

Use these tokens consistently. The mockups are the source of truth; if a token conflicts with an
image, follow the image.

| Token | Value |
|---|---|
| App background | `#F6F8FC` |
| Surface | `#FFFFFF` |
| Border | `#E6EAF2` |
| Text primary | `#0F172A` |
| Text muted | `#64748B` |
| Primary | `#2563EB` |
| Primary hover | `#1D4ED8` |
| Primary soft | `#EFF6FF` |
| Success | `#16A34A`, soft `#DCFCE7` |
| Warning | `#EA580C`, soft `#FFEDD5` |
| Danger | `#DC2626`, soft `#FEE2E2` |
| Info | `#2563EB`, soft `#DBEAFE` |
| Neutral | `#64748B`, soft `#F1F5F9` |
| Purple accent | `#7C3AED`, soft `#EDE9FE` |
| Card radius | `16px` |
| Button/input radius | `10px` |
| Pill radius | `999px` |
| Shadow | `0 1px 2px rgba(15,23,42,.04)`; hover `0 4px 12px rgba(15,23,42,.08)` |
| Font | `Inter, "Segoe UI", system-ui, sans-serif` |
| Base text | `14px` |
| Small text | `12px` |
| H1 | `28px / 700` |
| H2 | `20px / 600` |
| Sidebar width | `232px` |
| Content max width | `1120px` |

Component rules:

- **Sidebar:** white, right border `#E6EAF2`; logo is a blue graduation-cap icon plus two lines
  `Личный кабинет / студента`; nav items with 18px icons; active item has `#EFF6FF` background,
  blue text, and a blue left indicator; logout at the bottom (can be disabled/hidden because
  there is no auth).
- **Topbar:** page title and subtitle on the left; bell with red unread badge and avatar with
  initials on the right. No search bar unless it is functional.
- **Cards:** white, 16px radius, 1px border, no heavy shadow; 20–24px padding.
- **Buttons:** primary filled blue; secondary white with border; ghost transparent; danger soft
  red. 40px height, 10px radius, 14px/600. Include icons where the mockup does.
- **Status pills:** 12px/600, 4px 10px padding, 6px dot, soft background + strong text color.
  Labels and tones:
  - `Не загружен` — neutral
  - `Черновик` — neutral
  - `На проверке` — warning
  - `Возвращён на доработку` — danger
  - `Принят` — success
  - `В процессе` — success
  - `Завершена` — info
  - `Не начата` — neutral
  - `Назначено` — info
- **Tables:** header background `#F8FAFC`, 13px/500 muted; rows 14px; bottom border `#EEF2F7`;
  hover `#F8FAFC`; no vertical borders; row actions right-aligned.
- **Forms:** label 13px/500 muted above the field; input 40px, white, 1px border, 10px radius;
  focus ring 2px `#BFDBFE`; disabled/read-only fields use `#F8FAFC` background.
- **Empty states:** icon, short Russian message, primary action if applicable.
- **Icons:** `lucide-react` only. Suggested icons: `GraduationCap`, `Briefcase`, `FileText`,
  `CalendarDays`, `Building2`, `Users`, `Bell`, `User`, `ClipboardList`, `Download`, `Upload`,
  `Eye`, `Check`, `X`, `RotateCcw`, `Plus`, `Trash2`, `Search`, `Filter`, `ChevronRight`,
  `LogOut`, `Send`, `Clock`.
- **DOCX preview:** import `docx-preview` dynamically with `ssr: false`; it needs the DOM. Fetch
  the preview blob from the API and render it into a ref. For PDF, use an `<iframe>` with a blob
  URL and a download fallback.

### 9.2. Layout

- `AppShell` = sidebar + topbar + content area. The sidebar collapses to a top bar below 1024px.
- Content max width 1120px, centered, 24–32px padding.
- All pages are Russian. Use `Intl.DateTimeFormat('ru-RU')` and `Intl.NumberFormat('ru-RU')`.
- Use client-side data fetching with TanStack Query. The app is a prototype; SSR is not required.
- `next.config.ts` rewrites `/api/:path*` to `http://localhost:5080/api/:path*` so the browser can
  use relative URLs and CORS is not required in development. Keep backend CORS enabled anyway.

### 9.3. Screens

#### `/` → redirect to `/practice`

#### `/practice` — «Моя практика»

Recreate figure 7 closely:

1. Header: `Моя практика`, subtitle `Актуальная информация о практике и документах`.
2. Practice hero card:
   - blue rounded icon tile with a briefcase;
   - green pill `Активная практика`;
   - title and module subtitle;
   - meta row with four items and icons: `Сроки практики`, `Организация`,
     `Руководитель от организации`, `Руководитель от колледжа`;
   - right panel `Общий статус` with a blue dot, `В процессе`, and a 60% progress bar.
3. `Мои документы` card: table columns `Документ`, `Статус`, `Дата загрузки / обновления`,
   `Комментарий`, action. Rows exactly as the seed. Actions: `Просмотр` (eye) or `Загрузить`
   (download) for `NotUploaded`.
4. Bottom grid:
   - `Ближайшие сроки` card: calendar icon, `14.03.2027`, `Окончание практики`, `до 5 дней`.
   - `Уведомления` card: two unread items, `Все уведомления →`.
5. `История практик` card: table of past practices with period, organization, result, grade.

#### `/documents` — «Документы»

- Header and subtitle.
- Filter tabs: `Все`, `Не загружены`, `На проверке`, `Возвращены`, `Приняты`.
- Search by document name.
- Table with all documents, status, current version, updated date, comment, action.
- Empty state when the filter has no results.

#### `/documents/[id]` — document detail

- Breadcrumb `← К списку документов`.
- Header: document name, practice context, status pill.
- Three columns on desktop:
  - left: list of the student's documents with status dots;
  - center: document preview on a gray background inside a white A4-like page, toolbar with
    download and print; use `docx-preview` for DOCX and an iframe for PDF;
  - right: teacher comment card and version history timeline.
- Actions:
  - `Скачать`;
  - `Сгенерировать из шаблона` (when allowed);
  - `Загрузить новую версию` (when allowed);
  - `Отправить на проверку` (only `Draft`);
  - `Исправить` (only `Returned`).
- Show the teacher comment in a red-tinted box when `Returned`.
- Show version history: number, source (`Сгенерирован` / `Загружен`), author `Студент`, date,
  size, download link, teacher comment.

#### `/documents/[id]/fill` — fill/generate

Recreate figure 8 closely:

1. Breadcrumb `← К списку документов`.
2. Title: document name (`Дневник практики` for diary, `Отчёт по практике` for report).
3. Practice context strip: briefcase icon, practice name and module, dates, organization.
4. `Сведения о практике` card: read-only fields in a two-column grid — ФИО студента, группа,
   руководитель от организации, руководитель от учебного учреждения. Read-only fields use a
   gray background.
5. `Дневник практики` card (for diary): editable table with columns `Дата`, `Выполненная работа`,
   `Примечание`; date input, text input, delete row button; `+ Добавить запись`.
6. For report: a large textarea `Содержание отчёта` bound to `Document.Content`.
7. File dropzone: `Прикрепите итоговый отчёт (опционально)`, drag-and-drop, `.docx`/`.pdf`,
   ≤10 MB.
8. Sticky footer: `Сохранить черновик` (secondary) and `Отправить на проверку` (primary, send
   icon). Disable submit while saving or when validation fails.
9. Show Russian validation messages inline and server errors in an alert.

#### `/tasks` — «Задания»

- Cards or list with title, description, deadline, status pill, teacher comment.
- Filter by status.
- Empty state.

#### `/notifications` — «Уведомления»

- List with unread indicator, title, message, date, link to the document.
- `Отметить прочитанным` per item and `Прочитать все`.
- Unread count in the topbar bell and sidebar.

#### `/profile` — «Профиль»

- Read-only card: avatar with initials, full name, group, specialty, student number, phone, email.
- No editing.

#### `/progress` — «Успеваемость»

- Assessment card: grade, result, date, comment; `—` if not assessed.
- Stats: accepted documents / total, completed tasks / total.
- Progress bars using the design tokens.

### 9.4. States and UX

- Loading skeletons for cards and tables.
- Error state with a retry button and a Russian message.
- Empty states with icons.
- Toasts or inline alerts for success/error; do not use `alert()`.
- Disable actions while a request is in flight.
- Confirm destructive actions (delete diary row, regenerate version).
- Keyboard focus visible; semantic HTML; labels for inputs; `aria-live` for toasts.
- Responsive: 1280px desktop, 768px tablet, 375px mobile. Tables scroll horizontally; cards stack.

## 10. Tests and verification

### 10.1. Backend tests

`dotnet test` must pass without Docker. Cover:

- simple token replacement in a paragraph;
- token split across runs;
- token replacement inside a table cell;
- repeating table rows;
- empty repeating block removal;
- footer `PAGE` field preservation;
- unresolved token becomes empty and produces a warning;
- allowed and rejected document status transitions.

Add an integration test for `POST /api/documents/{id}/generate` if PostgreSQL is available;
otherwise mark it skipped with a clear reason.

### 10.2. Frontend checks

- `npm run build` passes.
- `npm run lint` passes.
- No TypeScript `any` in application code unless justified.
- No console errors on the main screens.

### 10.3. End-to-end smoke test

If browser automation is available, walk through:

1. Open `http://localhost:3000` → redirected to `/practice`.
2. Verify the seeded practice, documents, deadlines, and notifications.
3. Open the returned characteristic → see the teacher comment.
4. Open the diary → edit a row → save draft → submit → status becomes `На проверке`.
5. Open the report → generate from template → download → verify the DOCX.
6. Upload a DOCX/PDF → verify a new version appears and the old one is still downloadable.
7. Open `/tasks`, `/notifications`, `/profile`, `/progress` → no errors.
8. Resize to 768px → layout remains usable.

Take screenshots and save them under `docs/screenshots/` if possible.

### 10.4. DOCX verification

Run `tools/verify_docx.py` on every generated file and assert:

- no `{{` remains;
- expected demo strings are present;
- `word/footer*.xml` still contains a `PAGE` field;
- page size and margins match the source;
- diary row count equals the number of entries plus the header;
- the file opens as a valid ZIP with `[Content_Types].xml`.

## 11. README and documentation

`README.md` must contain:

- prerequisites (.NET 8 targeting pack or SDK, Node.js 20+, Docker);
- exact commands to start the database, backend, and frontend;
- URLs and ports;
- demo data summary;
- API endpoint list;
- template syntax and how to add a new template;
- assumptions and out-of-scope items;
- troubleshooting (port in use, DB not ready, migration errors);
- how to reset the database.

## 12. Definition of Done

- [ ] `docker compose up -d db` starts PostgreSQL.
- [ ] `dotnet build` and `dotnet test` pass.
- [ ] `npm run build` and `npm run lint` pass.
- [ ] Backend runs at `http://localhost:5080`; Swagger and `/health` work.
- [ ] Frontend runs at `http://localhost:3000`; no login screen; `/practice` shows seed data.
- [ ] Six DOCX templates exist with manifests.
- [ ] Report, diary, characteristic, and individual plan generate valid DOCX files.
- [ ] Placeholders are replaced; formatting, tables, headers, footers, and page numbers survive.
- [ ] Upload DOCX/PDF creates a new immutable version; old versions remain downloadable.
- [ ] Submit changes `Draft` → `Submitted`; invalid transitions return `409`.
- [ ] Returned documents show the teacher comment and accept a corrected version.
- [ ] UI matches the TZ mockups: palette, cards, pills, sidebar, typography, tables.
- [ ] Responsive at 1280px, 768px, and 375px.
- [ ] README is complete; assumptions are documented.
- [ ] No authentication, admin, teacher, or organization UI is present.

## 13. Execution order

1. Read the TZ and the three report samples. Extract the TZ text and the embedded mockups to
   `docs/source/`. Use figures 7 and 8 as the visual reference.
2. Use the existing `backend/` and `frontend/` folders; create `templates/`, `tools/`, and
   `docs/source/`, and copy the source DOCX files into `docs/source/`.
3. Verify the existing `PracticeManager.Api` solution and the Next.js app both build before
   changing them.
4. Implement the domain model, `AppDbContext`, migration, and seed.
5. Implement the DOCX renderer and its unit tests.
6. Create the six templates and manifests with `tools/normalize_templates.py`; verify generation.
7. Implement the API endpoints and status policy.
8. Implement the frontend shell, design system, and pages.
9. Run the full stack; fix build, runtime, and UI issues.
10. Run tests, the smoke test, and DOCX verification.
11. Write `README.md` and a final report with commands, results, screenshots, assumptions, and
    known limitations.

## 14. Final report format

When finished, report in the chat:

- what was built and where;
- exact commands to run;
- test and build results;
- generated DOCX paths and verification output;
- screenshots or a description of the verified screens;
- assumptions made where the TZ was silent;
- known limitations and what is intentionally out of scope.

<!-- PROMPT END -->

---

## Приложение A. Как использовать промпт

1. Проект уже создан в `C:\dev\opencode\PracticeManager`. Внутри две обязательные папки:
   `backend/` (ASP.NET Core 8, проект `PracticeManager.Api`) и `frontend/` (Next.js App Router).
   Скопируйте четыре исходных DOCX в `PracticeManager/docs/source/`.
2. MCP-серверы уже установлены глобально (`microsoft-learn`, `context7`, `playwright`;
   `dbhub` добавлен выключенным до запуска PostgreSQL). Skills лежат в
   `PracticeManager/.opencode/skills/`.
3. Дайте агенту промпт целиком, начиная с `# Build Task` и заканчивая разделом 14.
4. Агент дополнит существующие `backend/` и `frontend/`, создаст шаблоны, API и UI.
5. Проверьте приёмку по чек-листу из раздела 12 промпта.

Минимальные команды после генерации:

```bash
cd C:\dev\opencode\PracticeManager
docker compose up -d db
cd backend && dotnet run --project PracticeManager.Api
# в другом терминале
cd frontend && npm install && npm run dev
```

Ожидаемые адреса:

- frontend: `http://localhost:3000`
- backend: `http://localhost:5080`
- Swagger: `http://localhost:5080/swagger`
- health: `http://localhost:5080/health`

## Приложение B. Что осталось за рамками прототипа

Авторизация и роли, кабинеты учебной части/преподавателя/организации, договоры, импорт Excel,
массовая генерация документов, PDF-генерация, email-уведомления, аудит, интеграции, архив
прошлых лет, электронное согласование. Эти пункты перечислены в ТЗ как отдельные этапы; их
следует добавлять после проверки студенческого сценария.
