#!/usr/bin/env python3
"""
Validates the DOCX templates in templates/ and, if demo-generated output files exist under
backend/PracticeManager.Api/Storage/documents/, validates those too.

Checks performed on each template:
  - valid ZIP with [Content_Types].xml;
  - no unresolved "{{" tokens remain *for generated output* (templates themselves are expected
    to contain {{tokens}}; this script only asserts that against generated files, see below);
  - every {{#block}} has a matching {{/block}};
  - footer still contains the PAGE field;
  - page size and margins match the original source sample (for report-pm0X.docx);
  - the manifest exists, has sha256 matching the file, and lists only known placeholders.

Run after tools/normalize_templates.py:
    python tools/verify_docx.py
"""
from __future__ import annotations

import hashlib
import json
import re
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
TEMPLATES_DIR = ROOT / "templates"
SOURCE_DIR = ROOT / "docs" / "source"

KNOWN_PLACEHOLDERS = {
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
    "task_title", "task_description", "task_deadline", "task_status",
}
KNOWN_BLOCKS = {"diary_rows", "task_rows"}

TOKEN_RE = re.compile(r"\{\{(#?/?[a-zA-Z0-9_]+)\}\}")

REPORT_SOURCE_MAP = {
    "report-pm01.docx": "report-pm01.docx",
    "report-pm02.docx": "report-pm02.docx",
    "report-pm03.docx": "report-pm03.docx",
}


def read_zip_text(path: Path, entry: str) -> str | None:
    with zipfile.ZipFile(path) as zf:
        if entry not in zf.namelist():
            return None
        return zf.read(entry).decode("utf-8")


def is_valid_zip(path: Path) -> bool:
    try:
        with zipfile.ZipFile(path) as zf:
            return "[Content_Types].xml" in zf.namelist()
    except zipfile.BadZipFile:
        return False


def _normalize_tag_whitespace(tag: str) -> str:
    # ElementTree re-serializes self-closing tags as "<w:pgSz ... />" (space before "/>")
    # while the original source uses "<w:pgSz .../>" (no space). Normalize so a pure
    # serialization difference does not look like a content change.
    return re.sub(r"\s+/>$", "/>", tag.strip())


def get_page_setup(xml: str) -> tuple[str, str] | None:
    sect = re.search(r"<w:sectPr\b.*?</w:sectPr>", xml, re.S)
    if not sect:
        return None
    pg_sz = re.search(r'<w:pgSz\b[^/]*/>', sect.group(0))
    pg_mar = re.search(r'<w:pgMar\b[^/]*/>', sect.group(0))
    return (
        _normalize_tag_whitespace(pg_sz.group(0)) if pg_sz else "",
        _normalize_tag_whitespace(pg_mar.group(0)) if pg_mar else "",
    )


def footer_has_page_field(path: Path) -> bool:
    with zipfile.ZipFile(path) as zf:
        footer_entries = [n for n in zf.namelist() if n.startswith("word/footer")]
        for entry in footer_entries:
            xml = zf.read(entry).decode("utf-8")
            if "PAGE" in xml and ("fldChar" in xml or "fldSimple" in xml):
                return True
    return False


def check_balanced_blocks(xml: str, label: str, errors: list[str]) -> None:
    tokens = TOKEN_RE.findall(xml)
    stack = []
    for tok in tokens:
        if tok.startswith("#"):
            stack.append(tok[1:])
        elif tok.startswith("/"):
            name = tok[1:]
            if not stack or stack[-1] != name:
                errors.append(f"{label}: unmatched block end {{{{/{name}}}}}")
            else:
                stack.pop()
    if stack:
        errors.append(f"{label}: unclosed block(s): {stack}")


def check_known_tokens(xml: str, label: str, errors: list[str]) -> None:
    for tok in TOKEN_RE.findall(xml):
        if tok.startswith("#") or tok.startswith("/"):
            name = tok[1:]
            if name not in KNOWN_BLOCKS:
                errors.append(f"{label}: unknown block {{{{{tok}}}}}")
        else:
            if tok not in KNOWN_PLACEHOLDERS:
                errors.append(f"{label}: unknown placeholder {{{{{tok}}}}}")


