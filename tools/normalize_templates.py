#!/usr/bin/env python3
"""
Normalizes the three filled ПМ.01/02/03 report samples into DOCX templates with
{{snake_case}} placeholders, and derives diary.docx, characteristic.docx, and
individual-plan.docx from the same structure.

Design:
  - Operates on word/document.xml with the standard library's xml.etree.ElementTree,
    treating each <w:p> (paragraph) as the unit of replacement: all <w:t> runs in a
    paragraph are joined into one string, literal substitutions are applied, and the
    result is written back into the first run of the paragraph (remaining runs in the
    paragraph are cleared). This keeps one token per paragraph as required by the
    template rules, while tolerating the source runs being split arbitrarily.
  - The diary table (last table in the document) is converted to a repeating block:
    a start-marker row, one template row, an end-marker row.
  - Never edits the source files in place: reads from docs/source/, writes new files
    to templates/.
  - No macros, comments, tracked changes, external links are introduced; the sources
    already have none (verified separately).

Usage:
    python tools/normalize_templates.py
"""
from __future__ import annotations

import hashlib
import json
import re
import zipfile
from dataclasses import dataclass
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SOURCE_DIR = ROOT / "docs" / "source"
TEMPLATES_DIR = ROOT / "templates"

W_NS = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
NSMAP = {"w": W_NS}


def qn(tag: str) -> str:
    return f"{{{W_NS}}}{tag}"


# ---------------------------------------------------------------------------
# Generic DOCX paragraph text helpers
# ---------------------------------------------------------------------------

import xml.etree.ElementTree as ET

# Register the w: namespace so ET.tostring emits w:... instead of ns0:...
ET.register_namespace("w", W_NS)


def iter_paragraphs(root: ET.Element):
    yield from root.iter(qn("p"))


def paragraph_text(p: ET.Element) -> str:
    return "".join(t.text or "" for t in p.iter(qn("t")))


def set_paragraph_text(p: ET.Element, new_text: str) -> None:
    """Writes new_text into the paragraph's first run's <w:t>, clearing all other
    <w:t> elements in the paragraph (their parent <w:r> is left in place so that run
    properties such as bold/italic toggles elsewhere are unaffected, but text is empty).
    If the paragraph has no runs at all, a plain run is appended.
    """
    texts = list(p.iter(qn("t")))
    if not texts:
        r = ET.SubElement(p, qn("r"))
        t_el = ET.SubElement(r, qn("t"))
        t_el.text = new_text
        t_el.set(f"{{http://www.w3.org/XML/1998/namespace}}space", "preserve")
        return
    texts[0].text = new_text
    texts[0].set(f"{{http://www.w3.org/XML/1998/namespace}}space", "preserve")
    for t in texts[1:]:
        t.text = ""


def replace_literal(text: str, mapping: list[tuple[str, str]]) -> str:
    for old, new in mapping:
        text = text.replace(old, new)
    return text


# "практике от предприятия<Имя> [trailing punctuation/space variants]" appears several times
# with inconsistent spacing/punctuation across the three samples (e.g. with or without a space
# after "предприятия", with/without a trailing period). A regex is more robust here than a list
# of exact literal strings for every variant.
SUPERVISOR_SIGNATURE_RE = re.compile(
    r"практике от предприятия\s*Глаговский В\. ?А\.?\s*"
)
SUPERVISOR_SIGNATURE_FULL_RE = re.compile(
    r"от предприятия Владимир Анатольевич Глаговский\s*"
)


def replace_signature_lines(text: str, organization_supervisor_token: str, organization_supervisor_full_token: str) -> str:
    text = SUPERVISOR_SIGNATURE_FULL_RE.sub(f"от предприятия {organization_supervisor_full_token} ", text)
    text = SUPERVISOR_SIGNATURE_RE.sub(f"практике от предприятия {organization_supervisor_token} ", text)
    return text


# ---------------------------------------------------------------------------
# Template token values (placeholders only, never literal demo data)
# ---------------------------------------------------------------------------

