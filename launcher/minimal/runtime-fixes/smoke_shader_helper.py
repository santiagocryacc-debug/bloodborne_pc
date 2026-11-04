from pathlib import Path
import ctypes
import subprocess
import sys
import tempfile

def run(executable):
    previous=ctypes.windll.kernel32.SetErrorMode(0x8003)
    try:
        with tempfile.TemporaryDirectory(prefix="bb-shader-smoke-") as folder:
            result=subprocess.run([str(executable),"--recompile-cache",folder],cwd=executable.parent,capture_output=True,timeout=30,creationflags=subprocess.CREATE_NO_WINDOW)
            if result.returncode or b"Shader cache:" not in result.stdout:
                raise RuntimeError("Shader helper failed: exit "+hex(result.returncode & 0xffffffff)+" "+result.stderr.decode(errors="replace"))
            print(result.stdout.decode(errors="replace").strip())
    finally:ctypes.windll.kernel32.SetErrorMode(previous)

if __name__=="__main__":run(Path(sys.argv[1]).resolve())