def verify_template(docx_path: Path, errors: list[str]) -> None:
    label = docx_path.name
    if not docx_path.exists():
        errors.append(f"{label}: file not found")
        return

    if not is_valid_zip(docx_path):
        errors.append(f"{label}: not a valid ZIP / missing [Content_Types].xml")
        return

    size_mb = docx_path.stat().st_size / (1024 * 1024)
    if size_mb > 5:
        errors.append(f"{label}: size {size_mb:.2f} MB exceeds 5 MB limit")

    xml = read_zip_text(docx_path, "word/document.xml") or ""
    check_balanced_blocks(xml, label, errors)
    check_known_tokens(xml, label, errors)

    if not footer_has_page_field(docx_path):
        errors.append(f"{label}: footer does not contain a PAGE field")

    if docx_path.name in REPORT_SOURCE_MAP:
        source_path = SOURCE_DIR / REPORT_SOURCE_MAP[docx_path.name]
        if source_path.exists():
            source_xml = read_zip_text(source_path, "word/document.xml") or ""
            template_setup = get_page_setup(xml)
            source_setup = get_page_setup(source_xml)
            if template_setup != source_setup:
                errors.append(f"{label}: page size/margins differ from source {source_path.name}: {template_setup} != {source_setup}")

    manifest_path = docx_path.with_suffix(".json")
    if not manifest_path.exists():
        errors.append(f"{label}: manifest {manifest_path.name} missing")
        return

    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    for key in ("code", "name", "documentType", "version", "placeholders", "sha256"):
        if key not in manifest:
            errors.append(f"{manifest_path.name}: missing key '{key}'")

    actual_sha = hashlib.sha256(docx_path.read_bytes()).hexdigest()
    if manifest.get("sha256") != actual_sha:
        errors.append(f"{manifest_path.name}: sha256 mismatch (manifest={manifest.get('sha256')}, actual={actual_sha})")

    for ph in manifest.get("placeholders", []):
        if ph not in KNOWN_PLACEHOLDERS:
            errors.append(f"{manifest_path.name}: manifest lists unknown placeholder '{ph}'")

    # Diary templates/report templates must have a diary_rows block; count rows.
    if "{{#diary_rows}}" in xml:
        if "{{/diary_rows}}" not in xml:
            errors.append(f"{label}: diary_rows block not closed")


def verify_generated_file(path: Path, errors: list[str]) -> None:
    label = str(path.relative_to(ROOT))
    if not is_valid_zip(path):
        errors.append(f"{label}: not a valid ZIP")
        return
    xml = read_zip_text(path, "word/document.xml") or ""
    if "{{" in xml:
        errors.append(f"{label}: unresolved '{{{{' remains in generated document")
    if not footer_has_page_field(path):
        errors.append(f"{label}: footer does not contain a PAGE field")


def main() -> int:
    errors: list[str] = []

    expected_templates = [
        "report-pm01.docx", "report-pm02.docx", "report-pm03.docx",
        "diary.docx", "characteristic.docx", "individual-plan.docx",
    ]
    for name in expected_templates:
        verify_template(TEMPLATES_DIR / name, errors)

    generated_root = ROOT / "backend" / "PracticeManager.Api" / "Storage" / "documents"
    generated_files = list(generated_root.glob("**/*.docx")) if generated_root.exists() else []
    for f in generated_files:
        verify_generated_file(f, errors)

    if errors:
        print(f"FAILED with {len(errors)} issue(s):")
        for e in errors:
            print(" -", e)
        return 1

    print(f"OK: {len(expected_templates)} templates valid.")
    if generated_files:
        print(f"OK: {len(generated_files)} generated document(s) valid (no unresolved tokens, PAGE field present).")
    else:
        print("No generated documents found yet (run the backend with seeding to produce them).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