TOKENS = {
    "college_name": "{{college_name}}",
    "college_address": "{{college_address}}",
    "city": "{{city}}",
    "year": "{{year}}",
    "student_full_name": "{{student_full_name}}",
    "student_short_name": "{{student_short_name}}",
    "student_dative_full_name": "{{student_dative_full_name}}",
    "student_genitive_full_name": "{{student_genitive_full_name}}",
    "student_group": "{{student_group}}",
    "student_number": "{{student_number}}",
    "student_course": "{{student_course}}",
    "student_phone": "{{student_phone}}",
    "student_email": "{{student_email}}",
    "specialty_code": "{{specialty_code}}",
    "specialty_name": "{{specialty_name}}",
    "practice_type": "{{practice_type}}",
    "practice_name": "{{practice_name}}",
    "professional_module": "{{professional_module}}",
    "practice_hours": "{{practice_hours}}",
    "date_start": "{{date_start}}",
    "date_end": "{{date_end}}",
    "practice_period": "{{practice_period}}",
    "organization_name": "{{organization_name}}",
    "organization_address": "{{organization_address}}",
    "organization_inn": "{{organization_inn}}",
    "organization_supervisor": "{{organization_supervisor}}",
    "organization_supervisor_full_name": "{{organization_supervisor_full_name}}",
    "organization_supervisor_position": "{{organization_supervisor_position}}",
    "college_supervisor": "{{college_supervisor}}",
    "college_supervisor_full_name": "{{college_supervisor_full_name}}",
    "college_supervisor_position": "{{college_supervisor_position}}",
    "assessment_grade": "{{assessment_grade}}",
    "assessment_result": "{{assessment_result}}",
    "assessment_date": "{{assessment_date}}",
    "assessment_comment": "{{assessment_comment}}",
    "report_content": "{{report_content}}",
    "characteristic_text": "{{characteristic_text}}",
    "review_text": "{{review_text}}",
}


@dataclass
class ReportSource:
    file_name: str
    professional_module: str
    pm_code: str
    # Literal strings (as they appear joined across runs) that must be replaced.
    # Matched longest-first to avoid partial overlaps.
    literal_map: list[tuple[str, str]]


