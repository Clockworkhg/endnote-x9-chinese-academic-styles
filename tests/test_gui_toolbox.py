from pathlib import Path
import json
import re


ROOT = Path("app")


def test_gui_package_structure():
    required = [
        "StyleToolbox.ps1",
        "打开中文学术格式工具箱.cmd",
        "可视化版使用说明.txt",
        "style-manifest.json",
        "格式库测试矩阵.xlsx",
        "64条标准测试文献.ris",
        "统一多语种文献类型.xml",
    ]
    assert all((ROOT / item).is_file() for item in required)


def test_manifest_and_styles():
    manifest = json.loads((ROOT / "style-manifest.json").read_text(encoding="utf-8"))
    assert len(manifest) == 18
    assert len(list((ROOT / "Styles").glob("*.ens"))) == 18
    assert all((ROOT / "Styles" / item["filename"]).is_file() for item in manifest)


def test_gui_has_expected_actions_and_safe_target():
    script = (ROOT / "StyleToolbox.ps1").read_text(encoding="utf-8-sig")
    for token in [
        "Install-Style",
        "Remove-Style",
        "Get-InstalledState",
        "安装选中格式",
        "卸载选中格式",
        "安装全部18套",
        "格式库测试矩阵.xlsx",
        'Join-Path $script:Documents "EndNote\\Styles"',
    ]:
        assert token in script
    assert "Program Files" not in script
    assert "Remove-Item" not in script
    assert "Move-Item" in script


def test_powershell_delimiters_are_balanced():
    script = (ROOT / "StyleToolbox.ps1").read_text(encoding="utf-8-sig")
    stripped = re.sub(r'"(?:`.|[^"`])*"', '""', script)
    stripped = re.sub(r"'(?:''|[^'])*'", "''", stripped)
    pairs = {"(": ")", "[": "]", "{": "}"}
    stack = []
    for char in stripped:
        if char in pairs:
            stack.append(char)
        elif char in pairs.values():
            assert stack and pairs[stack.pop()] == char
    assert not stack
