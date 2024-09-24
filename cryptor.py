#!/usr/bin/python3

# A simple XOR Cryptor using a shared key between the BasedMarseyLoader and the based.*.dll plugins.
# Purpose is to deter Russian skids from easily dotnet-reflecting the .dlls
# Yes you could dump memory. Yes you could easily RE the decryption from a release.

# TODO. shared key-box xor. read shared key generated via BasedMarseyLoader complication.

from random import randbytes
from binascii import unhexlify, hexlify
import sys

MAGIC = b'\xde\xaa\xff\xea'
#key = randbytes(256)
key = unhexlify('985621f687318cf8e4b8008fefb8d91fc14260fdf037607d642f5414d2fcbe15dfbf3bf2e311e270a4377341fdd8027f335af08c48552dd48dd4154585d823940761fd64b3e99f8c3ecd5b0b020502d5154b823161bb41aa8c7d9f5451544619180ad4f8e684b1fff01ded0862b9e2ca802ea9b65ab8bd960bf25ae1d2e3a3b1e08f0d8b2365a8b73bc12fd06bb6969e8ea3d309f3da202a5387682ff2ec59859705bc47c976739f23a202396938c088d57e22fc317c16cdbd6c4f3d3183aa3a3f1e0a8ec97adbccfb9a7d78e283ccd47f92799d9037b67f6d9f608a3245ed421b16d142a6146a21b7516a4e65cd457ea9796f31c8519d6f420d63bf3f876aac')

def printDotnetKey(key):
    out = "new byte[] {\n"
    line = "    "
    linec = 0
    for c in key:
        line += f"{hex(c)}, "
        linec += 1
        if linec == 16:
            out += line + "\n"
            line = "    "
            linec = 0
    out += "}\n"
    print(out)

def xor(data: bytearray, key: bytes) -> bytes:
    i = 0
    keyidx = 0
    while i<len(data):
        if keyidx == len(key):
            keyidx = 0
        data[i] ^= key[keyidx]
        keyidx += 1
        i += 1
    return bytes(data)

if __name__ == '__main__':
    print(f'XOR Crypting {sys.argv[0]} to {sys.argv[1]}')
    printDotnetKey(key)
    print(hexlify(key))
    with open(sys.argv[1], 'rb') as f:
        data = xor(bytearray(f.read()), key)
    with open(sys.argv[2], 'wb') as f:
        f.write(MAGIC + data)