def build_literal_map(pm_full_module: str) -> list[tuple[str, str]]:
    """Common literal replacements shared by all three report samples."""
    return [
        # College header (two paragraphs in the title-page table)
        (
            "АВТОНОМНАЯ НЕКОММЕРЧЕСКАЯ ОРГАНИЗАЦИЯ ПРОФЕССИОНАЛЬНАЯ",
            TOKENS["college_name"],
        ),
        (
            "ОБРАЗОВАТЕЛЬНАЯ ОРГАНИЗАЦИЯ МОСКОВСКИЙ МЕЖДУНАРОДНЫЙ КОЛЛЕДЖ ЦИФРОВЫХ ТЕХНОЛОГИЙ «АКАДЕМИЯ ТОП»",
            "",
        ),
        (
            "Юридический адрес: 121170, г. Москва, вн.тер.г. муниципальный округ Дорогомилово,",
            TOKENS["college_address"],
        ),
        (
            "пр-кт Кутузовский, д.36, стр. 2, этаж I, помещение/ комната I/12, тел.: +7(495)151-19-01, ИНН 7730265193КПП 773001001ОГРН 1217700253594",
            "",
        ),
        # Title page: specialty / module already generic text + code, keep "о прохождении..." line
        ("09.02.07 Информационные системы и программирование", f"{TOKENS['specialty_code']} {TOKENS['specialty_name']}"),
        ("профессиональный модуль:", "профессиональный модуль:"),
        (pm_full_module, TOKENS["professional_module"]),
        # Author / group / grade / supervisor / organization / dates / city-year
        ("Выполнил: Перепичка Д. К. Группа:9\\3-РПО-23\\3", f"Выполнил: {TOKENS['student_short_name']} Группа: {TOKENS['student_group']}"),
        ("Оценка: Руководитель практики:", f"Оценка: {TOKENS['assessment_grade']} Руководитель практики:"),
        ("Пузиков Денис Владимирович", TOKENS["college_supervisor_full_name"]),
        (
            "Практика пройдена в Министерство Социальной Политики Калининградской области Дата прохождения: 27.04.2026 г. – 15.05.2026 г.",
            f"Практика пройдена в {TOKENS['organization_name']} Дата прохождения: {TOKENS['practice_period']}",
        ),
        (
            "Практика пройдена в Министерство Социальной Политики Калининградской области Дата прохождения: 18.05.2026 г. – 05.06.2026 г.",
            f"Практика пройдена в {TOKENS['organization_name']} Дата прохождения: {TOKENS['practice_period']}",
        ),
        (
            "Практика пройдена в Министерство Социальной Политики Калининградской области Дата прохождения: 08.06.2026 г. – 19.06.2026 г.",
            f"Практика пройдена в {TOKENS['organization_name']} Дата прохождения: {TOKENS['practice_period']}",
        ),
        ("Калининград 2026", f"{TOKENS['city']} {TOKENS['year']}"),
        # ЗАДАНИЕ section
        (
            "Выдано студенту Перепичка Данилу Константиновичу группы 9\\3-РПО-23\\3 Автономной некоммерческой организации профессиональной организации Московского международного колледжа цифровых технологий",
            f"Выдано студенту {TOKENS['student_dative_full_name']} группы {TOKENS['student_group']} {TOKENS['college_name']}",
        ),
        ("«Академия ТОП».", ""),
        ("Программа подготовки 09.02.07 Информационные системы и программирование", f"Программа подготовки {TOKENS['specialty_code']} {TOKENS['specialty_name']}"),
        ("Вид практики: Производственная", f"Вид практики: {TOKENS['practice_type']}"),
        ("Срок прохождения: 27.04.2026 г. – 15.05.2026 г.", f"Срок прохождения: {TOKENS['practice_period']}"),
        ("Срок прохождения: 18.05.2026 г. – 05.06.2026 г.", f"Срок прохождения: {TOKENS['practice_period']}"),
        ("Срок прохождения: 08.06.2026 г. – 19.06.2026 г.", f"Срок прохождения: {TOKENS['practice_period']}"),
        ("от колледжа Пузиков Денис Владимирович ", f"от колледжа {TOKENS['college_supervisor_full_name']} "),
        ("к исполнению Перепичка Данил Константинович", f"к исполнению {TOKENS['student_full_name']}"),
        # АТТЕСТАЦИОННЫЙ ЛИСТ intro paragraph (student + org identification)
        (
            "Студент, Перепичка Д. К. 9\\3-РПО-23\\3, во время прохождения производственной практики в Министерство Социальной Политики Калининградской области, проявил следующий уровень",
            f"Студент, {TOKENS['student_short_name']} {TOKENS['student_group']}, во время прохождения производственной практики в {TOKENS['organization_name']}, проявил следующий уровень",
        ),
        # ХАРАКТЕРИСТИКА ОБЪЕКТА ПРАКТИКИ — whole body becomes empty (object characteristic is
        # not in the token dictionary for the report template; keep heading, drop sample text).
        # ХАРАКТЕРИСТИКА (обучающегося) block
        ("Перепичка Д. К. Группа РПО 23-3", f"{TOKENS['student_short_name']} Группа {TOKENS['student_group']}"),
        ("Даты прохождения практики: 27.04.26 – 15.05.26", f"Даты прохождения практики: {TOKENS['practice_period']}"),
        ("Даты прохождения практики: 18.05.26 – 05.06.26", f"Даты прохождения практики: {TOKENS['practice_period']}"),
        ("Даты прохождения практики: 08.06.26 – 19.06.26", f"Даты прохождения практики: {TOKENS['practice_period']}"),
        ("Руководитель по производственной", "Руководитель по производственной"),
        # ОТЗЫВ РУКОВОДИТЕЛЯ ПРАКТИКИ block
        (
            "Перепичка Данил Константинович студент 3 курса, группы РПО-23-3 проходил производственную практику",
            f"{TOKENS['student_full_name']} студент {TOKENS['student_course']} курса, группы {TOKENS['student_group']} проходил производственную практику",
        ),
        ("Министерство Социальной Политики Калининградской обл.", TOKENS["organization_name"]),
    ]


REPORTS = [
    ReportSource("report-pm01.docx", "ПМ.01 Разработка модулей программного обеспечения для компьютерных систем", "pm01", []),
    ReportSource("report-pm02.docx", "ПМ.02 Осуществление интеграции программных модулей", "pm02", []),
    ReportSource("report-pm03.docx", "ПМ.03 Сопровождение и обслуживание программного обеспечения компьютерных систем", "pm03", []),
]

