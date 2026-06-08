"""Unit and integration tests for extract.py."""

import pytest
from pathlib import Path

import fitz

from extract import (
    _cell,
    _collect_body_sizes,
    _heading_level,
    _inside_any_table,
    build_frontmatter,
    extract,
    table_to_markdown,
)


# ---------------------------------------------------------------------------
# Fixtures — build minimal PDFs in memory
# ---------------------------------------------------------------------------

def _make_simple_pdf(path: Path) -> None:
    doc = fitz.open()
    page = doc.new_page()
    page.insert_text((50, 50), "1.1 Introduction", fontsize=14)
    page.insert_text((50, 80), "This is body text.", fontsize=10)
    doc.save(str(path))
    doc.close()


def _make_table_pdf(path: Path) -> None:
    doc = fitz.open()
    page = doc.new_page()
    # Draw 2×2 grid
    page.draw_rect(fitz.Rect(50, 100, 300, 160), width=1)
    page.draw_line(fitz.Point(175, 100), fitz.Point(175, 160), width=1)
    page.draw_line(fitz.Point(50, 130), fitz.Point(300, 130), width=1)
    page.insert_text((55, 120), "Header A", fontsize=10)
    page.insert_text((180, 120), "Header B", fontsize=10)
    page.insert_text((55, 150), "Value 1", fontsize=10)
    page.insert_text((180, 150), "Value 2", fontsize=10)
    doc.save(str(path))
    doc.close()


def _make_multipage_pdf(path: Path, n: int = 3) -> None:
    doc = fitz.open()
    for i in range(n):
        page = doc.new_page()
        page.insert_text((50, 50), f"Content on page {i + 1}.", fontsize=10)
    doc.save(str(path))
    doc.close()


# ---------------------------------------------------------------------------
# _cell
# ---------------------------------------------------------------------------

class TestCell:
    def test_none_returns_empty(self):
        assert _cell(None) == ""

    def test_pipe_escaped(self):
        assert _cell("a|b") == "a\\|b"

    def test_multiple_pipes_escaped(self):
        assert _cell("a|b|c") == "a\\|b\\|c"

    def test_newline_replaced_with_space(self):
        assert _cell("line1\nline2") == "line1 line2"

    def test_normal_string_unchanged(self):
        assert _cell("hello") == "hello"

    def test_integer_converted_to_string(self):
        assert _cell(42) == "42"


# ---------------------------------------------------------------------------
# table_to_markdown
# ---------------------------------------------------------------------------

class TestTableToMarkdown:
    def test_empty_input_returns_empty_string(self):
        assert table_to_markdown([]) == ""

    def test_header_only_two_rows(self):
        lines = table_to_markdown([["A", "B"]]).splitlines()
        assert lines[0] == "| A | B |"
        assert lines[1] == "| --- | --- |"
        assert len(lines) == 2

    def test_header_plus_data_row(self):
        lines = table_to_markdown([["H1", "H2"], ["R1", "R2"]]).splitlines()
        assert lines[0] == "| H1 | H2 |"
        assert lines[1] == "| --- | --- |"
        assert lines[2] == "| R1 | R2 |"

    def test_jagged_rows_padded_to_widest(self):
        result = table_to_markdown([["A", "B", "C"], ["X"]])
        last_row = result.splitlines()[-1]
        assert last_row.count("|") == 4  # 3 cols → 4 pipes

    def test_none_cell_becomes_empty_string(self):
        result = table_to_markdown([["A", None], ["B", None]])
        assert "| A |  |" in result

    def test_pipe_in_cell_is_escaped(self):
        result = table_to_markdown([["a|b", "c"]])
        assert "a\\|b" in result

    def test_newline_in_cell_replaced_with_space(self):
        result = table_to_markdown([["a\nb", "c"]])
        assert "a b" in result

    def test_separator_column_count_matches_header(self):
        result = table_to_markdown([["X", "Y", "Z"]])
        sep = result.splitlines()[1]
        assert sep.count("---") == 3

    def test_multirow_order_preserved(self):
        rows = [["H"], ["1"], ["2"], ["3"]]
        lines = table_to_markdown(rows).splitlines()
        assert lines[2] == "| 1 |"
        assert lines[3] == "| 2 |"
        assert lines[4] == "| 3 |"


# ---------------------------------------------------------------------------
# _heading_level
# ---------------------------------------------------------------------------

BODY = 10.0


