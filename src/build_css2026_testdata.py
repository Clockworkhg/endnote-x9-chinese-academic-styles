#!/usr/bin/env python3
"""Generate the CSS 2026 EndNote test corpus and audit-tool sample XML."""

from __future__ import annotations

import csv
import json
import xml.etree.ElementTree as ET
from pathlib import Path


OUT = Path("build/testdata")


def c(case_id, ris_type, scene, title, authors=(), year="", language="Chinese", **fields):
    return {
        "id": case_id,
        "ris_type": ris_type,
        "scene": scene,
        "title": title,
        "authors": list(authors),
        "year": year,
        "language": language,
        **fields,
    }


CASES = [
    c("T01", "BOOK", "中文专著—单一作者", "单一作者图书", ["陈明"], "2021", place="北京", publisher="人民出版社", cited="8"),
    c("T02", "BOOK", "中文专著—两位作者", "两位作者图书", ["马克思", "恩格斯"], "2018", place="北京", publisher="人民出版社", cited="42–45"),
    c("T03", "BOOK", "中文译著", "正义论", ["罗尔斯"], "2009", place="北京", publisher="中国社会科学出版社", translator="何怀宏", cited="55"),
    c("T04", "BOOK", "中文著作—三位作者", "三位作者图书", ["赵一", "钱二", "孙三"], "2019", place="上海", publisher="复旦大学出版社", cited="11"),
    c("T05", "BOOK", "中文著作—四位作者", "四位作者截断测试", ["赵一", "钱二", "孙三", "李四"], "2023", place="北京", publisher="北京大学出版社", cited="20"),
    c("T06", "BOOK", "中文著作—机构作者", "机构作者年度研究", ["中国社会科学院政治学研究所,"], "2024", place="北京", publisher="社会科学文献出版社", cited="12"),
    c("T07", "BOOK", "中文著作—无作者", "无作者图书", [], "2017", place="北京", publisher="中华书局", cited="9"),
    c("T08", "BOOK", "中文著作—卷次", "马克思恩格斯全集", [], "2002", place="北京", publisher="人民出版社", volume="3", cited="209"),
    c("T09", "BOOK", "中文著作—版次", "政治学概论", ["王强"], "2022", place="北京", publisher="高等教育出版社", edition="2", cited="30"),
    c("T10", "BOOK", "中文著作—缺出版地", "缺少出版地的图书", ["吴六"], "2017", publisher="中华书局", cited="9"),
    c("T11", "BOOK", "中文著作—缺出版社", "缺少出版社的图书", ["郑七"], "2016", place="南京", cited="18"),
    c("T12", "BOOK", "中文著作—分散页码", "分散页码测试", ["周八"], "2020", place="广州", publisher="中山大学出版社", cited="55、88"),
    c("T13", "JOUR", "中文期刊—年期", "21世纪马克思主义的基础性问题", ["韩庆祥"], "2022", journal="中国社会科学", issue="4", cited="10"),
    c("T14", "JOUR", "中文期刊—两位作者", "数字时代的公共治理", ["张三", "李四"], "2025", journal="中国行政管理", issue="3", cited="12–13"),
    c("T15", "JOUR", "中文期刊—三位作者", "三位作者期刊论文", ["甲一", "乙二", "丙三"], "2023", journal="公共管理学报", issue="2", cited="75"),
    c("T16", "JOUR", "中文期刊—四位作者", "四位作者期刊论文", ["甲一", "乙二", "丙三", "丁四"], "2023", journal="政治学研究", issue="6", cited="75"),
    c("T17", "JOUR", "中文期刊—卷期", "卷期齐全的期刊论文", ["林九"], "2024", journal="学术月刊", volume="56", issue="5", cited="35"),
    c("T18", "JOUR", "中文期刊—完整日期", "早期期刊日期测试", ["李初黎"], "1937", journal="解放", volume="1", issue="24", date="1937年11月20日"),
    c("T19", "JOUR", "中文期刊—译文", "论德国刑法教义学的方法", ["路易斯·格雷克"], "2025", journal="法治现代化研究", issue="1", translator="郑力凡", cited="22"),
    c("T20", "JOUR", "中文期刊—同名异地", "回忆父亲董希文", ["董一沙"], "2001", journal="传记文学（北京）", issue="3", cited="18"),
    c("T21", "JOUR", "中文期刊—题名特殊标点", "书名中的问号？冒号：破折号——测试", ["刘七"], "2022", journal="新闻与传播研究", issue="8", cited="105", doi="10.1234/test.2022.8"),
    c("T22", "JOUR", "中文期刊—未填具体引文页", "无Cited Pages测试", ["钱十三"], "2020", journal="社会学研究", issue="1", pages="10–25"),
    c("T23", "CHAP", "中文析出—单一作者", "中国史叙论", ["梁启超"], "1999", book_title="梁启超全集", volume="1", place="北京", publisher="北京出版社", cited="448–454"),
    c("T24", "CHAP", "中文析出—带编者", "公共治理中的协同生产", ["赵六"], "2023", editors=["孙七"], book_title="公共治理新论", place="北京", publisher="社会科学文献出版社", cited="60"),
    c("T25", "CHAP", "中文析出—带译者", "后人类的可能", ["克里斯·哈布尔斯·格雷"], "2004", translator="张立英", editors=["曹荣湘"], book_title="后人类文化", place="上海", publisher="上海三联书店", cited="7"),
    c("T26", "CHAP", "中文析出—同一责任者", "学术研究方法", ["陈明"], "2020", book_title="陈明文集", place="北京", publisher="商务印书馆", cited="28"),
    c("T27", "CHAP", "中文析出—序言", "传统、克里斯玛和理性化——译序", ["傅铿"], "2014", book_title="论传统", place="上海", publisher="上海人民出版社", cited="4"),
    c("T28", "CHAP", "中文析出—缺编者", "缺少编者的图书章节", ["郑八"], "2020", book_title="基层治理研究", place="武汉", publisher="武汉大学出版社", cited="25"),
    c("T29", "THES", "硕士学位论文", "内蒙古和林格尔县东头号墓地人骨研究", ["朱思媚"], "2016", university="吉林大学考古学系", degree="硕士", cited="51–52"),
    c("T30", "THES", "博士学位论文", "基层治理中的政策适配研究", ["王五"], "2024", university="中国传媒大学政府与公共事务学院", degree="博士", cited="76"),
    c("T31", "THES", "学位论文—缺学校", "缺少学校的学位论文", ["林九"], "2022", degree="硕士", cited="36"),
    c("T32", "CONF", "会议论文", "再生育研究的理论和方法", ["尹文耀"], "2016", conference="第二届社会科学研究方法与应用学术研讨会", place="北京"),
    c("T33", "CONF", "会议论文—有页码", "会议论文具体页码测试", ["黄十二"], "2023", conference="国家治理现代化论坛", place="北京", cited="90"),
    c("T34", "RPRT", "机构报告", "中国公共治理年度报告", ["某研究中心,"], "2025", place="北京", publisher="某研究中心", cited="12"),
    c("T35", "NEWS", "中文报纸—有作者", "以高度文化自觉自信担负起新的文化使命", ["常江"], "2023", journal="中国社会科学报", date="2023年12月26日", edition="1"),
    c("T36", "NEWS", "中文报纸—无作者", "正确理解和大力推进中国式现代化", [], "2023", journal="人民日报", date="2023年2月8日", edition="1"),
    c("T37", "NEWS", "中文报纸—同名异地", "损蚀冲洗下的乡土", ["费孝通"], "1947", journal="大公报", place="上海", date="1947年11月30日", edition="2"),
    c("T38", "NEWS", "中文报纸—页码", "英国印花税章程续编", ["刘镜人"], "1900", journal="江南商务报", issue="15", date="光绪二十六年六月廿一日", cited="9–12"),
    c("T39", "NEWS", "中文报纸—卷号与日期", "吾国银行放款事业之观察", ["郑维钧"], "1922", journal="银行周报", volume="6", issue="47", date="1922年12月5日", cited="11–14"),
    c("T40", "ELEC", "政府网页", "中国关于世贸组织改革的立场文件", ["商务部,"], "2018", date="2018年12月17日", url="https://example.gov.cn/document", access="2026年5月31日"),
    c("T41", "ELEC", "网页—缺更新日期", "缺少更新日期的网页", ["陈十一"], "", url="https://example.org/page", access="2026年6月1日"),
    c("T42", "ELEC", "网页—复杂URL", "复杂URL保持半角测试", ["某政府部门,"], "2025", date="2025年3月2日", url="https://example.org/a/b?q=x%20y&lang=zh", access="2026年6月2日"),
    c("T43", "GOVDOC", "政府文件—出版项", "国家治理现代化指导意见", ["某市人民政府,"], "2024", place="某市", publisher="某市人民政府"),
    c("T44", "GOVDOC", "政府文件—网址", "政府信息公开文件", ["某部门,"], "2025", url="https://example.gov.cn/open", cited="3"),
    c("T45", "MANSCPT", "档案—有作者", "奏为爱民恤吏敬陈管见事", ["陈官俊"], "1820", date="嘉庆二十五年十月十二日", collection="宫中奏折", archive_no="04—01—13—0217—001", publisher="中国第一历史档案馆"),
    c("T46", "MANSCPT", "档案—无作者", "外务部致吕大臣、盛大臣", [], "1902", date="1902年2月27日", collection="盛宣怀档案", archive_no="004220", publisher="上海图书馆"),
    c("T47", "CLSWK", "古籍—刻本", "古今伪书考", ["姚际恒"], "1877", label="卷3，光绪三年苏州文学山房活字本", cited="9页a"),
    c("T48", "CLSWK", "古籍—点校整理本", "尚书正义", ["孔安国传，孔颖达正义,"], "2007", label="卷13《周书·康诰第十一》", editors=["黄怀信"], place="上海", publisher="上海古籍出版社", cited="529"),
    c("T49", "CLSWK", "古籍—影印本", "太平御览", [], "1985", label="卷690《服章部七》引《魏台访议》，影印本", place="北京", publisher="中华书局", cited="3080"),
    c("T50", "CLSWK", "古籍—丛书析出", "近光集", ["周伯琦"], "1986", label="卷2《立秋日书事五首》，景印文渊阁《四库全书》第1214册", place="台北", publisher="台湾商务印书馆", cited="523"),
    c("T51", "CLSWK", "古籍—地方志", "嘉定县志", [], "", label="卷12，乾隆本《风俗》", cited="7"),
    c("T52", "CLSWK", "常用基本典籍", "旧唐书", [], "1975", label="卷9《玄宗纪下》", place="北京", publisher="中华书局", cited="233"),
    c("T53", "CLSWK", "年号换算", "资治通鉴", [], "1956", label="卷194，唐太宗贞观十年（636）十二月条", place="北京", publisher="中华书局", cited="6124"),
    c("T54", "GEN", "转引文献", "汪玉山集·乞申严元置斥堠铺指挥札子", ["汪应辰"], "1986", note="转引自《永乐大典》卷14575，北京：中华书局，1986年，第6458页"),
    c("T55", "BOOK", "英文专著—单一作者", "Troubling Confessions: Speaking Guilt in Law and Literature", ["Brooks, Peter"], "2000", "English", place="Chicago", publisher="University of Chicago Press", cited="48"),
    c("T56", "BOOK", "英文专著—两位作者", "The Arts of Power: Three Halls of State in Italy, 1300-1600", ["Starn, Randolph", "Partridge, Loren"], "1992", "English", place="Berkeley", publisher="California University Press", cited="19–28"),
    c("T57", "BOOK", "英文译著", "The Travels of Marco Polo", ["Polo, M."], "1997", "English", translator="Marsden, William", place="Hertfordshire", publisher="Cumberland House", cited="55, 88"),
    c("T58", "JOUR", "英文期刊论文", "On the Search for Civil Society in China", ["Chamberlain, Heath B."], "1993", "English", journal="Modern China", volume="19", issue="2", date="April 1993", cited="199–215"),
    c("T59", "CHAP", "英文文集析出", "The Impact of Scarcity and Plenty on Population Change in England", ["Schofield, R. S."], "1983", "English", editors=["Rotberg, R. I.", "Rabb, T. K."], book_title="Hunger and History: The Impact of Changing Food Production and Consumption Pattern on Society", place="Cambridge, Mass.", publisher="Cambridge University Press", cited="79"),
    c("T60", "JOUR", "英文期刊—四位作者", "English Author Truncation Test", ["Smith, John", "Jones, Mary", "Brown, Alice", "White, David"], "2024", "English", journal="Journal of Test Studies", volume="10", issue="2", cited="20–23"),
    c("T61", "BOOK", "日文著作", "宋代礼説研究", [], "1996", "Japanese", formatted_author="山根三芳", publisher="渓水社", cited="153"),
    c("T62", "CHAP", "日文析出文献", "『反応』から『理念』へ―対アフリカ外交", [], "2013", "Japanese", formatted_author="遠藤貢", formatted_editor="国分良成", book_title="日本の外交 第４巻 対外政策 地域編", publisher="岩波書店", cited="300"),
    c("T63", "JOUR", "日文期刊论文", "キャンパスの文章", [], "1995", "Japanese", formatted_author="梅津彰人", journal="國文學: 解釈と教材の研究", volume="40", issue="2", cited="71–74"),
    c("T64", "GEN", "预排版特殊文献", "预排版脚注兜底测试", [], "", note="将完整注文填入 Complete Footnote 字段，样式不得添加或删除任何字符。"),
]


