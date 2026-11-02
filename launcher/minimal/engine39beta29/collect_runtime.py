"""Collect the complete DLL closure of the patched executables from CLANG64."""
from pathlib import Path
import argparse
import re
import shutil
import subprocess
import os


def collect(source: Path, destination: Path, runtime: Path) -> None:
    objdump = runtime / 'llvm-objdump.exe'
    system = Path(os.environ['SystemRoot']) / 'System32'
    queue = [destination / 'bb-probe.exe', destination / 'bb-engine-assets.exe']
    seen = set()
    while queue:
        path = queue.pop()
        if path.name.lower() in seen:
            continue
        seen.add(path.name.lower())
        text = subprocess.check_output([str(objdump), '-p', str(path)], text=True)
        for name in re.findall(r'DLL Name:\s*(\S+)', text):
            target = destination / name
            candidate = runtime / name
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
