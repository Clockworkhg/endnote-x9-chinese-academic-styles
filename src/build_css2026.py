#!/usr/bin/env python3
"""Build the EndNote X9 CSS journal footnote style (2026 revision).

The output is intentionally separate from the already field-tested Alpha 1.
It uses standard EndNote reference types for Chinese material and dedicated
custom types for English and Japanese material so one document can mix them.
"""

from __future__ import annotations

import copy
import xml.etree.ElementTree as ET
from pathlib import Path

from ens_tool import ENSParser, Node, serialize_ens, walk


SOURCE = Path("assets/UC Chicago 17th Footnote.ens")
OUTPUT = Path("build/CSS Footnotes 2026 Alpha 2.7.ens")
REF_TYPES_SOURCE = Path("assets/ReferenceType_ChineseArticle.xml")
REF_TYPES_OUTPUT = Path("build/CSS2026 Reference Types.xml")

# Dedicated reference-type ids. 14/23/24 are EndNote's three unused slots.
# 25/26/27 are the rarely used Figure/Chart/Equation slots and are repurposed
# only after the user has backed up and checked their current type table.
EN_JOURNAL = 14
EN_BOOK = 23
EN_BOOK_SECTION = 24
JA_JOURNAL = 25
JA_BOOK = 26
JA_BOOK_SECTION = 27
PREFORMATTED = 31


def child(node: Node, tag: int) -> Node:
    return next(item for item in node.children if item.tag == tag)


def first_text(node: Node) -> Node:
    return next(item for _, item in walk(node) if item.kind == 2)


def set_text(node: Node, value: str) -> None:
    first_text(node).text = value


def template_map(container: Node) -> dict[int, Node]:
    return {child(item, 0x10A1).value: item for item in container.children}


def field_id(token: Node) -> int:
    return child(token, 0x1091).value


def configure_token(
    token: Node,
    prefix: str,
    suffix: str,
    *,
    first: bool = False,
    style: int = 0,
) -> Node:
    token = copy.deepcopy(token)
    fmt = child(token, 0x1092)
    child(fmt, 0x0010).value = 1
    child(fmt, 0x0011).value = 0
    child(fmt, 0x0012).value = style  # 0 plain; 2 italic
    child(token, 0x1093).value = 0 if first else 1
    set_text(child(token, 0x1094), prefix)
    set_text(child(token, 0x1095), suffix)
    child(token, 0x1096).children = []
    child(token, 0x1097).children = []
    return token


def donor_tokens(root: Node) -> dict[int, Node]:
    donors: dict[int, Node] = {}
    for _, node in walk(root):
        if node.tag != 0x1090 or node.kind != 1:
            continue
        try:
            donors.setdefault(field_id(node), node)
        except StopIteration:
            pass
    return donors


# Each item is: field id, prefix, suffix, text style.
Spec = list[tuple[int, str, str, int]]


def rewrite_template(template: Node, spec: Spec, donors: dict[int, Node]) -> None:
    body = child(template, 0x10A2)
    local = {field_id(token): token for token in body.children}
    new_tokens: list[Node] = []
    for index, (fid, prefix, suffix, style) in enumerate(spec):
        donor = local.get(fid) or donors.get(fid) or donors.get(4)
        if donor is None:
            raise ValueError(f"No token donor for field id {fid}")
        configured = configure_token(
            donor, prefix, suffix, first=index == 0, style=style
        )
        child(configured, 0x1091).value = fid
        new_tokens.append(configured)
    body.children = new_tokens


def ensure_template(container: Node, target_id: int, donor_id: int) -> Node:
    templates = template_map(container)
    if target_id in templates:
        return templates[target_id]
    clone = copy.deepcopy(templates[donor_id])
    child(clone, 0x10A1).value = target_id
    container.children.append(clone)
    container.children.sort(key=lambda item: child(item, 0x10A1).value)
    return clone


def set_name_list(control: Node, *, chinese: bool) -> None:
    """Configure author-list separators and 4+-author abbreviation."""
    groups = [n for n in control.children if n.tag == 0x1074]
    if not groups:
        raise ValueError("Author/editor list control has no separator groups")
    for group in groups:
        for _, item in walk(group):
            if item.kind != 2:
                continue
            if chinese and item.text in {" and ", ", ", ", and "}:
                item.text = "、"
    abbreviation = child(control, 0x1075)
    child(abbreviation, 0x107D).value = 1
    child(abbreviation, 0x107E).value = 4
    child(abbreviation, 0x107F).value = 1
    child(abbreviation, 0x1081).text = "等" if chinese else " et al."
    child(abbreviation, 0x1080).value = 0


