from pathlib import Path
import xml.etree.ElementTree as ET

from ens_tool import ENSParser, serialize_ens, walk
from build_css2026 import (
    EN_BOOK,
    EN_BOOK_SECTION,
    EN_JOURNAL,
    JA_BOOK,
    JA_BOOK_SECTION,
    JA_JOURNAL,
    PREFORMATTED,
)


STYLE = Path("app/Styles/01 中国社会科学（2026）.ens")
REF_TYPES = Path("app/统一多语种文献类型.xml")


def get_child(node, tag):
    return next(item for item in node.children if item.tag == tag)


def templates(container):
    return {get_child(item, 0x10A1).value: item for item in container.children}


def tokens(template):
    return get_child(template, 0x10A2).children


def field_ids(template):
    return [get_child(token, 0x1091).value for token in tokens(template)]


def field_style(template, field_id):
    token = next(t for t in tokens(template) if get_child(t, 0x1091).value == field_id)
    return get_child(get_child(token, 0x1092), 0x0012).value


def texts(node):
    return [item.text or "" for _, item in walk(node) if item.kind == 2]


def test_source_roundtrip_remains_byte_exact():
    data = Path("assets/UC Chicago 17th Footnote.ens").read_bytes()
    parser = ENSParser(data)
    assert serialize_ens(parser.parse(), parser.endian) == data


def test_generated_style_parses_and_has_correct_identity():
    parser = ENSParser(STYLE.read_bytes())
    root = parser.parse()
    metadata = get_child(root, 0x1001)
    assert get_child(metadata, 0x1010).text == "中文学术－中国社会科学（2026） 0.1"
    assert "中国社会科学" in get_child(metadata, 0x1013).text


def test_document_bibliography_is_disabled():
    root = ENSParser(STYLE.read_bytes()).parse()
    footnotes = get_child(root, 0x1004)
    assert get_child(footnotes, 0x1050).value == 0
    assert get_child(footnotes, 0x1052).value == 1
    bibliography = get_child(root, 0x1002)
    assert get_child(bibliography, 0x1023).children == []


def test_full_templates_cover_core_chinese_and_foreign_types():
    root = ENSParser(STYLE.read_bytes()).parse()
    full = templates(get_child(get_child(root, 0x1004), 0x1055))
    required = {
        0, 1, 2, 3, 5, 7, 9, 10, 16, 20, 32, 33, 35,
        EN_JOURNAL, EN_BOOK, EN_BOOK_SECTION,
        JA_JOURNAL, JA_BOOK, JA_BOOK_SECTION, PREFORMATTED,
    }
    assert required <= set(full)
    for ref_type in required - {16, PREFORMATTED}:
        assert 16390 in field_ids(full[ref_type])


def test_chinese_titles_are_plain_and_english_containers_italic():
    root = ENSParser(STYLE.read_bytes()).parse()
    full = templates(get_child(get_child(root, 0x1004), 0x1055))
    for ref_type in (0, 1, 2, 5, 7, 9, 10, 16, 20, 32, 33, 35):
        assert field_style(full[ref_type], 4) == 0
    assert field_style(full[EN_BOOK], 4) == 2
    assert field_style(full[EN_BOOK_SECTION], 6) == 2
    assert field_style(full[EN_JOURNAL], 6) == 2


def test_default_chinese_templates_do_not_emit_doi():
    root = ENSParser(STYLE.read_bytes()).parse()
    full = templates(get_child(get_child(root, 0x1004), 0x1055))
    for ref_type in (0, 1, 2, 3, 5, 7, 9, 10, 16, 20, 32, 33, 35):
        assert 43 not in field_ids(full[ref_type])


def test_author_and_editor_lists_are_language_split():
    root = ENSParser(STYLE.read_bytes()).parse()
    footnotes = get_child(root, 0x1004)
    author_text = "".join(texts(get_child(footnotes, 0x1053)))
    editor_text = "".join(texts(get_child(footnotes, 0x1054)))
    assert "、" in author_text and "等" in author_text
    assert " and " not in author_text and "et al." not in author_text
    assert " and " in editor_text and "et al." in editor_text


def test_repeated_citations_use_short_form_without_ibid():
    root = ENSParser(STYLE.read_bytes()).parse()
    repeated = get_child(get_child(root, 0x1004), 0x1059)
    assert get_child(repeated, 0x10C0).value == 0
    assert get_child(repeated, 0x10C1).value == 0
    assert get_child(repeated, 0x10C2).value == 2
    assert get_child(repeated, 0x10C3).value == 2
    assert not any("Ibid" in text or "同上" in text for text in texts(repeated))


def test_multiple_citations_do_not_emit_period_semicolon_pair():
    root = ENSParser(STYLE.read_bytes()).parse()
    footnotes = get_child(root, 0x1004)
    assert get_child(get_child(footnotes, 0x105A), 0x0020).text == " "


def test_repeated_templates_follow_2026_distinction():
    root = ENSParser(STYLE.read_bytes()).parse()
    footnotes = get_child(root, 0x1004)
    short = templates(get_child(get_child(get_child(footnotes, 0x1059), 0x10CA), 0x1055))
    # Chinese books omit publication place/publisher/year when repeated.
    assert 11 not in field_ids(short[1])
    assert 12 not in field_ids(short[1])
    assert 3 not in field_ids(short[1])
    # Chinese journals and theses retain issue/institution/year information.
    assert {3, 6, 8} <= set(field_ids(short[0]))
    assert {3, 7, 12} <= set(field_ids(short[2]))
    # English repeated journal/book-section citations keep author, title, pages.
    assert field_ids(short[EN_JOURNAL]) == [10, 4, 16390, 0]
    assert field_ids(short[EN_BOOK_SECTION]) == [10, 4, 16390, 0]


def test_reference_type_table_contains_expected_custom_types_and_fields():
    root = ET.parse(REF_TYPES).getroot()
    types = {int(item.get("id")): item for item in root.findall("RefType")}
    expected = {
        EN_JOURNAL: "English Journal Article",
        EN_BOOK: "English Book",
        EN_BOOK_SECTION: "English Book Section",
        JA_JOURNAL: "Japanese Journal Article",
        JA_BOOK: "Japanese Book",
        JA_BOOK_SECTION: "Japanese Book Section",
        PREFORMATTED: "Preformatted Footnote",
    }
    assert {key: types[key].get("name") for key in expected} == expected

    en_fields = {int(f.get("id")): f.text for f in types[EN_BOOK].find("Fields")}
    ja_fields = {int(f.get("id")): f.text for f in types[JA_BOOK].find("Fields")}
    pre_fields = {int(f.get("id")): f.text for f in types[PREFORMATTED].find("Fields")}
    assert en_fields[10] == "English Author"
    assert ja_fields[19] == "Japanese Authors (formatted)"
    assert pre_fields[4] == "Complete Footnote"


if __name__ == "__main__":
    checks = [value for name, value in sorted(globals().items()) if name.startswith("test_")]
    for check in checks:
        check()
        print(f"PASS {check.__name__}")
