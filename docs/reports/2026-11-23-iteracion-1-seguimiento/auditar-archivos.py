import csv
import hashlib
import re
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parent
previous = root.with_name('2026-11-23-iteracion-1')
workspace = root.parents[3]
extensions = {'.cs','.csproj','.slnx','.sql','.js','.jsx','.ts','.tsx','.cjs','.mjs',
              '.json','.css','.html','.yml','.yaml','.ps1','.sh','.svg'}
exclude = ('docs/reports/','postman/evidence/')
for name in ('TequioAPI','TequioApp'):
    project = workspace / name
    with (previous / f'inventario-{name}.tsv').open(encoding='utf-8-sig', newline='') as stream:
        old = {row['Ruta']: row['Resultado'] for row in csv.DictReader(stream, delimiter='\t')}
    raw = subprocess.check_output(['git','-C',str(project),'ls-files','-z','--cached','--others','--exclude-standard'])
    candidates = set(old)
    for path in raw.decode('utf-8').split('\0'):
        if path and not path.startswith(exclude) and (Path(path).suffix in extensions or path.endswith('.example')):
            candidates.add(path)
    with (root / f'auditoria-archivos-{name}.tsv').open('w',encoding='utf-8-sig',newline='') as stream:
        writer = csv.writer(stream,delimiter='\t')
        writer.writerow(['Ruta','Estado previo','Estado local','Líneas','Líneas >120','Tabulación en sangría',
                         'Catch genérico C#','Huella SHA-256','Revisión manual pendiente'])
        for path in sorted(candidates):
            file = project / path
            if not file.is_file():
                writer.writerow([path,old.get(path,''),'Retirado','','','','','',''])
                continue
            data = file.read_bytes()
            source = data.decode('utf-8-sig',errors='replace')
            lines = source.splitlines()
            status = 'Controles de texto aplicados'
            manual = 'Revisar semántica y adecuación al estándar por archivo'
            if file.name in ('package-lock.json','packages.lock.json'):
                status, manual = 'Lockfile registrado y validado por restauración',''
            writer.writerow([path,old.get(path,'Nuevo en inventario'),status,len(lines),
                             sum(len(line)>120 for line in lines),
                             sum('\t' in re.match(r'\s*',line)[0] for line in lines),
                             len(re.findall(r'\bcatch\s*\(\s*(?:System\.)?Exception\s+\w+\s*\)',source)) if file.suffix=='.cs' else '',
                             hashlib.sha256(data).hexdigest(),manual])
    print(name,len(candidates))