P = 0
I = 2

# Standard reference types are Chinese. Optional fields are linked to their
# own punctuation so missing data does not leave a dangling comma/colon.
CN_FULL: dict[int, Spec] = {
    # Journal Article
    0: [
        (2, "", "：", P), (4, "《", "》", P), (19, "，", "", P),
        (6, "，《", "》", P), (3, "", "年", P), (7, "第", "卷", P),
        (8, "第", "期", P), (17, "，", "", P),
        (16390, "，第", "页", P), (0, "。", "", P),
    ],
    # Book
    1: [
        (2, "", "：", P), (4, "《", "》", P), (7, "第", "卷", P),
        (13, "，", "译", P), (14, "，第", "版", P),
        (11, "，", "：", P), (12, "", "", P), (3, "，", "年", P),
        (16390, "，第", "页", P), (0, "。", "", P),
    ],
    # Thesis. Put the complete degree-granting institution in University.
    2: [
        (2, "", "：", P), (4, "《", "》", P),
        (7, "，", "学位论文", P), (12, "，", "", P),
        (3, "，", "年", P), (16390, "，第", "页", P),
        (0, "。", "", P),
    ],
    # Conference Proceedings
    3: [
        (2, "", "：", P), (4, "《", "》", P), (10, "，", "编：", P),
        (27, "《", "》", P), (25, "，", "：", P), (12, "", "", P),
        (26, "，", "年", P), (16390, "，第", "页", P),
        (0, "。", "", P),
    ],
    # Newspaper Article. Use Issue Date for the complete publication date.
    5: [
        (2, "", "：", P), (4, "《", "》", P), (6, "，《", "》", P),
        (11, "（", "）", P), (17, "", "", P),
        (14, "，第", "版", P), (16390, "，第", "页", P),
        (0, "。", "", P),
    ],
    # Book Section. Label can hold a preformatted secondary responsibility.
    7: [
        (2, "", "：", P), (4, "《", "》", P), (13, "，", "译", P),
        (0, "，", "", P), (10, "", "编：", P), (19, "", "：", P),
        (6, "《", "》", P), (7, "第", "卷", P),
        (11, "，", "：", P), (12, "", "", P), (3, "，", "年", P),
        (16390, "，第", "页", P), (0, "。", "", P),
    ],
    # Edited Book
    9: [
        (2, "", "主编：", P), (4, "《", "》", P), (7, "第", "卷", P),
        (14, "，第", "版", P), (11, "，", "：", P),
        (12, "", "", P), (3, "，", "年", P),
        (16390, "，第", "页", P), (0, "。", "", P),
    ],
    # Report
    10: [
        (2, "", "：", P), (4, "《", "》", P), (24, "，", "", P),
        (11, "，", "：", P), (12, "", "", P), (3, "，", "年", P),
        (16390, "，第", "页", P), (0, "。", "", P),
    ],
    # Web Page / electronic document
    16: [
        (2, "", "：", P), (4, "《", "》", P), (17, "，", "", P),
        (20, "，", "", P), (8, "，", "", P), (0, "。", "", P),
    ],
    # Manuscript / archive. Label stores an optional archive category/number.
    20: [
        (2, "", "：", P), (4, "《", "》", P), (17, "，", "", P),
        (6, "，", "", P), (9, "，档案号：", "", P),
        (12, "，", "藏", P), (16390, "，第", "页", P),
        (0, "。", "", P),
    ],
    # Government document
    32: [
        (2, "", "：", P), (4, "《", "》", P), (24, "，", "", P),
        (11, "，", "：", P), (12, "", "", P), (3, "，", "年", P),
        (20, "，", "", P), (16390, "，第", "页", P),
        (0, "。", "", P),
    ],
    # Conference paper. Put the complete meeting description in Conference Name.
    33: [
        (2, "", "：", P), (4, "《", "》", P),
        (6, "，", "论文", P), (11, "，", "", P), (3, "，", "年", P),
        (16390, "，第", "页", P), (0, "。", "", P),
    ],
    # Classical work. Label is a preformatted volume/chapter/version statement.
    35: [
        (2, "", "：", P), (4, "《", "》", P), (19, "", "", P),
        (22, "，", "整理", P), (11, "，", "：", P),
        (12, "", "", P), (3, "，", "年", P),
        (16390, "，第", "页", P), (0, "。", "", P),
    ],
}

