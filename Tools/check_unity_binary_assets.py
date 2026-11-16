"""Check binary Unity assets without opening Unity or changing any files."""

import pathlib
import struct
import subprocess
import sys


def git(*args, data=None):
    return subprocess.check_output(["git", *args], input=data)


def check_asset(path, data):
    # Text assets and LFS pointers are not Unity serialized binary files.
    if len(data) < 20 or data.startswith((b"%YAML", b"version https://git-lfs")):
        return None, []
    version = struct.unpack_from(">I", data, 8)[0]
    if not 9 <= version <= 30:
        return None, []

    errors = []
    # Unity 2020+ uses an extended header with a 64-bit file size.
    if version >= 22:
        if len(data) < 48:
            return version, ["Truncated Unity file header."]
        expected = struct.unpack_from(">Q", data, 24)[0]
    else:
        expected = struct.unpack_from(">I", data, 4)[0]
    if len(data) != expected:
        errors.append(f"Header expects {expected} bytes; file has {len(data)}.")

    # Ask Git itself what it would store. No object is written without -w.
    raw = git("hash-object", "--stdin", data=data)
    converted = git("hash-object", "--path=" + path, "--stdin", data=data)
    if raw != converted:
        errors.append("Git would change the binary bytes when staging this file.")

    attributes = git("check-attr", "-z", "text", "merge", "--", path).split(b"\0")
    settings = {attributes[i + 1]: attributes[i + 2] for i in range(0, len(attributes) - 1, 3)}
    if settings[b"text"] not in (b"unset", b"auto"):
        errors.append("Binary asset needs -text (or text=auto) in .gitattributes.")
    if settings[b"merge"] != b"unset":
        errors.append("Mark this binary asset as binary in .gitattributes; it cannot use a text merge driver.")
    return version, errors


def main():
    paths = git("ls-files", "-z", "--", "Assets").decode("utf-8").split("\0")
    checked = 0
    failures = 0
    for path in paths:
        if not path.endswith((".asset", ".lighting")) or not pathlib.Path(path).is_file():
            continue
        version, errors = check_asset(path, pathlib.Path(path).read_bytes())
        if version is None:
            continue
        checked += 1
        for error in errors:
            print(f"ERROR: {path}: {error}")
            failures += 1
    print(f"Checked {checked} binary Unity assets; {failures} errors.")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