# Paragraph text (as sampled from each source) that marks the start of a long free-text
# section whose entire body (until the next heading) must collapse into a single token
# paragraph. Identified by the heading text just above the body.
FREE_TEXT_SECTIONS = {
    "ХАРАКТЕРИСТИКА ОБЪЕКТА ПРАКТИКИ": None,  # dropped: no token in dictionary for this section
    "ОТЧЕТ О ПРОДЕЛАННОЙ РАБОТЕ": TOKENS["report_content"],
    "ХАРАКТЕРИСТИКА": TOKENS["characteristic_text"],
    "ОТЗЫВ РУКОВОДИТЕЛЯ ПРАКТИКИ": TOKENS["review_text"],
}

# Headings that terminate a free-text section (the next structural heading).
SECTION_HEADINGS = [
    "ЗАДАНИЕ",
    "АТТЕСТАЦИОННЫЙ ЛИСТ",
    "ДНЕВНИК",
    "ХАРАКТЕРИСТИКА ОБЪЕКТА ПРАКТИКИ",
    "ОТЧЕТ О ПРОДЕЛАННОЙ РАБОТЕ",
    "ХАРАКТЕРИСТИКА",
    "ОТЗЫВ РУКОВОДИТЕЛЯ ПРАКТИКИ",
]


def is_heading_paragraph(text: str, heading: str) -> bool:
    return text.strip() == heading


def collapse_free_text_sections(body: ET.Element) -> None:
    """Finds each tracked heading, then collapses every paragraph until the next
    structural heading (or a table) into a single paragraph holding the section token
    (or removes the section entirely when the token is None, e.g. object characteristic).
    Signature paragraphs that follow directly after certain bodies (e.g. "Руководитель
    по...") are preserved because they match a SECTION_HEADINGS/known-signature guard.
    """
    paragraphs = list(body.iter(qn("p")))

    # Known literal signature-line starts that must NOT be swallowed into the free text.
    signature_starts = (
        "Руководитель по",
        "Руководитель практики",
        "практике от предприятия",
        "от колледжа",
        "(подпись)",
    )

    for heading, token in FREE_TEXT_SECTIONS.items():
        # Find heading paragraph (there may be more than one occurrence pattern, so locate
        # by exact text match on the *trimmed* joined text).
        heading_index = None
        for i, p in enumerate(paragraphs):
            if is_heading_paragraph(paragraph_text(p), heading):
                heading_index = i
                break
        if heading_index is None:
            continue

        start = heading_index + 1
        end = start
        while end < len(paragraphs):
            txt = paragraph_text(paragraphs[end]).strip()
            if not txt:
                end += 1
                continue
            if any(txt == h for h in SECTION_HEADINGS if h != heading):
                break
            if any(txt.startswith(s) for s in signature_starts):
                break
            if paragraphs[end].find(f"../{qn('tbl')}") is not None:
                break
            end += 1

        body_paragraphs = paragraphs[start:end]
        if not body_paragraphs:
            continue

        first = body_paragraphs[0]
        if token is None:
            set_paragraph_text(first, "")
        else:
            set_paragraph_text(first, token)

        for p in body_paragraphs[1:]:
            parent_map = {c: p2 for p2 in body.iter() for c in p2}
            parent = parent_map.get(p)
            if parent is not None:
                parent.remove(p)


def apply_literal_substitutions(body: ET.Element, literal_map: list[tuple[str, str]]) -> None:
    for p in list(body.iter(qn("p"))):
        original = paragraph_text(p)
        if not original.strip():
            continue
        replaced = replace_literal(original, literal_map)
        replaced = replace_signature_lines(replaced, TOKENS["organization_supervisor"], TOKENS["organization_supervisor_full_name"])
        if replaced != original:
            set_paragraph_text(p, replaced)