# English authors are stored in field 10 (a secondary-name field), allowing
# the separate Editor Lists controls to retain English commas/"and".
EN_FULL: dict[int, Spec] = {
    EN_JOURNAL: [
        (10, "", ", ", P), (4, "“", ",” ", P), (6, "", "", I),
        (7, ", vol. ", "", P), (8, ", no. ", "", P),
        (17, " (", ")", P), (3, " (", ")", P),
        (16390, ", p.^pp.", "", P), (0, ".", "", P),
    ],
    EN_BOOK: [
        (10, "", ", ", P), (4, "", "", I),
        (13, ", trans. ", "", P), (14, ", ", " ed.", P),
        (11, ", ", ": ", P), (12, "", "", P), (3, ", ", "", P),
        (16390, ", p.^pp.", "", P), (0, ".", "", P),
    ],
    EN_BOOK_SECTION: [
        (10, "", ", ", P), (4, "“", ",” ", P),
        (6, "in ", "", I), (22, ", ed.^eds. ", "", P),
        (11, ", ", ": ", P), (12, "", "", P), (3, ", ", "", P),
        (16390, ", p.^pp.", "", P), (0, ".", "", P),
    ],
}

# Japanese responsibility statements use a plain text field (Label) so they
# are not forced through the Chinese or English author-list controls.
JA_FULL: dict[int, Spec] = {
    JA_JOURNAL: [
        (19, "", "", P), (4, "「", "」", P), (6, "、『", "』", P),
        (7, "、", "巻", P), (8, " ", "号", P), (3, "、", "年", P),
        (16390, "、", "頁", P), (0, "。", "", P),
    ],
    JA_BOOK: [
        (19, "", "", P), (4, "『", "』", P), (12, "、", "", P),
        (3, "、", "年", P), (16390, "、", "頁", P),
        (0, "。", "", P),
    ],
    JA_BOOK_SECTION: [
        (19, "", "", P), (4, "「", "」", P), (25, "、", "編、", P),
        (6, "『", "』", P), (12, "、", "", P), (3, "、", "年", P),
        (16390, "、", "頁", P), (0, "。", "", P),
    ],
}

PREFORMATTED_SPEC: Spec = [(4, "", "", P)]

# Repeated book-like works are shortened. Continuous publications and
# unpublished works keep their full template under the 2026 rules.
CN_SHORT: dict[int, Spec] = {
    1: [(2, "", "：", P), (4, "《", "》", P), (7, "第", "卷", P),
        (16390, "，第", "页", P), (0, "。", "", P)],
    7: [(2, "", "：", P), (4, "《", "》", P),
        (16390, "，第", "页", P), (0, "。", "", P)],
    9: [(2, "", "主编：", P), (4, "《", "》", P),
        (16390, "，第", "页", P), (0, "。", "", P)],
    10: [(2, "", "：", P), (4, "《", "》", P),
         (16390, "，第", "页", P), (0, "。", "", P)],
    16: [(2, "", "：", P), (4, "《", "》", P), (0, "。", "", P)],
    32: [(2, "", "：", P), (4, "《", "》", P),
         (16390, "，第", "页", P), (0, "。", "", P)],
    35: [(2, "", "：", P), (4, "《", "》", P), (19, "", "", P),
         (16390, "，第", "页", P), (0, "。", "", P)],
}

EN_SHORT: dict[int, Spec] = {
    EN_BOOK: [(10, "", ", ", P), (4, "", "", I),
              (16390, ", p.^pp.", "", P), (0, ".", "", P)],
    EN_JOURNAL: [(10, "", ", ", P), (4, "“", ",” ", P),
                 (16390, "p.^pp.", "", P), (0, ".", "", P)],
    EN_BOOK_SECTION: [(10, "", ", ", P), (4, "“", ",” ", P),
                      (16390, "p.^pp.", "", P), (0, ".", "", P)],
}

JA_SHORT: dict[int, Spec] = {
    JA_BOOK: [(19, "", "", P), (4, "『", "』", P),
              (16390, "、", "頁", P), (0, "。", "", P)],
    JA_JOURNAL: [(19, "", "", P), (4, "「", "」", P),
                 (16390, "、", "頁", P), (0, "。", "", P)],
    JA_BOOK_SECTION: [(19, "", "", P), (4, "「", "」", P),
                      (16390, "、", "頁", P), (0, "。", "", P)],
}


