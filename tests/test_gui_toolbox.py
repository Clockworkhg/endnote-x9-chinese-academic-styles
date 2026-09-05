from pathlib import Path
import json


ROOT = Path("app")
PROJECT = Path("windows/EndNoteStyleToolbox")
EXPECTED_STYLE_COUNT = 47


def test_exe_project_structure():
    required = [
        "EndNoteStyleToolbox.csproj",
        "Program.cs",
        "MainForm.cs",
        "StyleInfo.cs",
        "StyleService.cs",
        "EmbeddedAssets.cs",
        "Diagnostics.cs",
        "SelfTestRunner.cs",
        "app.manifest",
    ]
    assert all((PROJECT / item).is_file() for item in required)


def test_project_is_real_self_contained_windows_exe():
    project = (PROJECT / "EndNoteStyleToolbox.csproj").read_text(encoding="utf-8")
    for token in [
        "<OutputType>WinExe</OutputType>",
        "<TargetFramework>net8.0-windows</TargetFramework>",
        "<UseWindowsForms>true</UseWindowsForms>",
        "<RuntimeIdentifier>win-x64</RuntimeIdentifier>",
        "<SelfContained>true</SelfContained>",
        "<PublishSingleFile>true</PublishSingleFile>",
    ]:
        assert token in project


def test_exe_embeds_all_runtime_assets():
    project = (PROJECT / "EndNoteStyleToolbox.csproj").read_text(encoding="utf-8")
    for token in [
        "style-manifest.json",
        "app/Styles/*.ens",
        "格式库测试矩阵.xlsx",
        "64条标准测试文献.ris",
        "统一多语种文献类型.xml",
        "THIRD_PARTY_NOTICES.md",
    ]:
        assert token in project


def test_manifest_and_styles():
    manifest = json.loads((ROOT / "style-manifest.json").read_text(encoding="utf-8"))
    assert len(manifest) == EXPECTED_STYLE_COUNT
    assert len(list((ROOT / "Styles").glob("*.ens"))) == EXPECTED_STYLE_COUNT
    assert all((ROOT / "Styles" / item["filename"]).is_file() for item in manifest)


def test_native_gui_has_expected_actions_and_safe_target():
    form = (PROJECT / "MainForm.cs").read_text(encoding="utf-8")
    service = (PROJECT / "StyleService.cs").read_text(encoding="utf-8")
    for token in [
        "安装所选格式",
        "卸载所选格式",
        "安装全部",
        "卸载全部",
        "导出测试与配置",
        "查看规范来源",
        "使用帮助",
    ]:
        assert token in form
    directories = (PROJECT / "StyleDirectoryService.cs").read_text(encoding="utf-8")
    assert '"EndNote", "Styles"' in directories
    assert "SpecialFolder.MyDocuments" in directories
    assert "StyleDirectoryService.Validate" in service
    assert "StyleDirectoryService.CheckWritable" in service
    assert "IsManaged" in service
    assert "Program Files" not in service
    assert "File.Delete(target)" not in service
    assert "File.Move(target" in service
    assert "CreateBackupDirectory" in service


def test_headless_self_test_covers_exe_resources_and_lifecycle():
    source = (PROJECT / "SelfTestRunner.cs").read_text(encoding="utf-8")
    program = (PROJECT / "Program.cs").read_text(encoding="utf-8")
    assert '"--self-test"' in program
    for token in [
        "Manifest contains all 46 upstream note styles plus the CUC profile",
        "Main window constructs successfully",
        "Main window handle can be created",
        "Assembly contains {styles.Count} ENS resources",
        "SHA-256 matches",
        "Install writes the embedded ENS",
        "Reinstall backs up the existing file",
        "Uninstall moves the file into a recoverable backup",
        "Support files export successfully",
    ]:
        assert token in source


def test_exe_does_not_launch_powershell_or_cmd():
    source = "\n".join(path.read_text(encoding="utf-8") for path in PROJECT.glob("*.cs"))
    lowered = source.lower()
    assert "powershell" not in lowered
    assert "cmd.exe" not in lowered
    assert "processstartinfo" in lowered  # Native updater, folder and HTTPS source links.


def test_windows_workflow_builds_and_launches_the_published_exe():
    workflow = Path(".github/workflows/windows-exe.yml").read_text(encoding="utf-8")
    for token in [
        "windows-latest",
        "dotnet publish",
        "--self-contained true",
        "Start-Process",
        "--self-test",
        'status -ne "passed"',
        "upload-artifact@v4",
    ]:
        assert token in workflow
