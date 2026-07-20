import os
import re
from collections import defaultdict

BASE = r"d:/сайты/usasymbol/usasymbol/Content"
DIRS = [
    os.path.join(BASE, "symbols"),
    os.path.join(BASE, "rankings"),
]

TITLE_MAX = 65
DESC_MAX = 140

def collect_files(dirs):
    files = []
    for d in dirs:
        for root, _, fnames in os.walk(d):
            for f in fnames:
                if f.endswith('.yml') or f.endswith('.yaml'):
                    files.append(os.path.join(root, f))
    return files

def extract_seo_block(text):
    result = {}
    seo_match = re.search(r'^seo:\s*\n((?:[ \t]+.+\n?)*)', text, re.MULTILINE)
    if seo_match:
        block = seo_match.group(1)
        for field in ['title', 'description']:
            m = re.search(r'[ \t]+' + field + r':\s*(.+)', block)
            if m:
                result['seo.' + field] = m.group(1)
    h1_match = re.search(r'^[ \t]+h1:\s*(.+)', text, re.MULTILINE)
    if h1_match:
        result['page.h1'] = h1_match.group(1)
    title_match = re.search(r'^title:\s*(.+)', text, re.MULTILINE)
    if title_match:
        result['title'] = title_match.group(1)
    desc_match = re.search(r'^description:\s*(.+)', text, re.MULTILINE)
    if desc_match:
        result['description'] = desc_match.group(1)
    return result

def clean_value(raw):
    raw = raw.strip()
    if (raw.startswith('"') and raw.endswith('"')) or \
       (raw.startswith("'") and raw.endswith("'")):
        return raw[1:-1]
    return raw

def check_quotes(raw):
    raw = raw.strip()
    probs = []
    if raw.startswith('"') and not raw.endswith('"'):
        probs.append('UNCLOSED_DOUBLE_QUOTE')
    if raw.startswith("'") and not raw.endswith("'"):
        probs.append('UNCLOSED_SINGLE_QUOTE')
    if raw.endswith('"') and not raw.startswith('"'):
        probs.append('EXTRA_CLOSING_DOUBLE_QUOTE')
    if raw.endswith("'") and not raw.startswith("'"):
        probs.append('EXTRA_CLOSING_SINGLE_QUOTE')
    if raw.startswith('"') and raw.endswith('"'):
        inner = raw[1:-1]
        if '"' in inner:
            probs.append('UNESCAPED_DOUBLE_QUOTE_INSIDE')
    if raw.startswith("'") and raw.endswith("'"):
        inner = raw[1:-1]
        if "'" in inner and "''" not in inner:
            probs.append('UNESCAPED_SINGLE_QUOTE_INSIDE')
    return probs

files = collect_files(DIRS)
print(f"Total files: {len(files)}\n")

all_issues = []

for fpath in sorted(files):
    try:
        with open(fpath, encoding='utf-8') as f:
            text = f.read()
    except Exception as e:
        all_issues.append({'file': fpath, 'field': 'READ_ERROR', 'issue': str(e), 'value': ''})
        continue

    fields = extract_seo_block(text)
    file_issues = []

    for field, raw in fields.items():
        q_issues = check_quotes(raw)
        for qi in q_issues:
            file_issues.append({'field': field, 'issue': qi, 'value': raw})

        val = clean_value(raw)

        if field in ('seo.title', 'title'):
            if len(val) > TITLE_MAX:
                file_issues.append({'field': field, 'issue': 'TITLE_TOO_LONG (' + str(len(val)) + ' chars)', 'value': val})
        if field in ('seo.description', 'description'):
            if len(val) > DESC_MAX:
                file_issues.append({'field': field, 'issue': 'DESC_TOO_LONG (' + str(len(val)) + ' chars)', 'value': val})
        if not val or val in ('""', "''"):
            file_issues.append({'field': field, 'issue': 'EMPTY_VALUE', 'value': raw})

    if not fields.get('seo.title') and not fields.get('title'):
        file_issues.append({'field': 'seo.title', 'issue': 'MISSING_FIELD', 'value': ''})
    if not fields.get('seo.description') and not fields.get('description'):
        file_issues.append({'field': 'seo.description', 'issue': 'MISSING_FIELD', 'value': ''})

    for fi in file_issues:
        rel = fpath.replace(BASE, '').replace('\\', '/').lstrip('/')
        all_issues.append({'file': rel, 'field': fi['field'], 'issue': fi['issue'], 'value': fi['value']})

by_type = defaultdict(list)
for issue in all_issues:
    key = issue['issue'].split(' ')[0]
    by_type[key].append(issue)

print(f"=== TOTAL ISSUES: {len(all_issues)} ===\n")

for itype, items in sorted(by_type.items()):
    print(f"\n--- {itype} ({len(items)}) ---")
    for item in items:
        print(f"  {item['file']}  [{item['field']}]")
        if item['value']:
            print(f"    > {item['value'][:120]}")