def configure_repeated_citations(footnotes: Node) -> None:
    """Let consecutive repeats follow the ordinary short-form templates."""
    repeated = child(footnotes, 0x1059)
    # Disable special consecutive-reference replacement. Alpha 2.5/2.6 kept
    # this handler active, so X9 replaced the citation with an empty string
    # and left only the terminal period.
    child(repeated, 0x10C0).value = 0
    child(repeated, 0x10C1).value = 0
    child(repeated, 0x10C2).value = 2
    child(repeated, 0x10C3).value = 2
    for tag in (0x10C4, 0x10C5):
        text = first_text(child(repeated, tag))
        text.text = ""
    # Clear the legacy same-source output sequence as a second safeguard.
    for _, item in walk(child(repeated, 0x10CC)):
        if item.kind == 2 and "Ibid." in (item.text or ""):
            item.text = (item.text or "").replace("Ibid.", "")


def disable_bibliography(root: Node) -> None:
    """Suppress the document bibliography for this footnote-only style.

    A real EndNote X9 save operation established that the Footnotes >
    Templates checkbox named "Include citations in bibliography" is tag
    0x1050: one means checked and zero means unchecked.  Tag 0x1052 belongs to
    the adjacent short-form disambiguation option.  Clearing Bibliography
    templates alone is not sufficient because CWYW can still create a document
    bibliography.
    """
    footnotes = child(root, 0x1004)
    child(footnotes, 0x1050).value = 0
    # Keep the adjacent short-form option in the state verified by EndNote X9.
    child(footnotes, 0x1052).value = 1
    bibliography = child(root, 0x1002)
    templates = child(bibliography, 0x1023)
    templates.children = []


def build_style() -> None:
    data = SOURCE.read_bytes()
    parser = ENSParser(data)
    root = parser.parse()
    donors = donor_tokens(root)

    metadata = child(root, 0x1001)
    child(metadata, 0x1010).text = "CSS Footnotes 2026 Alpha 2.7"
    child(metadata, 0x1013).text = "中国社会科学期刊引文注释规定（2026年修订）"
    first_text(child(metadata, 0x1015)).text = (
        "依据《中国社会科学杂志社期刊引文注释规定（2026年修订）》制作。"
        "中文资料使用普通类型；英文、日文资料使用配套专用类型。"
        "保留每次引用单独填写 Cited Pages 的能力；不使用 Ibid./同上。"
        "脚注序号由 Word/WPS 控制，不由本样式控制。"
        "Alpha 2.7 关闭连续重复引用的特殊替换，使相邻再次引用统一调用简略模板，避免第二条只剩句点；"
        "并保留 Alpha 2.5 对 Include citations in bibliography 开关的修正，"
        "需先在文档副本中完成 EndNote X9 与 Word/WPS 实机测试。"
    )

    disable_bibliography(root)
    footnotes = child(root, 0x1004)
    # Each citation template already ends with its own full stop.  A plain
    # space avoids the malformed full-stop-plus-semicolon sequence seen in
    # WPS when several references share one footnote.
    set_text(child(footnotes, 0x105A), " ")
    set_name_list(child(footnotes, 0x1053), chinese=True)
    set_name_list(child(footnotes, 0x1054), chinese=False)
    full_container = child(footnotes, 0x1055)

    for target, donor in {
        EN_JOURNAL: 0, EN_BOOK: 1, EN_BOOK_SECTION: 7,
        JA_JOURNAL: 0, JA_BOOK: 1, JA_BOOK_SECTION: 7,
    }.items():
        ensure_template(full_container, target, donor)

    full = template_map(full_container)
    for ref_type, spec in (CN_FULL | EN_FULL | JA_FULL).items():
        rewrite_template(full[ref_type], spec, donors)
    rewrite_template(full[PREFORMATTED], PREFORMATTED_SPEC, donors)

    configure_repeated_citations(footnotes)
    short_section = child(child(footnotes, 0x1059), 0x10CA)
    set_name_list(child(short_section, 0x1053), chinese=True)
    set_name_list(child(short_section, 0x1054), chinese=False)
    short_container = child(short_section, 0x1055)
    for target, donor in {
        EN_JOURNAL: 0, EN_BOOK: 1, EN_BOOK_SECTION: 7,
        JA_JOURNAL: 0, JA_BOOK: 1, JA_BOOK_SECTION: 7,
        PREFORMATTED: 1,
    }.items():
        ensure_template(short_container, target, donor)
    short = template_map(short_container)

    # Full-form short templates for Chinese periodicals and unpublished works.
    for ref_type in (0, 2, 3, 5, 20, 33):
        rewrite_template(short[ref_type], CN_FULL[ref_type], donors)
    for ref_type, spec in (CN_SHORT | EN_SHORT | JA_SHORT).items():
        rewrite_template(short[ref_type], spec, donors)
    rewrite_template(short[PREFORMATTED], PREFORMATTED_SPEC, donors)

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_bytes(serialize_ens(root, parser.endian))
    ENSParser(OUTPUT.read_bytes()).parse()
    print(f"wrote {OUTPUT} ({OUTPUT.stat().st_size} bytes)")


