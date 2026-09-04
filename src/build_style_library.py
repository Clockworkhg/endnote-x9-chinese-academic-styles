#!/usr/bin/env python3
"""Generate an EndNote X9 Chinese academic footnote style library.

The stable CSS 2026 Alpha 2.7 ENS is the shared binary/template base.  Each
preview style applies a small, auditable family override derived from the
corresponding CSL examples in zotero-chinese/styles.  The resulting files are
preview builds: they are structurally validated here and require Word/WPS
output testing before being presented as journal-conformant releases.
"""

from __future__ import annotations

import copy
import hashlib
import html
import json
import re
from pathlib import Path

from build_css2026 import (
    EN_BOOK,
    EN_BOOK_SECTION,
    EN_JOURNAL,
    I,
    P,
    child,
    donor_tokens,
    rewrite_template,
    template_map,
)
from ens_tool import ENSParser, serialize_ens


BASE = Path("app/Styles/01 中国社会科学（2026）.ens")
OUT = Path("build/chinese-academic-style-library")
PACKAGE = Path("build/generated-package")
EXAMPLES_PATH = Path("assets/style-examples.json")


STYLES = [
    (1, "中国社会科学（2026）", "中国社会科学", "css", "已实机验证"),
    (2, "综合性期刊文献引证技术规范", "综合性期刊文献引证技术规范（注释）", "technical", "预览版"),
    (3, "人民出版社", "人民出版社", "publisher", "预览版"),
    (4, "清华大学（人文社科）", "清华大学（人文社科）", "technical", "预览版"),
    (5, "政治学研究", "政治学研究", "politics", "预览版"),
    (6, "世界经济与政治", "世界经济与政治", "international", "预览版"),
    (7, "外交评论", "外交评论", "foreign_affairs", "预览版"),
    (8, "国际政治研究", "国际政治研究", "intl_politics", "预览版"),
    (9, "现代国际关系", "现代国际关系", "modern_ir", "预览版"),
    (10, "历史研究", "历史研究", "history", "预览版"),
    (11, "世界历史", "世界历史", "modern_ir", "预览版"),
    (12, "文学评论", "文学评论", "literature", "预览版"),
    (13, "新闻与传播研究", "新闻与传播研究", "news", "预览版"),
    (14, "法学引注手册（第二版）", "法学引注手册（第二版，多语言）", "law_manual", "预览版"),
    (15, "中国政法大学", "中国政法大学", "law_manual", "预览版"),
    (16, "中外法学", "中外法学", "law_review", "预览版"),
    (17, "GB/T 7714—2015（注释·双语）", "GB-T-7714—2015（注释，双语）", "gbt2015", "预览版"),
    (18, "GB/T 7714—2025（注释·双语）", "GB-T-7714—2025（注释，双语）", "gbt2025", "预览版"),
]


def cn_common(*, author_end="：", year_end="年", place=True, year_edition=False):
    pub = [(11, "，", "：", P), (12, "", "", P)] if place else [(12, "，", "", P)]
    year_suffix = "年版" if year_edition else year_end
    return {
        1: [
            (2, "", author_end, P), (4, "《", "》", P),
            (14, "（第", "版）", P), (13, "，", "译", P),
            *pub, (3, "，", year_suffix, P),
            (16390, "，第", "页", P), (0, "。", "", P),
        ],
        0: [
            (2, "", author_end, P), (4, "《", "》", P),
            (6, "，《", "》", P), (3, "", "年", P),
            (7, "第", "卷", P), (8, "第", "期", P),
            (16390, "，第", "页", P), (0, "。", "", P),
        ],
        7: [
            (2, "", author_end, P), (4, "《", "》", P),
            (13, "，", "译", P), (10, "，载", "编：", P),
            (6, "《", "》", P), *pub, (3, "，", year_suffix, P),
            (16390, "，第", "页", P), (0, "。", "", P),
        ],
        2: [
            (2, "", author_end, P), (4, "《", "》", P),
            (7, "，", "学位论文", P), (12, "，", "", P),
            (3, "，", "年", P), (16390, "，第", "页", P),
            (0, "。", "", P),
        ],
        5: [
            (2, "", author_end, P), (4, "《", "》", P),
            (6, "，载《", "》", P), (17, "", "", P),
            (14, "，第", "版", P), (0, "。", "", P),
        ],
        16: [
            (2, "", author_end, P), (4, "《", "》", P),
            (17, "，", "", P), (20, "，", "", P),
            (8, "，", "", P), (0, "。", "", P),
        ],
    }


