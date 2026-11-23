"""Measure covered OpenCover sequence points by unique source file and line."""
import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

source, target = map(Path, sys.argv[1:])
scopes = ('/Tequio.Api/Controllers/', '/Tequio.Api/Validators/', '/Tequio.Domain/Services/')
lines = {}
for module in ET.parse(source).getroot().findall('./Modules/Module'):
    files = {file.get('uid'): file.get('fullPath').replace('\\','/') for file in module.findall('./Files/File')}
    for point in module.iter('SequencePoint'):
        file = files.get(point.get('fileid'),'')
        if any(scope in file for scope in scopes):
            key = (file, point.get('sl'))
            lines[key] = lines.get(key, False) or int(point.get('vc','0')) > 0
assert lines, "The expected API source directories were not instrumented"
covered = sum(lines.values())
report = {'scope':scopes,'covered_lines':covered,'instrumented_lines':len(lines),
          'percentage':round(100 * covered / len(lines),2),'target':80}
target.write_text(json.dumps(report,indent=2) + '\n',encoding='utf-8')
print(json.dumps(report))

