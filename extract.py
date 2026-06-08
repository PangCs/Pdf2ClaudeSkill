#!/usr/bin/env python3
"""extract.py — PDF → Markdown extractor for Claude skill bundles.

Library: PyMuPDF (fitz) — chosen for:
  - find_tables() for robust table detection without Java dependencies
  - span-level font size / bold flags for accurate heading classification
  - already installed in the project CI pipeline
"""

import argparse
import re
import sys
from pathlib import Path
from statistics import median

import fitz  # PyMuPDF ≥ 1.23


_SECTION_RE = re.compile(
    r"^(?:\d+\.)+\d*\.?\s+\S"             # "1.2 Heading", "1.2.3 Sub", "1. Top"
    r"|^(?:Chapter|Section|Appendix|Part)\s+\w+",
    re.IGNORECASE,
)


# ---------------------------------------------------------------------------
# Table helpers
# ---------------------------------------------------------------------------

def _cell(v) -> str:
    return ("" if v is None else str(v)).replace("\n", " ").replace("|", "\\|")


def table_to_markdown(rows: list[list]) -> str:
    """Convert a list-of-rows (strings/None) to a GFM markdown table."""
    if not rows:
        return ""

    col_count = max(len(r) for r in rows)
    padded = [r + [None] * (col_count - len(r)) for r in rows]
    cells = [[_cell(c) for c in row] for row in padded]

    header = "| " + " | ".join(cells[0]) + " |"
    sep    = "| " + " | ".join(["---"] * col_count) + " |"
    body   = "\n".join("| " + " | ".join(row) + " |" for row in cells[1:])

    return "\n".join(filter(None, [header, sep, body]))


# ---------------------------------------------------------------------------
# Heading detection
# ---------------------------------------------------------------------------

def _heading_level(text: str, size: float, flags: int, body_size: float) -> int | None:
    """Return Markdown heading level (2-4) or None for body text.

    PyMuPDF flags: bit 4 (0x10) = bold.
    Thresholds are relative to the page's median body font size.
    """
    is_bold  = bool(flags & 0x10)
    is_large = size >= body_size * 1.25
    is_section_pattern = bool(_SECTION_RE.match(text.strip()))

    if is_large and is_bold:
        return 2 if size >= body_size * 1.6 else 3
    if is_large:
        return 3
    if is_section_pattern:
        return 4
    return None


# ---------------------------------------------------------------------------
# Per-page extraction
# ---------------------------------------------------------------------------

def _collect_body_sizes(dict_data: dict) -> list[float]:
    sizes = []
    for block in dict_data["blocks"]:
        if block["type"] != 0:
            continue
        for line in block.get("lines", []):
            for span in line.get("spans", []):
                if span["text"].strip():
                    sizes.append(span["size"])
    return sizes


def _inside_any_table(bbox, table_bboxes, tol=2.0) -> bool:
    bx0, by0, bx1, by1 = bbox
    for tx0, ty0, tx1, ty1 in table_bboxes:
        if bx0 >= tx0 - tol and bx1 <= tx1 + tol and by0 >= ty0 - tol and by1 <= ty1 + tol:
            return True
    return False


def extract_page_markdown(page: fitz.Page) -> str:
    """Return a markdown string for all content on this page."""
    # --- tables ---
    tab_finder  = page.find_tables()
    table_bboxes = []
    table_items  = []  # (y0, markdown)
    for tab in tab_finder.tables:
        rows = tab.extract()
        md = table_to_markdown(rows)
        if md:
            table_bboxes.append(tab.bbox)
            table_items.append((tab.bbox[1], md))

    # --- text ---
    dict_data = page.get_text("dict", sort=True)
    sizes = _collect_body_sizes(dict_data)
    body_size = median(sizes) if sizes else 10.0

    text_items = []  # (y0, rendered_str)
    for block in dict_data["blocks"]:
        if block["type"] != 0:
            continue
        if _inside_any_table(block["bbox"], table_bboxes):
            continue

        block_lines = []
        for line in block.get("lines", []):
            parts, max_size, merged_flags = [], 0.0, 0
            for span in line.get("spans", []):
                t = span["text"]
                if t.strip():
                    parts.append(t)
                    max_size     = max(max_size, span["size"])
                    merged_flags |= span["flags"]

            if not parts:
                continue

            line_text = " ".join(parts).strip()
            level = _heading_level(line_text, max_size, merged_flags, body_size)
            block_lines.append(f"\n{'#' * level} {line_text}" if level else line_text)

        if block_lines:
            text_items.append((block["bbox"][1], "\n".join(block_lines)))

    # --- merge in reading order ---
    all_items = (
        [(y, "table", md)  for y, md  in table_items] +
        [(y, "text",  txt) for y, txt in text_items]
    )
    all_items.sort(key=lambda x: x[0])

    parts = []
    for _, kind, content in all_items:
        if kind == "table":
            parts.append("")
            parts.append(content)
            parts.append("")
        else:
            parts.append(content)

    return "\n".join(parts)


# ---------------------------------------------------------------------------
# Document-level
# ---------------------------------------------------------------------------

def build_frontmatter(skill_name: str, skill_description: str, source: str) -> str:
    return "\n".join([
        "---",
        f"skill: {skill_name}",
        f"description: {skill_description}",
        f"source: {source}",
        "---",
        "",
    ])


def extract(pdf_path: Path, output_dir: Path, skill_name: str, skill_description: str) -> Path:
    output_dir.mkdir(parents=True, exist_ok=True)
    out_file = output_dir / f"{pdf_path.stem}.md"

    with fitz.open(str(pdf_path)) as doc:
        page_count = doc.page_count
        parts = [build_frontmatter(skill_name, skill_description, pdf_path.name)]

        for page_num, page in enumerate(doc, start=1):
            parts.append(f"\n---\n\n<!-- Page {page_num} of {page_count} -->\n")
            parts.append(extract_page_markdown(page))

        out_file.write_text("\n".join(parts), encoding="utf-8")

    return out_file


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def main() -> None:
    parser = argparse.ArgumentParser(
        description=(
            "Extract a PDF to Markdown, preserving tables, page numbers, "
            "and section/heading structure."
        ),
    )
    parser.add_argument(
        "--pdf", required=True, type=Path, metavar="FILE",
        help="Path to the input PDF file",
    )
    parser.add_argument(
        "--output-dir", required=True, type=Path, metavar="DIR",
        help="Directory where the output .md file is written",
    )
    parser.add_argument(
        "--skill-name", required=True,
        help="Skill name written to YAML frontmatter",
    )
    parser.add_argument(
        "--skill-description", required=True,
        help="Skill description written to YAML frontmatter",
    )

    args = parser.parse_args()

    if args.pdf.suffix.lower() != ".pdf":
        print(f"error: not a .pdf file: {args.pdf}", file=sys.stderr)
        sys.exit(1)
    if not args.pdf.exists():
        print(f"error: PDF not found: {args.pdf}", file=sys.stderr)
        sys.exit(1)

    out = extract(args.pdf, args.output_dir, args.skill_name, args.skill_description)
    # ExtractionOrchestrator parses this line to locate the output file — keep format stable.
    print(f"Written: {out}")


if __name__ == "__main__":
    main()
