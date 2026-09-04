from pathlib import Path
import json
import re
import zipfile

from ens_tool import ENSParser, walk


ROOT = Path("app")


def child(node, tag):
    return next(item for item in node.children if item.tag == tag)


def test_manifest_and_style_count():
    manifest = json.loads((ROOT / "style-manifest.json").read_text(encoding="utf-8"))
    assert len(manifest) == 18
    assert len(list((ROOT / "Styles").glob("*.ens"))) == 18
    assert len({item["sha256"] for item in manifest}) == 18


def test_every_style_parses_and_disables_bibliography():
    for path in (ROOT / "Styles").glob("*.ens"):
        root = ENSParser(path.read_bytes()).parse()
        footnotes = child(root, 0x1004)
        assert child(footnotes, 0x1050).value == 0
        assert child(child(footnotes, 0x1059), 0x10C0).value == 0
        assert child(child(footnotes, 0x1059), 0x10C1).value == 0
        assert child(child(root, 0x1002), 0x1023).children == []


def test_every_style_has_cited_pages_and_no_ibid():
    for path in (ROOT / "Styles").glob("*.ens"):
        root = ENSParser(path.read_bytes()).parse()
        footnotes = child(root, 0x1004)
        texts = [item.text or "" for _, item in walk(footnotes) if item.kind == 2]
        full = child(footnotes, 0x1055)
        field_ids = []
        for template in full.children:
            try:
                body = child(template, 0x10A2)
            except StopIteration:
                continue
            for token in body.children:
                try:
                    field_ids.append(child(token, 0x1091).value)
                except StopIteration:
                    pass
        assert 16390 in field_ids
        assert not any("Ibid." in text for text in texts)


def test_installers_are_version_independent():
    installer = (ROOT / "install_all_styles.ps1").read_text(encoding="utf-8")
    remover = (ROOT / "remove_all_styles.ps1").read_text(encoding="utf-8")
    assert 'Filter "*.ens"' in installer
    assert 'Filter "*.ens"' in remover


def test_package_support_files_are_complete():
    required = [
        "README.md",
        "格式选择中心.html",
        "格式库测试矩阵.xlsx",
        "64条标准测试文献.ris",
        "统一多语种文献类型.xml",
        "THIRD_PARTY_NOTICES.md",
        "LICENSE-EndNote-Chinese-Literature.txt",
    ]
    assert all((ROOT / name).is_file() for name in required)
    ris = (ROOT / "64条标准测试文献.ris").read_text(encoding="utf-8-sig")
    assert len(re.findall(r"^ER  -", ris, flags=re.MULTILINE)) == 64
    with zipfile.ZipFile(ROOT / "格式库测试矩阵.xlsx") as workbook:
        assert workbook.testzip() is None
        assert "xl/workbook.xml" in workbook.namelist()
    html = (ROOT / "格式选择中心.html").read_text(encoding="utf-8")
    assert len(re.findall(r'class="card"', html)) == 18
