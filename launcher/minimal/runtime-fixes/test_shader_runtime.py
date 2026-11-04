from pathlib import Path
import importlib.util
import shutil
import tempfile
import sys
sys.path.insert(0, str(Path(__file__).parent))
from fix_shader_lab_runtime import apply, patched_shader, SHADER_SHA256, PRIVATE_DLL

def run(official):
    import hashlib
    original = (official / "bb-shader-lab.exe").read_bytes()
    patched = patched_shader(original)
    assert len(original) == len(patched)
    assert hashlib.sha256(original).hexdigest() == SHADER_SHA256
    assert 0 < sum(a != b for a, b in zip(original, patched)) <= len(PRIVATE_DLL)
    with tempfile.TemporaryDirectory() as folder:
        target=Path(folder)
        shutil.copyfile(official/"bb-shader-lab.exe", target/"bb-shader-lab.exe")
        apply(official,target)
        once=(target/"bb-shader-lab.exe").read_bytes()
        apply(official,target)
        assert (target/"bb-shader-lab.exe").read_bytes()==once
        (target/"bb-shader-lab.exe").write_bytes(b"unknown executable")
        (target/"bbproto_old.dll").unlink()
        try:apply(official,target)
        except ValueError:pass
        else:raise AssertionError("Unknown shader executable accepted")
        assert not (target/"bbproto_old.dll").exists()
    print("Shader runtime patch checks passed")

if __name__=="__main__":run(Path(sys.argv[1]))