def convert_diary_table_to_block(body: ET.Element) -> None:
    """Finds the diary table (the one whose header row says Дата | Выполняемая работа |
    Отметка...) and replaces its data rows with a 3-row {{#diary_rows}} block template.
    """
    tables = list(body.iter(qn("tbl")))
    diary_table = None
    for t in tables:
        rows = t.findall(qn("tr"))
        if not rows:
            continue
        header_text = "".join(paragraph_text(p) for p in rows[0].iter(qn("p")))
        if "Дата" in header_text and "Выполняемая работа" in header_text:
            diary_table = t
            break

    if diary_table is None:
        raise RuntimeError("Diary table not found in source document")

    rows = diary_table.findall(qn("tr"))
    header_row = rows[0]
    data_rows = rows[1:]
    if not data_rows:
        raise RuntimeError("Diary table has no data rows to use as a template")

    sample_row = data_rows[0]
    cells = sample_row.findall(qn("tc"))
    if len(cells) < 3:
        raise RuntimeError("Diary template row does not have 3 cells")

    set_cell_text(cells[0], TOKENS_DIARY["diary_date"])
    set_cell_text(cells[1], TOKENS_DIARY["diary_work"])
    set_cell_text(cells[2], TOKENS_DIARY["diary_mark"])

    start_row = make_marker_row(sample_row, "{{#diary_rows}}", len(cells))
    end_row = make_marker_row(sample_row, "{{/diary_rows}}", len(cells))

    for row in data_rows[1:]:
        diary_table.remove(row)

    diary_table.remove(sample_row)
    header_index = list(diary_table).index(header_row)
    diary_table.insert(header_index + 1, start_row)
    diary_table.insert(header_index + 2, sample_row)
    diary_table.insert(header_index + 3, end_row)


TOKENS_DIARY = {
    "diary_date": "{{diary_date}}",
    "diary_work": "{{diary_work}}",
    "diary_mark": "{{diary_mark}}",
}

TOKENS_TASK = {
    "task_title": "{{task_title}}",
    "task_description": "{{task_description}}",
    "task_deadline": "{{task_deadline}}",
    "task_status": "{{task_status}}",
}


def set_cell_text(cell: ET.Element, text: str) -> None:
    paragraphs = cell.findall(qn("p"))
    if not paragraphs:
        raise RuntimeError("Table cell has no paragraphs")
    set_paragraph_text(paragraphs[0], text)
    for p in paragraphs[1:]:
        cell.remove(p)


def make_marker_row(template_row: ET.Element, marker_text: str, cell_count: int) -> ET.Element:
    row = ET.fromstring(ET.tostring(template_row))
    cells = row.findall(qn("tc"))
    set_cell_text(cells[0], marker_text)
    for c in cells[1:]:
        set_cell_text(c, "")
    return row


# ---------------------------------------------------------------------------
# DOCX read/write
# ---------------------------------------------------------------------------


def load_document_xml(docx_path: Path) -> tuple[ET.Element, dict[str, bytes]]:
    entries: dict[str, bytes] = {}
    with zipfile.ZipFile(docx_path) as zf:
        for name in zf.namelist():
            entries[name] = zf.read(name)
    root = ET.fromstring(entries["word/document.xml"])
    return root, entries


def save_docx(entries: dict[str, bytes], root: ET.Element, out_path: Path) -> None:
    entries = dict(entries)
    entries["word/document.xml"] = b'<?xml version="1.0" encoding="UTF-8" standalone="yes"?>\r\n' + ET.tostring(
        root, encoding="utf-8", xml_declaration=False
    )
    out_path.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(out_path, "w", zipfile.ZIP_DEFLATED) as zf:
        for name, data in entries.items():
            zf.writestr(name, data)


