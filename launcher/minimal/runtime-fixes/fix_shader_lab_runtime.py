"""Keep the pinned shader lab on its own matching Protobuf runtime.

The cinematic bb-probe uses a newer compiler runtime than the official shader lab.
Only the shader lab's DLL import filename changes; no machine code or symbols change.
Both inputs must match the pinned official v0.9.39-beta.29 release.
"""
from pathlib import Path
import argparse
import hashlib
import struct

SHADER_SHA256 = '01dedc37b153be122d57923b935a32cc1d217dcbe4fe4d8dcf97d0602aac0c52'
PROTO_SHA256 = '1c175e2064d7e66b8b0c7558195fe2ea7f7afdfdadd98077fcf19c61233923f4'
ORIGINAL_DLL = b'libprotobuf.dll'
PRIVATE_DLL = b'bbproto_old.dll'


def imports(data):
    if data[:2] != b'MZ':
        raise ValueError('Not a PE image')
    pe = struct.unpack_from('<I', data, 60)[0]
    if data[pe:pe + 4] != b'PE\0\0':
        raise ValueError('Invalid PE image')
    sections = struct.unpack_from('<H', data, pe + 6)[0]
    optional_size = struct.unpack_from('<H', data, pe + 20)[0]
    optional = pe + 24
    if struct.unpack_from('<H', data, optional)[0] != 0x20b:
        raise ValueError('Expected the pinned 64 bit PE image')
    import_rva = struct.unpack_from('<I', data, optional + 112 + 8)[0]
    table = []
    for i in range(sections):
        at = optional + optional_size + i * 40
        size, rva, raw_size, raw = struct.unpack_from('<IIII', data, at + 8)
        table.append((rva, max(size, raw_size), raw))
    def offset(address):
        for rva, size, raw in table:
            if rva <= address < rva + size:
                return raw + address - rva
        raise ValueError('Invalid PE import address')
    at = offset(import_rva)
    result = []
    while True:
        descriptor = struct.unpack_from('<IIIII', data, at)
        if not any(descriptor):
            return result
        begin = offset(descriptor[3])
        end = data.index(b'\0', begin)
        result.append((begin, data[begin:end]))
        at += 20


def patched_shader(original):
    if hashlib.sha256(original).hexdigest() != SHADER_SHA256:
        raise ValueError('Shader lab does not match the pinned official release')
    locations = [at for at, name in imports(original) if name.lower() == ORIGINAL_DLL]
    if len(locations) != 1 or len(PRIVATE_DLL) != len(ORIGINAL_DLL):
        raise ValueError('Unexpected Protobuf import layout')
    at = locations[0]
    result = bytearray(original)
    result[at:at + len(PRIVATE_DLL)] = PRIVATE_DLL
    assert len(result) == len(original)
    assert result[:at] == original[:at] and result[at + len(PRIVATE_DLL):] == original[at + len(PRIVATE_DLL):]
    return bytes(result)


def apply(official, merged):
    original = (official / 'bb-shader-lab.exe').read_bytes()
    protobuf = (official / ORIGINAL_DLL.decode()).read_bytes()
    if hashlib.sha256(protobuf).hexdigest() != PROTO_SHA256:
        raise ValueError('Protobuf does not match the pinned official release')
    patched = patched_shader(original)
    target = merged / 'bb-shader-lab.exe'
    if target.read_bytes() not in (original, patched):
        raise ValueError('Refusing to overwrite an unknown shader lab')
    private = merged / PRIVATE_DLL.decode()
    if private.exists() and private.read_bytes() != protobuf:
        raise ValueError('Refusing to overwrite an unknown private runtime')
    # Check all inputs before changing either output. The shared engine DLL is never replaced.
    private.write_bytes(protobuf)
    target.write_bytes(patched)
    return {'shaderSha256': hashlib.sha256(patched).hexdigest(), 'privateDllSha256': PROTO_SHA256}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('official_out', type=Path)
    parser.add_argument('merged_out', type=Path)
    args = parser.parse_args()
    print(apply(args.official_out, args.merged_out))
