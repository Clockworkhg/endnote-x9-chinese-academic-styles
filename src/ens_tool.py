#!/usr/bin/env python3
"""Read EndNote RSFTSTYL TLV containers without changing the source file."""

from __future__ import annotations

import argparse
import json
import struct
from dataclasses import dataclass, field
from pathlib import Path


@dataclass
class Node:
    tag: int
    kind: int
    value: int
    offset: int
    end: int
    text: str | None = None
    children: list["Node"] = field(default_factory=list)

    def as_dict(self) -> dict:
        result = {
            "tag": f"0x{self.tag:04x}",
            "kind": self.kind,
            "value": self.value,
            "offset": self.offset,
            "end": self.end,
        }
        if self.text is not None:
            result["text"] = self.text
        if self.children:
            result["children"] = [child.as_dict() for child in self.children]
        return result


class ENSParser:
    def __init__(self, data: bytes):
        self.data = data
        if len(data) < 24 or data[8:16] != b"RSFTSTYL":
            raise ValueError("Not an EndNote RSFTSTYL file")
        self.endian = "<" if data[:8] == bytes.fromhex("0008ff0000001000") else ">"

    def u16(self, pos: int) -> int:
        return struct.unpack_from(self.endian + "H", self.data, pos)[0]

    def u32(self, pos: int) -> int:
        return struct.unpack_from(self.endian + "I", self.data, pos)[0]

    def parse_node(self, pos: int) -> tuple[Node, int]:
        if pos + 8 > len(self.data):
            raise ValueError(f"Truncated record at {pos}")
        tag, kind, value = self.u16(pos), self.u16(pos + 2), self.u32(pos + 4)
        if kind == 1:
            end = pos + value
            if value < 8 or end > len(self.data):
                raise ValueError(f"Invalid container size {value} at {pos}")
            node = Node(tag, kind, value, pos, end)
            cursor = pos + 8
            while cursor < end:
                child, cursor = self.parse_node(cursor)
                node.children.append(child)
                if cursor < end and cursor + 2 <= len(self.data) and self.data[cursor:cursor + 2] == b"\xfb\xfb":
                    cursor += 2
            if cursor != end:
                raise ValueError(f"Container 0x{tag:04x} ends at {end}, parsed to {cursor}")
            return node, end
        if kind == 2:
            end = pos + value
            if value < 8 or end > len(self.data):
                raise ValueError(f"Invalid string size {value} at {pos}")
            encoding = "utf-16-le" if self.endian == "<" else "utf-16-be"
            text = self.data[pos + 8:end].decode(encoding, errors="replace")
            return Node(tag, kind, value, pos, end, text=text), end
        if kind == 3:
            return Node(tag, kind, value, pos, pos + 8), pos + 8
        raise ValueError(f"Unknown record kind {kind} at {pos}")

    def parse(self) -> Node:
        node, end = self.parse_node(16)
        if end != len(self.data):
            raise ValueError(f"Root ends at {end}; file ends at {len(self.data)}")
        return node


def serialize_node(node: Node, endian: str, *, root: bool = False) -> bytes:
    """Serialize a parsed or modified node, recalculating every record size."""
    header = lambda tag, kind, value: struct.pack(endian + "HHI", tag, kind, value)
    if node.kind == 3:
        return header(node.tag, node.kind, node.value)
    if node.kind == 2:
        encoding = "utf-16-le" if endian == "<" else "utf-16-be"
        payload = (node.text or "").encode(encoding)
        return header(node.tag, node.kind, 8 + len(payload)) + payload
    if node.kind == 1:
        pieces: list[bytes] = []
        for index, child in enumerate(node.children):
            raw = serialize_node(child, endian)
            pieces.append(raw)
            if index != len(node.children) - 1 and len(raw) % 4:
                pieces.append(b"\xfb\xfb")
        payload = b"".join(pieces)
        return header(node.tag, node.kind, 8 + len(payload)) + payload
    raise ValueError(f"Unknown record kind {node.kind}")


def serialize_ens(root: Node, endian: str) -> bytes:
    marker = bytes.fromhex("0008ff0000001000") if endian == "<" else bytes.fromhex("000800ff00000010")
    return marker + b"RSFTSTYL" + serialize_node(root, endian, root=True)


def walk(node: Node, path: tuple[int, ...] = ()):
    here = path + (node.tag,)
    yield here, node
    for child in node.children:
        yield from walk(child, here)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("file", type=Path)
    parser.add_argument("--json", action="store_true")
    parser.add_argument("--text", action="store_true")
    args = parser.parse_args()
    ens = ENSParser(args.file.read_bytes())
    root = ens.parse()
    if args.json:
        print(json.dumps(root.as_dict(), ensure_ascii=False, indent=2))
    elif args.text:
        for path, node in walk(root):
            if node.text is not None:
                print(f"{node.offset:7d} {'/'.join(f'{part:04x}' for part in path)} {node.text!r}")
    else:
        print(f"endian={ens.endian!r} size={len(ens.data)} root=0x{root.tag:04x}")


if __name__ == "__main__":
    main()
