"""Check normal and delayed imports against every packaged DLL export table."""
from pathlib import Path
import sys
import pefile

def check(root):
    files={p.name.lower():p for p in root.iterdir() if p.suffix.lower() in ('.exe','.dll')}
    exports={}; imports={}
    for name,path in files.items():
        pe=pefile.PE(str(path),fast_load=True)
        pe.parse_data_directories(directories=[0,1,13])
        symbols=getattr(getattr(pe,'DIRECTORY_ENTRY_EXPORT',None),'symbols',[])
        exports[name]={s.name for s in symbols if s.name} | {('#'+str(s.ordinal)).encode() for s in symbols}
        imports[name]=[(entry.dll.decode().lower(),[(i.name if i.name else ('#'+str(i.ordinal)).encode()) for i in entry.imports]) for entry in getattr(pe,'DIRECTORY_ENTRY_IMPORT',[])+getattr(pe,'DIRECTORY_ENTRY_DELAY_IMPORT',[])]
        pe.close()
    missing=[]
    for name,entries in imports.items():
        for dll,symbols in entries:
            if dll not in files:continue
            for symbol in symbols:
                if symbol not in exports[dll]:missing.append(name+' -> '+dll+': '+symbol.decode(errors='replace'))
    if missing:raise RuntimeError('Unresolved packaged symbols:\n'+'\n'.join(missing))
    print('Verified internal PE imports and exports for',len(files),'packaged binaries')

if __name__=='__main__':check(Path(sys.argv[1]))