def ris_lines(case):
    out = [f"TY  - {case['ris_type']}", f"ID  - {case['id']}", f"KW  - CSS2026:{case['id']}"]
    out += [f"AU  - {a}" for a in case["authors"]]
    tag_map = {
        "title": "TI", "year": "PY", "journal": "JF", "volume": "VL",
        "issue": "IS", "place": "CY", "publisher": "PB", "book_title": "T2",
        "edition": "ET", "date": "DA", "url": "UR", "language": "LA",
        "label": "LB",
    }
    for key, tag in tag_map.items():
        value = case.get(key)
        if value:
            out.append(f"{tag}  - {value}")
    out += [f"ED  - {a}" for a in case.get("editors", [])]
    if case.get("pages"):
        out.append(f"SP  - {case['pages']}")
    if case.get("doi"):
        out.append(f"DO  - {case['doi']}")
    notes = [f"SCENE={case['scene']}", f"SUGGESTED_CITED_PAGES={case.get('cited','')}"]
    for key in ("translator", "access", "collection", "archive_no", "note", "degree", "university", "conference", "formatted_author", "formatted_editor"):
        if case.get(key):
            notes.append(f"{key.upper()}={case[key]}")
    out += [f"N1  - {note}" for note in notes]
    out += ["ER  - ", ""]
    return out