def clone_fields(root: ET.Element, target_id: int, donor_id: int, name: str) -> ET.Element:
    types = {int(item.get("id")): item for item in root.findall("RefType")}
    target = types[target_id]
    donor = types[donor_id]
    target.set("name", name)
    old = target.find("Fields")
    if old is not None:
        target.remove(old)
    target.append(copy.deepcopy(donor.find("Fields")))
    return target


def rename_field(ref_type: ET.Element, fid: int, label: str, order: int | None = None) -> None:
    fields = ref_type.find("Fields")
    assert fields is not None
    field = next((f for f in fields.findall("Field") if int(f.get("id")) == fid), None)
    if field is None:
        field = ET.Element("Field", {"id": str(fid), "order": str(order or 10)})
        fields.insert(0, field)
    field.text = label
    if order is not None:
        field.set("order", str(order))


def build_reference_types() -> None:
    tree = ET.parse(REF_TYPES_SOURCE)
    root = tree.getroot()

    en_journal = clone_fields(root, EN_JOURNAL, 0, "English Journal Article")
    rename_field(en_journal, 2, "Original Author (backup)", 15)
    rename_field(en_journal, 10, "English Author", 10)

    en_book = clone_fields(root, EN_BOOK, 1, "English Book")
    rename_field(en_book, 2, "Original Author (backup)", 15)
    rename_field(en_book, 10, "English Author", 10)

    en_section = clone_fields(root, EN_BOOK_SECTION, 7, "English Book Section")
    rename_field(en_section, 2, "Original Author (backup)", 15)
    rename_field(en_section, 10, "English Author", 10)
    rename_field(en_section, 22, "English Editor", 130)

    ja_journal = clone_fields(root, JA_JOURNAL, 0, "Japanese Journal Article")
    rename_field(ja_journal, 19, "Japanese Authors (formatted)", 10)
    rename_field(ja_journal, 2, "Original Author (backup)", 15)

    ja_book = clone_fields(root, JA_BOOK, 1, "Japanese Book")
    rename_field(ja_book, 19, "Japanese Authors (formatted)", 10)
    rename_field(ja_book, 2, "Original Author (backup)", 15)

    ja_section = clone_fields(root, JA_BOOK_SECTION, 7, "Japanese Book Section")
    rename_field(ja_section, 19, "Japanese Authors (formatted)", 10)
    rename_field(ja_section, 25, "Japanese Editor (formatted)", 20)
    rename_field(ja_section, 2, "Original Author (backup)", 15)

    preformatted = next(x for x in root.findall("RefType") if int(x.get("id")) == PREFORMATTED)
    preformatted.set("name", "Preformatted Footnote")
    rename_field(preformatted, 4, "Complete Footnote", 10)
    rename_field(preformatted, 32, "Short Complete Footnote", 20)

    # Helpful labels for complex Chinese materials without changing their ids.
    classical = next(x for x in root.findall("RefType") if int(x.get("id")) == 35)
    rename_field(classical, 19, "卷次篇名与版本说明（按规范填写）", 35)
    manuscript = next(x for x in root.findall("RefType") if int(x.get("id")) == 20)
    rename_field(manuscript, 9, "档案号", 90)

    ET.indent(tree, space="  ")
    REF_TYPES_OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    tree.write(REF_TYPES_OUTPUT, encoding="utf-8", xml_declaration=True)
    # Parse the written file once more as a structural validation.
    ET.parse(REF_TYPES_OUTPUT)
    print(f"wrote {REF_TYPES_OUTPUT} ({REF_TYPES_OUTPUT.stat().st_size} bytes)")


if __name__ == "__main__":
    build_style()
    build_reference_types()
