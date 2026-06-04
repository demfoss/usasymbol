"""
Convert YAML single-quoted strings that contain '' (escaped apostrophes)
to double-quoted strings, restoring normal apostrophes like Michigan's.
"""
import re
import sys
from pathlib import Path

def fix_file(path: Path) -> bool:
    content = path.read_text(encoding="utf-8")

    def replace(m):
        inner = m.group(1)
        if "''" not in inner:
            return m.group(0)  # no escaped apostrophes — leave as-is
        unescaped = inner.replace("''", "'")
        # Escape any inner double quotes, then wrap in double quotes
        escaped = unescaped.replace('\\', '\\\\').replace('"', '\\"')
        return '"' + escaped + '"'

    # Match single-quoted YAML scalars (may span '' escaped apostrophes)
    fixed = re.sub(r"'((?:[^']|'')*)'", replace, content)

    if fixed == content:
        return False

    path.write_text(fixed, encoding="utf-8")
    return True


root = Path(__file__).parent / "Content"
patterns = ["**/*.yaml", "**/*.yml"]

changed = []
for pattern in patterns:
    for p in sorted(root.glob(pattern)):
        if fix_file(p):
            changed.append(p.relative_to(root))

if changed:
    print(f"Fixed {len(changed)} file(s):")
    for f in changed:
        print(f"  {f}")
else:
    print("No files needed changes.")
