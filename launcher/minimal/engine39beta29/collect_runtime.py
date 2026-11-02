"""Collect the complete DLL closure of the patched executables from CLANG64."""
from pathlib import Path
import argparse
import shutil
import os
import struct


def import_names(path: Path) -> list[str]:
    """Read normal PE imports without relying on a compiler tool's location."""
    data = path.read_bytes()
    if data[:2] != b'MZ' or len(data) < 64:
        raise RuntimeError(f'Not a PE image: {path.name}')
    pe = struct.unpack_from('<I', data, 60)[0]
    if data[pe:pe + 4] != b'PE\0\0':
        raise RuntimeError(f'Invalid PE signature: {path.name}')
    section_count = struct.unpack_from('<H', data, pe + 6)[0]
    optional_size = struct.unpack_from('<H', data, pe + 20)[0]
    optional = pe + 24
    magic = struct.unpack_from('<H', data, optional)[0]
    if magic not in (0x10b, 0x20b):
        raise RuntimeError(f'Unsupported PE image: {path.name}')
    directories = optional + (112 if magic == 0x20b else 96)
    import_rva = struct.unpack_from('<I', data, directories + 8)[0]
    if not import_rva:
        return []
    sections = []
    for index in range(section_count):
        offset = optional + optional_size + index * 40
        virtual_size, virtual_address, raw_size, raw_address = struct.unpack_from('<IIII', data, offset + 8)
        sections.append((virtual_address, max(virtual_size, raw_size), raw_address))

    def offset_of(rva):
        for address, size, raw in sections:
            if address <= rva < address + size:
                offset = raw + rva - address
                if 0 <= offset < len(data):
                    return offset
        raise RuntimeError(f'Invalid PE import address in {path.name}')

    offset = offset_of(import_rva)
    names = []
    while offset + 20 <= len(data):
        descriptor = struct.unpack_from('<IIIII', data, offset)
        if not any(descriptor):
            return names
        begin = offset_of(descriptor[3])
        end = data.index(b'\0', begin)
        name = data[begin:end].decode('ascii')
        if not name or Path(name).name != name or '/' in name or '\\' in name:
            raise RuntimeError(f'Invalid PE library name in {path.name}')
        names.append(name)
        offset += 20
    raise RuntimeError(f'Unterminated PE imports in {path.name}')


def collect(source: Path, destination: Path, runtime: Path) -> None:
    if not runtime.is_dir():
        raise RuntimeError(f'Compiler runtime folder not found: {runtime}')
    system = Path(os.environ['SystemRoot']) / 'System32'
    queue = [destination / 'bb-probe.exe', destination / 'bb-engine-assets.exe']
    seen = set()
    while queue:
        path = queue.pop()
        if path.name.lower() in seen:
            continue
        seen.add(path.name.lower())
        for name in import_names(path):
            target = destination / name
            candidate = runtime / name
            if not candidate.is_file():
                candidate = source / name
            if candidate.is_file():
                if not target.exists():
                    shutil.copy2(candidate, target)
                queue.append(target)
            elif target.is_file():
                queue.append(target)
            elif not (system / name).is_file() and not name.lower().startswith(('api-ms-', 'ext-ms-')):
                raise RuntimeError(f'Missing native dependency: {name}')
    print(f'Checked {len(seen)} native executable/library imports')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('destination', type=Path)
    parser.add_argument('runtime', type=Path)
    args = parser.parse_args()
    collect(args.source, args.destination, args.runtime)
