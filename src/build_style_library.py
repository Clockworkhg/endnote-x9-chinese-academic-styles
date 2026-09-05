#!/usr/bin/env python3
"""Generate an EndNote X9 Chinese academic footnote style library.

The stable CSS 2026 Alpha 2.7 ENS is the shared binary/template base.  Each
preview style applies a small, auditable family override derived from the
corresponding CSL examples in zotero-chinese/styles.  The library covers every
upstream CSL style whose class is ``note`` at the pinned catalog revision, plus
an experimental China Communication University humanities profile based on
public CUC guidance.  The resulting files are preview builds: they are
structurally validated here and require Word/WPS output testing before being
presented as journal-conformant releases.
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
    (
        19,
        "中国传媒大学（人文社科脚注参考版）",
        "中国传媒大学（人文社科脚注参考版）",
        "cuc",
        "实验版",
        "https://scim.cuc.edu.cn/2025/1207/c2673a264878/page.htm",
    ),
    (20, "GB/T 7714—2015（注释·无URL/DOI）", "GB-T-7714—2015（注释，双语，姓名不大写，无URL、DOI，重复引用不省略）", "gbt2015_clean", "预览版"),
    (21, "GB/T 7714—2025（注释·无URL/DOI）", "GB-T-7714—2025（注释，双语，无URL，无DOI，重复引用不省略）", "gbt2025_clean", "预览版"),
    (22, "世界经济与政治论坛", "世界经济与政治论坛", "gbt2015", "预览版"),
    (23, "中国现代文学研究丛刊", "中国现代文学研究丛刊", "literature", "预览版"),
    (24, "关东学刊", "关东学刊", "history", "预览版"),
    (25, "华东理工大学（社会与公共管理学院）", "华东理工大学-社会与公共管理学院", "university_note", "预览版"),
    (26, "南京农业大学（人文社科类·脚注）", "南京农业大学（人文社科类，脚注）", "gbt2015", "预览版"),
    (27, "南京理工大学学报（社会科学版）", "南京理工大学学报（社会科学版）", "technical", "预览版"),
    (28, "南方民族考古", "南方民族考古", "history", "预览版"),
    (29, "国际关系研究", "国际关系研究", "international", "预览版"),
    (30, "国际安全研究", "国际安全研究", "international", "预览版"),
    (31, "国际法研究", "国际法研究", "law_manual", "预览版"),
    (32, "外国文学评论", "外国文学评论", "literature", "预览版"),
    (33, "太平洋学报", "太平洋学报", "international", "预览版"),
    (34, "学术评论", "学术评论", "literature", "预览版"),
    (35, "当代亚太", "当代亚太", "international", "预览版"),
    (36, "探索与争鸣", "探索与争鸣", "literature", "预览版"),
    (37, "教育史研究", "教育史研究", "history", "预览版"),
    (38, "文艺争鸣", "文艺争鸣", "literature", "预览版"),
    (39, "法学引注手册（多语言）", "法学引注手册（多语言）", "law_manual", "预览版"),
    (40, "法学引注手册（多语言·重复不省略）", "法学引注手册（多语言，重复引用不省略）", "law_manual", "预览版"),
    (41, "法学引注手册（第二版·重复不省略）", "法学引注手册（第二版，多语言，重复引用不省略）", "law_manual", "预览版"),
    (42, "法学引注手册（第二版·标注页码）", "法学引注手册（第二版，多语言，重复引用不省略，标注页码）", "law_manual", "预览版"),
    (43, "湖南大学（脚注）", "湖南大学（脚注）", "hunan_note", "预览版"),
    (44, "社会科学", "社会科学", "technical", "预览版"),
    (45, "臺大中文學報", "臺大中文學報", "taiwan_note", "预览版"),
    (46, "西南政法大学", "西南政法大学", "law_review", "预览版"),
    (47, "马克思主义研究", "马克思主义研究", "marxism", "预览版"),
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
    "university_note": cn_common(place=True, year_edition=True),
    "marxism": cn_common(place=True),
    "cuc": cn_common(place=False),
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

# China Communication University humanities/publication practice: footnote
# numbering remains a Word/WPS responsibility; publication place is normally
# omitted in the cited CUC examples, while publisher, year and cited page stay.
FAMILIES["cuc"][1] = [
    (2, "", "：", P), (4, "《", "》", P), (14, "（第", "版）", P),
    (13, "，", "译", P), (12, "，", "", P), (3, "，", "年", P),
    (16390, "，第", "页", P), (0, "。", "", P),
]
FAMILIES["cuc"][0] = [
    (2, "", "：", P), (4, "《", "》", P), (6, "，《", "》", P),
    (3, "", "年", P), (8, "第", "期", P),
    (16390, "，第", "页", P), (0, "。", "", P),
]

# Hunan University's note profile uses full-width periods and bare page
# numbers rather than Chinese title marks or the 第…页 wrapper.
FAMILIES["hunan_note"] = {
    ref_type: [
        (2, "", "．", P), (4, "", "．", P),
        *(([(13, "", "译．", P)] if ref_type in (1, 7) else [])),
        (11, "", "：", P), (12, "", "，", P), (3, "", "，", P),
        (16390, "", "", P), (0, "", "", P),
    ]
    for ref_type in (1, 0, 7, 2)
}
FAMILIES["hunan_note"].update({
    5: [(2, "", "．", P), (4, "", "．", P), (6, "", "，", P),
        (17, "", "", P), (0, "", "", P)],
    16: [(2, "", "．", P), (4, "", "．", P), (17, "", "，", P),
         (20, "", "", P), (0, "", "", P)],
})

# Traditional-Chinese note conventions used by 臺大中文學報.
FAMILIES["taiwan_note"] = cn_common(author_end="：", place=True)
FAMILIES["taiwan_note"][0] = [
    (2, "", "：", P), (4, "〈", "〉", P), (6, "，《", "》", P),
    (8, "第", "期", P), (3, "（", "年）", P),
    (16390, "，頁", "", P), (0, "。", "", P),
]
for ref_type in (1, 2, 7):
    FAMILIES["taiwan_note"][ref_type] = [
        (2, "", "：", P), (4, "《", "》", P), (13, "，", "譯", P),
        (11, "（", "：", P), (12, "", "，", P), (3, "", "年）", P),
        (16390, "，頁", "", P), (0, "。", "", P),
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
FAMILIES["gbt2015_clean"] = copy.deepcopy(FAMILIES["gbt2015"])
FAMILIES["gbt2025_clean"] = copy.deepcopy(FAMILIES["gbt2025"])
for family in ("gbt2015_clean", "gbt2025_clean"):
    FAMILIES[family][16] = [
        (2, "", ". ", P), (4, "", "[EB/OL]. ", P),
        (20, "", "[", P), (8, "", "].", P), (0, "", "", P),
    ]


def clean_examples(source_dir: str) -> list[str]:
    if not EXAMPLES_PATH.exists():
        return []
    entries = json.loads(EXAMPLES_PATH.read_text(encoding="utf-8"))
    match = next((item for item in entries if item["source_dir"] == source_dir), None)
    return list(match.get("examples", []))[:8] if match else []


def build_one(
    number: int,
    title: str,
    source_dir: str,
    family: str,
    status: str,
    source_url: str | None = None,
):
    base = BASE.read_bytes()
    parser = ENSParser(base)
    root = parser.parse()
    donors = donor_tokens(root)
    metadata = child(root, 0x1001)
    display = f"中文学术－{title} 0.1"
    child(metadata, 0x1010).text = display
    upstream_url = source_url or f"https://github.com/zotero-chinese/styles/tree/main/src/{source_dir}"
    source_label = "中国传媒大学公开规范" if source_url else f"zotero-chinese/styles/{source_dir}"
    child(metadata, 0x1013).text = f"参考 {source_label} 的 EndNote X9 脚注预览样式"
    # Description field.
    for item in metadata.children:
        if item.tag == 0x1015:
            for sub in item.children:
                if sub.kind == 2:
                    sub.text = (
                        f"{status}。规则来源：{source_label}；"
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
        "source_url": upstream_url,
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
<header><h1>EndNote X9 中文学术格式中心</h1><p>{len(items)}套脚注样式 · 中英日文共用类型 · Word/WPS · Cited Pages</p></header><main>
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