class TestHeadingLevel:
    def test_body_text_returns_none(self):
        assert _heading_level("normal text", BODY, 0, BODY) is None

    def test_bold_but_not_large_returns_none(self):
        assert _heading_level("small bold", BODY * 1.1, 0x10, BODY) is None

    def test_very_large_and_bold_returns_level_2(self):
        assert _heading_level("Title", BODY * 1.7, 0x10, BODY) == 2

    def test_moderately_large_and_bold_returns_level_3(self):
        assert _heading_level("Subtitle", BODY * 1.3, 0x10, BODY) == 3

    def test_large_not_bold_returns_level_3(self):
        assert _heading_level("Section Header", BODY * 1.3, 0, BODY) == 3

    def test_large_takes_priority_over_section_pattern(self):
        # Large (not bold) + section pattern → 3, not 4
        assert _heading_level("1.2 Overview", BODY * 1.3, 0, BODY) == 3

    def test_section_numeric_top_level_returns_4(self):
        assert _heading_level("1. Introduction", BODY, 0, BODY) == 4

    def test_section_numeric_two_level_returns_4(self):
        assert _heading_level("1.2 Overview", BODY, 0, BODY) == 4

    def test_section_numeric_three_level_returns_4(self):
        assert _heading_level("1.2.3 Details", BODY, 0, BODY) == 4

    def test_section_chapter_keyword_returns_4(self):
        assert _heading_level("Chapter 1", BODY, 0, BODY) == 4

    def test_section_appendix_keyword_returns_4(self):
        assert _heading_level("Appendix A", BODY, 0, BODY) == 4

    def test_section_section_keyword_returns_4(self):
        assert _heading_level("Section 3", BODY, 0, BODY) == 4

    def test_section_part_keyword_returns_4(self):
        assert _heading_level("Part II", BODY, 0, BODY) == 4

    def test_non_section_leading_digits_returns_none(self):
        # Plain number without structural pattern
        assert _heading_level("42 is the answer", BODY, 0, BODY) is None

    def test_bold_bit_4_only_is_recognized(self):
        # Bit 1 (italic) should not count as bold
        assert _heading_level("text", BODY * 1.3, 0x02, BODY) == 3   # large, not bold
        assert _heading_level("text", BODY * 1.3, 0x10, BODY) == 3   # large and bold → same level here


# ---------------------------------------------------------------------------
# _inside_any_table
# ---------------------------------------------------------------------------

TABLE_BBOX = [(50.0, 100.0, 300.0, 200.0)]


class TestInsideAnyTable:
    def test_clearly_inside_returns_true(self):
        assert _inside_any_table((60, 110, 280, 190), TABLE_BBOX) is True

    def test_clearly_outside_returns_false(self):
        assert _inside_any_table((400, 400, 500, 500), TABLE_BBOX) is False

    def test_within_tolerance_returns_true(self):
        # 1 unit outside each edge, tolerance=2
        assert _inside_any_table((49, 99, 301, 201), TABLE_BBOX) is True

    def test_beyond_tolerance_returns_false(self):
        assert _inside_any_table((40, 100, 300, 200), TABLE_BBOX) is False

    def test_no_tables_returns_false(self):
        assert _inside_any_table((60, 110, 280, 190), []) is False

    def test_custom_tolerance(self):
        # 5 units outside; tol=2 → False, tol=6 → True
        bbox = (45, 100, 300, 200)
        assert _inside_any_table(bbox, TABLE_BBOX, tol=2) is False
        assert _inside_any_table(bbox, TABLE_BBOX, tol=6) is True


# ---------------------------------------------------------------------------
# _collect_body_sizes
# ---------------------------------------------------------------------------

def _text_block(spans):
    return {
        "type": 0,
        "lines": [{"spans": [{"text": t, "size": s} for t, s in spans]}],
    }


class TestCollectBodySizes:
    def test_basic_sizes_returned(self):
        data = {"blocks": [_text_block([("hello", 10.0), ("world", 12.0)])]}
        assert _collect_body_sizes(data) == [10.0, 12.0]

    def test_non_text_block_skipped(self):
        data = {"blocks": [{"type": 1, "lines": [{"spans": [{"text": "img", "size": 20.0}]}]}]}
        assert _collect_body_sizes(data) == []

    def test_whitespace_only_span_skipped(self):
        data = {"blocks": [_text_block([("   ", 10.0), ("word", 11.0)])]}
        assert _collect_body_sizes(data) == [11.0]

    def test_empty_blocks_returns_empty(self):
        assert _collect_body_sizes({"blocks": []}) == []

    def test_multiple_blocks_combined(self):
        data = {"blocks": [
            _text_block([("a", 10.0)]),
            _text_block([("b", 14.0)]),
        ]}
        assert _collect_body_sizes(data) == [10.0, 14.0]