def build_ris():
    lines = []
    for case in CASES:
        lines.extend(ris_lines(case))
    (OUT / "test_references_css2026.ris").write_text("\r\n".join(lines), encoding="utf-8-sig")


def build_case_data():
    (OUT / "test_cases_css2026.json").write_text(
        json.dumps(CASES, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    with (OUT / "test_cases_css2026.csv").open("w", encoding="utf-8-sig", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(["编号", "场景", "初始RIS类型", "语种", "题名", "建议Cited Pages", "预期专用类型"])
        for case in CASES:
            suggested = ""
            if case["language"] == "English":
                suggested = {"BOOK": "English Book", "JOUR": "English Journal Article", "CHAP": "English Book Section"}.get(case["ris_type"], "")
            elif case["language"] == "Japanese":
                suggested = {"BOOK": "Japanese Book", "JOUR": "Japanese Journal Article", "CHAP": "Japanese Book Section"}.get(case["ris_type"], "")
            writer.writerow([case["id"], case["scene"], case["ris_type"], case["language"], case["title"], case.get("cited", ""), suggested])


def add_text(parent, tag, value):
    if value:
        ET.SubElement(parent, tag).text = str(value)


def build_audit_xml():
    root = ET.Element("xml")
    records = ET.SubElement(root, "records")
    type_names = {"BOOK": "Book", "JOUR": "Journal Article", "CHAP": "Book Section", "THES": "Thesis", "NEWS": "Newspaper Article", "ELEC": "Web Page", "CONF": "Conference Paper", "RPRT": "Report", "GOVDOC": "Government Document", "MANSCPT": "Manuscript", "CLSWK": "Classical Work", "GEN": "Generic"}
    for idx, case in enumerate(CASES, 1):
        rec = ET.SubElement(records, "record")
        add_text(rec, "rec-number", idx)
        ET.SubElement(rec, "ref-type", name=type_names.get(case["ris_type"], "Generic")).text = "0"
        contrib = ET.SubElement(rec, "contributors")
        authors = ET.SubElement(contrib, "authors")
        for author in case["authors"]:
            add_text(authors, "author", author)
        titles = ET.SubElement(rec, "titles")
        add_text(titles, "title", case["title"])
        add_text(titles, "secondary-title", case.get("book_title"))
        periodical = ET.SubElement(rec, "periodical")
        add_text(periodical, "full-title", case.get("journal"))
        dates = ET.SubElement(rec, "dates")
        add_text(dates, "year", case.get("year"))
        pub_dates = ET.SubElement(dates, "pub-dates")
        add_text(pub_dates, "date", case.get("date"))
        add_text(rec, "publisher", case.get("publisher") or case.get("university"))
        add_text(rec, "pub-location", case.get("place"))
        add_text(rec, "number", case.get("issue"))
        add_text(rec, "volume", case.get("volume"))
        add_text(rec, "degree", case.get("degree"))
        add_text(rec, "language", case.get("language"))
        if case.get("url"):
            urls = ET.SubElement(rec, "urls")
            related = ET.SubElement(urls, "related-urls")
            add_text(related, "url", case["url"])
    tree = ET.ElementTree(root)
    ET.indent(tree, space="  ")
    tree.write(OUT / "sample_endnote_export.xml", encoding="utf-8", xml_declaration=True)


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    build_ris()
    build_case_data()
    build_audit_xml()
    print(f"wrote {len(CASES)} test cases to {OUT}")