FAMILIES = {
    "technical": cn_common(),
    "publisher": cn_common(place=False, year_edition=True),
    "politics": cn_common(place=False, year_edition=True),
    "international": cn_common(place=False, year_edition=True),
    "foreign_affairs": cn_common(place=False),
    "intl_politics": cn_common(place=True, year_edition=True),
    "modern_ir": cn_common(author_end="，", place=False),
    "history": cn_common(place=True),
    "literature": cn_common(author_end="，", place=False, year_edition=True),
    "news": cn_common(author_end="，", place=True),
    "law_manual": cn_common(place=False, year_edition=True),
    "law_review": cn_common(place=False, year_edition=True),
}

# Family-specific adjustments that materially distinguish the output.
FAMILIES["politics"][1] = [
    (2, "", "：", P), (4, "《", "》", P), (13, "，", "译", P),
    (16390, "，第", "页", P), (12, "，", "", P),
    (3, "，", "年版", P), (0, "。", "", P),
]
FAMILIES["international"][0] = [
    (2, "", "：", P), (4, "《", "》", P), (6, "，载《", "》", P),
    (3, "，", "年", P), (8, "第", "期", P),
    (16390, "，第", "页", P), (0, "。", "", P),
]
FAMILIES["law_manual"][0] = [
    (2, "", "：", P), (4, "《", "》", P), (6, "，载《", "》", P),
    (3, "", "年", P), (8, "第", "期", P),
    (16390, "，第", "页", P), (0, "。", "", P),
]
FAMILIES["law_review"][0] = [
    (2, "", "：", P), (4, "“", "”", P), (6, "，《", "》", P),
    (3, "", "年", P), (8, "第", "期", P),
    (16390, "，第", "页", P), (0, "。", "", P),
]
FAMILIES["law_review"][7] = [
    (2, "", "：", P), (4, "“", "”", P), (10, "，载", "主编：", P),
    (6, "《", "》", P), (12, "，", "", P), (3, "", "年版", P),
    (16390, "，第", "页", P), (0, "。", "", P),
]


def gbt_specs(year: int):
    # A compact EndNote mapping of the common note-mode GB/T layouts.
    return {
        1: [(2, "", ". ", P), (4, "", "[M]. ", P), (13, "", "，译. ", P),
            (14, "", "版. ", P), (11, "", "：", P), (12, "", "，", P),
            (3, "", "：", P), (16390, "", "", P), (0, ".", "", P)],
        0: [(2, "", ". ", P), (4, "", "[J]. ", P), (6, "", "，", P),
            (3, "", "，", P), (7, "", "", P), (8, "（", "）", P),
            (16390, "：", "", P), (0, ".", "", P)],
        7: [(2, "", ". ", P), (4, "", "[C]//", P), (10, "", ". ", P),
            (6, "", ". ", P), (11, "", "：", P), (12, "", "，", P),
            (3, "", "：", P), (16390, "", "", P), (0, ".", "", P)],
        2: [(2, "", ". ", P), (4, "", "[D]. ", P), (12, "", "，", P),
            (3, "", "：", P), (16390, "", "", P), (0, ".", "", P)],
        5: [(2, "", ". ", P), (4, "", "[N]. ", P), (6, "", "，", P),
            (17, "", "（", P), (14, "", "）", P), (0, ".", "", P)],
        16: [(2, "", ". ", P), (4, "", "[EB/OL]. ", P),
             (20, "", "[", P), (8, "", "]. ", P), (17, "", "", P),
             (0, ".", "", P)],
        EN_BOOK: [(10, "", ". ", P), (4, "", "[M]. ", P),
                  (11, "", ": ", P), (12, "", ", ", P), (3, "", ": ", P),
                  (16390, "", "", P), (0, ".", "", P)],
        EN_JOURNAL: [(10, "", ". ", P), (4, "", "[J]. ", P),
                     (6, "", ", ", I), (3, "", ", ", P),
                     (7, "", "", P), (8, "(", ")", P),
                     (16390, ": ", "", P), (0, ".", "", P)],
        EN_BOOK_SECTION: [(10, "", ". ", P), (4, "", "[C]//", P),
                          (6, "", ". ", I), (11, "", ": ", P),
                          (12, "", ", ", P), (3, "", ": ", P),
                          (16390, "", "", P), (0, ".", "", P)],
    }


FAMILIES["gbt2015"] = gbt_specs(2015)
FAMILIES["gbt2025"] = gbt_specs(2025)


def clean_examples(source_dir: str) -> list[str]:
    if not EXAMPLES_PATH.exists():
        return []
    entries = json.loads(EXAMPLES_PATH.read_text(encoding="utf-8"))
    match = next((item for item in entries if item["source_dir"] == source_dir), None)
    return list(match.get("examples", []))[:8] if match else []