# ---------------------------------------------------------------------------
# build_frontmatter
# ---------------------------------------------------------------------------

class TestBuildFrontmatter:
    def test_skill_name_present(self):
        assert "skill: MySkill" in build_frontmatter("MySkill", "d", "f.pdf")

    def test_description_present(self):
        assert "description: A great skill" in build_frontmatter("s", "A great skill", "f.pdf")

    def test_source_present(self):
        assert "source: myfile.pdf" in build_frontmatter("s", "d", "myfile.pdf")

    def test_starts_with_yaml_fence(self):
        assert build_frontmatter("s", "d", "f.pdf").startswith("---\n")

    def test_yaml_fence_closed(self):
        lines = build_frontmatter("s", "d", "f.pdf").splitlines()
        assert "---" in lines[1:]  # closing fence somewhere after the opening


# ---------------------------------------------------------------------------
# Integration: extract()
# ---------------------------------------------------------------------------

class TestExtract:
    def test_creates_output_file(self, tmp_path):
        pdf = tmp_path / "sample.pdf"
        _make_simple_pdf(pdf)
        out = extract(pdf, tmp_path / "out", "Skill", "Desc")
        assert out.exists()

    def test_output_suffix_is_md(self, tmp_path):
        pdf = tmp_path / "sample.pdf"
        _make_simple_pdf(pdf)
        out = extract(pdf, tmp_path / "out", "Skill", "Desc")
        assert out.suffix == ".md"

    def test_output_stem_matches_pdf_stem(self, tmp_path):
        pdf = tmp_path / "mypdf.pdf"
        _make_simple_pdf(pdf)
        out = extract(pdf, tmp_path / "out", "Skill", "Desc")
        assert out.stem == "mypdf"

    def test_frontmatter_skill_name(self, tmp_path):
        pdf = tmp_path / "s.pdf"
        _make_simple_pdf(pdf)
        content = extract(pdf, tmp_path / "out", "TestSkill", "d").read_text("utf-8")
        assert "skill: TestSkill" in content

    def test_frontmatter_description(self, tmp_path):
        pdf = tmp_path / "s.pdf"
        _make_simple_pdf(pdf)
        content = extract(pdf, tmp_path / "out", "s", "My Description").read_text("utf-8")
        assert "description: My Description" in content

    def test_frontmatter_source_filename(self, tmp_path):
        pdf = tmp_path / "report.pdf"
        _make_simple_pdf(pdf)
        content = extract(pdf, tmp_path / "out", "s", "d").read_text("utf-8")
        assert "source: report.pdf" in content

    def test_single_page_marker(self, tmp_path):
        pdf = tmp_path / "s.pdf"
        _make_simple_pdf(pdf)
        content = extract(pdf, tmp_path / "out", "s", "d").read_text("utf-8")
        assert "<!-- Page 1 of 1 -->" in content

    def test_multipage_all_markers_present(self, tmp_path):
        pdf = tmp_path / "multi.pdf"
        _make_multipage_pdf(pdf, n=3)
        content = extract(pdf, tmp_path / "out", "s", "d").read_text("utf-8")
        assert "<!-- Page 1 of 3 -->" in content
        assert "<!-- Page 2 of 3 -->" in content
        assert "<!-- Page 3 of 3 -->" in content

    def test_multipage_no_extra_markers(self, tmp_path):
        pdf = tmp_path / "multi.pdf"
        _make_multipage_pdf(pdf, n=2)
        content = extract(pdf, tmp_path / "out", "s", "d").read_text("utf-8")
        assert "<!-- Page 3" not in content

    def test_table_produces_gfm_separator(self, tmp_path):
        pdf = tmp_path / "table.pdf"
        _make_table_pdf(pdf)
        content = extract(pdf, tmp_path / "out", "s", "d").read_text("utf-8")
        assert "| --- |" in content

    def test_output_dir_created_if_missing(self, tmp_path):
        pdf = tmp_path / "s.pdf"
        _make_simple_pdf(pdf)
        out_dir = tmp_path / "nested" / "deep" / "output"
        extract(pdf, out_dir, "s", "d")
        assert out_dir.exists()

    def test_section_heading_promoted(self, tmp_path):
        pdf = tmp_path / "s.pdf"
        _make_simple_pdf(pdf)
        content = extract(pdf, tmp_path / "out", "s", "d").read_text("utf-8")
        # "1.1 Introduction" at 14pt should become a heading
        assert "#" in content