def sha256_of(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


# ---------------------------------------------------------------------------
# Template builders
# ---------------------------------------------------------------------------

REPORT_PLACEHOLDERS = [
    "college_name", "college_address", "city", "year",
    "student_full_name", "student_short_name", "student_dative_full_name", "student_genitive_full_name",
    "student_group", "student_number", "student_course", "student_phone", "student_email",
    "specialty_code", "specialty_name",
    "practice_type", "practice_name", "professional_module", "practice_hours",
    "date_start", "date_end", "practice_period",
    "organization_name", "organization_address", "organization_inn",
    "organization_supervisor", "organization_supervisor_full_name", "organization_supervisor_position",
    "college_supervisor", "college_supervisor_full_name", "college_supervisor_position",
    "assessment_grade", "assessment_result", "assessment_date", "assessment_comment",
    "report_content", "characteristic_text", "review_text",
    "diary_date", "diary_work", "diary_mark",
]


def build_report_template(source: ReportSource) -> None:
    src_path = SOURCE_DIR / source.file_name
    root, entries = load_document_xml(src_path)
    body = root.find(qn("body"))
    assert body is not None

    literal_map = build_literal_map(source.professional_module)
    apply_literal_substitutions(body, literal_map)
    collapse_free_text_sections(body)
    convert_diary_table_to_block(body)

    out_path = TEMPLATES_DIR / f"report-{source.pm_code}.docx"
    save_docx(entries, root, out_path)
    write_manifest(
        code=f"report-{source.pm_code}",
        name=f"Отчёт по практике ({source.professional_module.split(' ')[0]})",
        document_type="report",
        professional_module=source.professional_module.split(" ", 1)[0],
        placeholders=REPORT_PLACEHOLDERS,
        docx_path=out_path,
    )
    print(f"Built {out_path.relative_to(ROOT)}")


DIARY_PLACEHOLDERS = [
    "college_name", "student_full_name", "student_group",
    "practice_name", "practice_period", "organization_name",
    "organization_supervisor_full_name", "college_supervisor_full_name",
    "diary_date", "diary_work", "diary_mark",
]


def build_diary_template() -> None:
    """Derives diary.docx from report-pm01's document.xml, keeping the title info and the
    diary table but dropping every other section (assignment, attestation, report body,
    characteristic, review).
    """
    src_path = SOURCE_DIR / "report-pm01.docx"
    root, entries = load_document_xml(src_path)
    body = root.find(qn("body"))
    assert body is not None

    literal_map = build_literal_map(REPORTS[0].professional_module)
    apply_literal_substitutions(body, literal_map)
    convert_diary_table_to_block(body)

    # Keep only paragraphs/tables between "ДНЕВНИК" heading (inclusive) and the table that
    # follows it (inclusive), dropping everything else in the body.
    children = list(body)
    diary_heading_idx = next(i for i, c in enumerate(children) if c.tag == qn("p") and paragraph_text(c).strip() == "ДНЕВНИК")
    diary_subtitle_idx = diary_heading_idx + 1  # "по производственной практике"
    diary_table_idx = next(i for i in range(diary_subtitle_idx, len(children)) if children[i].tag == qn("tbl"))
    # signature paragraphs right after the diary table ("Руководитель по" / "практике от предприятия...")
    sig_end = diary_table_idx + 1
    while sig_end < len(children) and children[sig_end].tag == qn("p") and paragraph_text(children[sig_end]).strip():
        sig_end += 1

    sect_pr = body.find(qn("sectPr"))

    keep = children[diary_heading_idx:sig_end]

    for c in list(body):
        body.remove(c)

    header_para = make_simple_paragraph("{{college_name}}")
    title_para = make_simple_paragraph("ДНЕВНИК практики {{student_full_name}}, группа {{student_group}}")
    meta_para = make_simple_paragraph("{{practice_name}}, {{practice_period}}, {{organization_name}}")

    body.append(header_para)
    body.append(title_para)
    body.append(meta_para)
    for c in keep:
        body.append(c)
    if sect_pr is not None:
        body.append(sect_pr)

    out_path = TEMPLATES_DIR / "diary.docx"
    save_docx(entries, root, out_path)
    write_manifest(
        code="diary",
        name="Дневник практики",
        document_type="diary",
        professional_module=None,
        placeholders=DIARY_PLACEHOLDERS,
        docx_path=out_path,
    )
    print(f"Built {out_path.relative_to(ROOT)}")


CHARACTERISTIC_PLACEHOLDERS = [
    "college_name", "student_full_name", "student_group",
    "practice_period", "characteristic_text",
    "organization_supervisor_full_name",
]


def build_characteristic_template() -> None:
    src_path = SOURCE_DIR / "report-pm01.docx"
    root, entries = load_document_xml(src_path)
    body = root.find(qn("body"))
    assert body is not None

    literal_map = build_literal_map(REPORTS[0].professional_module)
    apply_literal_substitutions(body, literal_map)

    children = list(body)
    heading_idx = next(i for i, c in enumerate(children) if c.tag == qn("p") and paragraph_text(c).strip() == "ХАРАКТЕРИСТИКА")
    # body runs until "ОТЗЫВ РУКОВОДИТЕЛЯ ПРАКТИКИ"
    end_idx = next(i for i in range(heading_idx, len(children)) if children[i].tag == qn("p") and paragraph_text(children[i]).strip() == "ОТЗЫВ РУКОВОДИТЕЛЯ ПРАКТИКИ")

    sect_pr = body.find(qn("sectPr"))
    keep = children[heading_idx:end_idx]

    for c in list(body):
        body.remove(c)

    header_para = make_simple_paragraph("{{college_name}}")
    meta_para = make_simple_paragraph("{{student_full_name}}, группа {{student_group}}, период практики: {{practice_period}}")

    body.append(header_para)
    body.append(meta_para)
    for c in keep:
        body.append(c)
    if sect_pr is not None:
        body.append(sect_pr)

    # Replace the collapsed free-text body paragraph (first non-heading paragraph after
    # "обучающегося"/meta line) with {{characteristic_text}} — already handled by
    # collapse_free_text_sections pattern reused here manually since this derived doc only
    # has the ХАРАКТЕРИСТИКА section.
    collapse_characteristic_body(body)

    out_path = TEMPLATES_DIR / "characteristic.docx"
    save_docx(entries, root, out_path)
    write_manifest(
        code="characteristic",
        name="Характеристика от организации",
        document_type="characteristic",
        professional_module=None,
        placeholders=CHARACTERISTIC_PLACEHOLDERS,
        docx_path=out_path,
    )
    print(f"Built {out_path.relative_to(ROOT)}")


def collapse_characteristic_body(body: ET.Element) -> None:
    paragraphs = list(body.iter(qn("p")))
    heading_idx = next(i for i, p in enumerate(paragraphs) if paragraph_text(p).strip() == "ХАРАКТЕРИСТИКА")
    # paragraphs after heading: "обучающегося", meta (already replaced), then free text body
    # until signature ("Руководитель по производственной")
    start = heading_idx + 1
    # skip "обучающегося" subtitle and the meta/name paragraphs we already rewrote
    while start < len(paragraphs) and paragraph_text(paragraphs[start]).strip() in ("обучающегося",):
        start += 1
    end = start
    while end < len(paragraphs):
        txt = paragraph_text(paragraphs[end]).strip()
        if txt.startswith("Руководитель по") or not txt:
            if txt.startswith("Руководитель по"):
                break
        end += 1

    body_paragraphs = [p for p in paragraphs[start:end] if paragraph_text(p).strip()]
    if not body_paragraphs:
        return
    set_paragraph_text(body_paragraphs[0], "{{characteristic_text}}")
    parent_map = {c: p2 for p2 in body.iter() for c in p2}
    for p in body_paragraphs[1:]:
        parent = parent_map.get(p)
        if parent is not None:
            parent.remove(p)


INDIVIDUAL_PLAN_PLACEHOLDERS = [
    "college_name", "student_full_name", "student_group",
    "specialty_code", "specialty_name", "practice_type", "practice_period",
    "college_supervisor_full_name", "organization_supervisor_full_name",
    "task_title", "task_description", "task_deadline", "task_status",
]


def make_simple_paragraph(text: str) -> ET.Element:
    p = ET.Element(qn("p"))
    r = ET.SubElement(p, qn("r"))
    t = ET.SubElement(r, qn("t"))
    t.text = text
    t.set(f"{{http://www.w3.org/XML/1998/namespace}}space", "preserve")
    return p


def build_individual_plan_template() -> None:
    """Builds individual-plan.docx from scratch (no source table exists for tasks), reusing
    the title-page table style constants (college header) from report-pm01 for visual
    consistency, and a simple table with {{#task_rows}} block.
    """
    src_path = SOURCE_DIR / "report-pm01.docx"
    root, entries = load_document_xml(src_path)
    body = root.find(qn("body"))
    assert body is not None

    literal_map = build_literal_map(REPORTS[0].professional_module)
    apply_literal_substitutions(body, literal_map)

    children = list(body)
    assignment_idx = next(i for i, c in enumerate(children) if c.tag == qn("p") and paragraph_text(c).strip() == "ЗАДАНИЕ")
    attestation_idx = next(i for i in range(assignment_idx, len(children)) if children[i].tag == qn("p") and paragraph_text(children[i]).strip() == "АТТЕСТАЦИОННЫЙ ЛИСТ")

    sect_pr = body.find(qn("sectPr"))
    assignment_section = children[assignment_idx:attestation_idx]

    for c in list(body):
        body.remove(c)

    body.append(make_simple_paragraph("{{college_name}}"))
    body.append(make_simple_paragraph("ИНДИВИДУАЛЬНЫЙ ПЛАН"))
    body.append(make_simple_paragraph("на {{practice_type}} практику"))
    body.append(make_simple_paragraph("Выдан студенту {{student_dative_full_name}} группы {{student_group}}"))
    body.append(make_simple_paragraph("Программа подготовки {{specialty_code}} {{specialty_name}}"))
    body.append(make_simple_paragraph("Срок прохождения: {{practice_period}}"))
    body.append(make_simple_paragraph("Перечень заданий:"))

    table = build_task_table()
    body.append(table)

    body.append(make_simple_paragraph("Руководитель практики от колледжа {{college_supervisor_full_name}} (подпись)"))
    body.append(make_simple_paragraph("Руководитель практики от предприятия {{organization_supervisor_full_name}} (подпись)"))
    body.append(make_simple_paragraph("Задание принял к исполнению {{student_full_name}} (подпись)"))

    if sect_pr is not None:
        body.append(sect_pr)

    out_path = TEMPLATES_DIR / "individual-plan.docx"
    save_docx(entries, root, out_path)
    write_manifest(
        code="individual-plan",
        name="Индивидуальный план",
        document_type="individual-plan",
        professional_module=None,
        placeholders=INDIVIDUAL_PLAN_PLACEHOLDERS,
        docx_path=out_path,
    )
    print(f"Built {out_path.relative_to(ROOT)}")


def build_task_table() -> ET.Element:
    tbl = ET.Element(qn("tbl"))
    tbl_pr = ET.SubElement(tbl, qn("tblPr"))
    tbl_w = ET.SubElement(tbl_pr, qn("tblW"))
    tbl_w.set(qn("w"), "5000")
    tbl_w.set(qn("type"), "pct")
    borders = ET.SubElement(tbl_pr, qn("tblBorders"))
    for side in ("top", "left", "bottom", "right", "insideH", "insideV"):
        b = ET.SubElement(borders, qn(side))
        b.set(qn("val"), "single")
        b.set(qn("sz"), "4")
        b.set(qn("space"), "0")
        b.set(qn("color"), "auto")

    grid = ET.SubElement(tbl, qn("tblGrid"))
    for w in ("700", "4500", "1500", "1800"):
        gc = ET.SubElement(grid, qn("gridCol"))
        gc.set(qn("w"), w)

    header = make_task_row(["№", "Задание", "Срок", "Статус"], is_header=True)
    header_tr_pr = ET.SubElement(header, qn("trPr"))
    ET.SubElement(header_tr_pr, qn("tblHeader"))
    header.insert(0, header_tr_pr)
    tbl.append(header)

    start_row = make_task_row(["{{#task_rows}}", "", "", ""])
    sample_row = make_task_row(["1", "{{task_title}}", "{{task_deadline}}", "{{task_status}}"])
    end_row = make_task_row(["{{/task_rows}}", "", "", ""])
    tbl.append(start_row)
    tbl.append(sample_row)
    tbl.append(end_row)
    return tbl


def make_task_row(cell_texts: list[str], is_header: bool = False) -> ET.Element:
    tr = ET.Element(qn("tr"))
    if not is_header:
        tr_pr = ET.SubElement(tr, qn("trPr"))
        cant_split = ET.SubElement(tr_pr, qn("cantSplit"))
    for text in cell_texts:
        tc = ET.SubElement(tr, qn("tc"))
        tc_pr = ET.SubElement(tc, qn("tcPr"))
        p = make_simple_paragraph(text)
        if is_header:
            r = p.find(qn("r"))
            r_pr = ET.Element(qn("rPr"))
            b = ET.SubElement(r_pr, qn("b"))
            r.insert(0, r_pr)
        tc.append(p)
    return tr


def write_manifest(code: str, name: str, document_type: str, professional_module: str | None, placeholders: list[str], docx_path: Path) -> None:
    manifest = {
        "code": code,
        "name": name,
        "documentType": document_type,
        "professionalModule": professional_module,
        "version": 1,
        "placeholders": placeholders,
        "sha256": sha256_of(docx_path),
    }
    manifest_path = docx_path.with_suffix(".json")
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main() -> None:
    TEMPLATES_DIR.mkdir(parents=True, exist_ok=True)
    for source in REPORTS:
        build_report_template(source)
    build_diary_template()
    build_characteristic_template()
    build_individual_plan_template()
    print("Done. Run tools/verify_docx.py next.")


if __name__ == "__main__":
    main()