def build_one(number: int, title: str, source_dir: str, family: str, status: str):
    base = BASE.read_bytes()
    parser = ENSParser(base)
    root = parser.parse()
    donors = donor_tokens(root)
    metadata = child(root, 0x1001)
    display = f"中文学术－{title} 0.1"
    child(metadata, 0x1010).text = display
    child(metadata, 0x1013).text = f"参考 {source_dir} CSL 规则的 EndNote X9 脚注预览样式"
    # Description field.
    for item in metadata.children:
        if item.tag == 0x1015:
            for sub in item.children:
                if sub.kind == 2:
                    sub.text = (
                        f"{status}。规则来源：zotero-chinese/styles/{source_dir}；"
                        "底层机制来源于CSS Footnotes 2026 Alpha 2.7。"
                        "脚注序号由Word/WPS控制；不生成文末参考文献表。"
                    )
                    break
            break

    footnotes = child(root, 0x1004)
    full = template_map(child(footnotes, 0x1055))
    if family != "css":
        for ref_type, spec in FAMILIES[family].items():
            if ref_type in full:
                rewrite_template(full[ref_type], spec, donors)

    safe_title = title.replace("/", "-").replace("：", "-")
    filename = f"{number:02d} {safe_title}.ens"
    raw = serialize_ens(root, parser.endian)
    # Structural round trip is mandatory for every generated style.
    ENSParser(raw).parse()
    (OUT / filename).write_bytes(raw)
    return {
        "number": number,
        "title": title,
        "display_name": display,
        "filename": filename,
        "family": family,
        "status": status,
        "source_dir": source_dir,
        "source_url": f"https://github.com/zotero-chinese/styles/tree/main/src/{source_dir}",
        "examples": clean_examples(source_dir),
        "sha256": hashlib.sha256(raw).hexdigest(),
        "bytes": len(raw),
    }


def build_selector(items):
    cards = []
    for item in items:
        examples = "".join(f"<li>{html.escape(x)}</li>" for x in item["examples"][:4])
        cards.append(f"""
        <article class="card" data-q="{html.escape(item['title'] + ' ' + item['family'])}">
          <div class="row"><b>{item['number']:02d}　{html.escape(item['title'])}</b><span>{item['status']}</span></div>
          <p>规则族：{html.escape(item['family'])}　·　文件：{html.escape(item['filename'])}</p>
          <details><summary>查看来源样例</summary><ol>{examples}</ol></details>
        </article>""")
    page = f"""<!doctype html><html lang="zh-CN"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1"><title>EndNote X9 中文学术格式中心</title>
<style>body{{font-family:"Microsoft YaHei",sans-serif;background:#f4f7fb;color:#17223b;margin:0}}header{{background:#173764;color:white;padding:28px max(5vw,24px)}}main{{max-width:1080px;margin:24px auto;padding:0 20px}}input{{width:100%;box-sizing:border-box;padding:13px;border:1px solid #b9c5d8;border-radius:10px;font-size:16px}}.grid{{display:grid;grid-template-columns:repeat(auto-fit,minmax(320px,1fr));gap:14px;margin-top:18px}}.card{{background:white;border:1px solid #dce4ef;border-radius:12px;padding:16px;box-shadow:0 2px 10px #18345c12}}.row{{display:flex;justify-content:space-between;gap:12px}}span{{background:#e9f1ff;color:#234f90;border-radius:999px;padding:4px 8px;font-size:12px}}p,li{{line-height:1.65}}summary{{cursor:pointer;color:#2459a5}}footer{{color:#64748b;margin:28px 0}}</style></head><body>
<header><h1>EndNote X9 中文学术格式中心</h1><p>18套脚注样式 · 中英日文共用类型 · Word/WPS · Cited Pages</p></header><main>
<input id="search" placeholder="搜索期刊、出版社、学科或规则族">
<div class="grid">{''.join(cards)}</div>
<footer>《中国社会科学（2026）》已完成X9/WPS基础实测；其他样式为预览版，使用前请在文档副本中核对。</footer></main>
<script>const q=document.querySelector('#search');q.oninput=()=>{{for(const c of document.querySelectorAll('.card'))c.hidden=!c.dataset.q.toLowerCase().includes(q.value.toLowerCase())}}</script></body></html>"""
    (PACKAGE / "格式选择中心.html").write_text(page, encoding="utf-8")


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    PACKAGE.mkdir(parents=True, exist_ok=True)
    items = [build_one(*spec) for spec in STYLES]
    (PACKAGE / "Styles").mkdir(exist_ok=True)
    for item in items:
        src = OUT / item["filename"]
        (PACKAGE / "Styles" / item["filename"]).write_bytes(src.read_bytes())
    (PACKAGE / "style-manifest.json").write_text(
        json.dumps(items, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    build_selector(items)
    print(f"generated {len(items)} styles in {OUT}")


if __name__ == "__main__":
    main()
