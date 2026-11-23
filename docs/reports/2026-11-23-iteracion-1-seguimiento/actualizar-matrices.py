"""Rebuild occurrence-level follow-up from preserved Sonar snapshots and the working tree."""
import csv
import json
import re
import subprocess
from collections import Counter
from pathlib import Path

out = Path(__file__).resolve().parent
previous = out.with_name('2026-11-23-iteracion-1')
workspace = out.parents[3]

def read(name):
    return json.loads((out / name).read_text(encoding='utf-8-sig'))

def tsv(name, rows):
    with (out / name).open('w', newline='', encoding='utf-8-sig') as stream:
        writer = csv.DictWriter(stream, rows[0].keys(), delimiter='\t')
        writer.writeheader()
        writer.writerows(rows)

def git(repo, *args):
    return subprocess.check_output(['git','-C',str(repo),*args], encoding='utf-8', errors='replace')

def pattern(source, regex, description, occurrence=0):
    matches = list(re.finditer(regex, source, re.M))
    if len(matches) <= occurrence:
        return None, 'No se localizó la comprobación específica: ' + description
    return source.count('\n', 0, matches[occurrence].start()) + 1, description

postman = {
    'D': (r'digest\(collection.item.slice\(0, 5\)\)', 'Comparación SHA-256 del conjunto original; sin resolver binario git por PATH'),
    'F': (r'execFileSync\(process.execPath', 'Node identificado por ruta absoluta con --check; el contenido solo se analiza'),
    'G': (r'for \(const item of node.item \?\? \[\]\) \{', 'Recorrido de elementos delimitado con llaves; visita recursiva explícita'),
    'I': (r'prepareCollection\(context\(\)\)', 'Invocación directa del preparador raíz importado'),
    'J': (r'prepareAuth\(context\(\)\)', 'Invocación directa del preparador de autenticación importado'),
    'K': (r'return fixture.data;', 'Retorno de datos fuera del bloque que rechaza JSON inválido'),
    'L': (r'assertAuth\(context\(\)\)', 'Invocación directa de aserciones de autenticación importadas'),
    'H': (r'const clock = this.clock;', 'Captura el valor del reloj local, sin alias del objeto'),
    'p': (r'digest\(originalGroups\)', 'Comparación SHA-256 de casos previos; no busca git mediante PATH'),
    'q': (r'digest\(e.values.slice\(0, 64\)\)', 'Comparación SHA-256 de variables previas; no busca git mediante PATH'),
    's': (r'execFileSync\(process.execPath', 'Sintaxis comprobada por Node --check sin evaluar el texto'),
    't': (r'for \(const item of node.item \?\? \[\]\) \{', 'Recorrido de eventos hijos con bloque explícito'),
    'u': (r'return this.productFixture\(m, local, data\);', 'Fixture separa metadatos de producto y carga en dos métodos probados'),
    'w': (r'\.isActive = false;', 'Cambio de estado separado del retorno anticipado por conflicto'),
    'y': (r"if \(name === 'crypto-js'\) \{", 'La rama SHA-256 tiene un bloque explícito'),
    'z': (r"throw new Error\('Forbidden offline module'\);", 'Rechazo explícito fuera de las ramas de módulos permitidos'),
    '0': (r'prepareCollection\(context\(\)\)', 'Llamada directa al preparador raíz importado'),
    '1': (r'prepareProfilesCatalog\(context\(\)\)', 'Llamada directa al preparador de perfiles importado'),
    '2': (r'prepareAuth\(context\(\)\)', 'Llamada directa al preparador de autenticación importado'),
    '3': (r'return r.data;', 'Retorno de respuesta fuera del bloque que comprueba JSON inválido'),
    '4': (r'assertProfilesCatalog\(context\(\)\)', 'Llamada directa y estática a las aserciones de perfiles y catálogo'),
    '5': (r"r.profiles.producer.biography = 'Unexpected change';", 'Verificación de escritura ajena separada del bucle de preparación'),
    '6': (r"r.run\('CP-MUL-03.4', \{ status: 413", 'Caso 413 se ejecuta una vez tras el bucle delimitado'),
    '7': (r'r.case\(.*CP-CAT-01.*\);', 'Caso de alta se ejecuta después del bucle que prepara usuarios y productos'),
    '8': (r"r.env.pcMaxPages = '1';", 'Tope de páginas se fija una vez después de preparar los registros'),
    '_': (r'assert.equal\(r.env.pcEvidence.includes\(r.env.authBuyerJwt\), false\);', 'Verificación de redacción fuera del bucle de esquema de evidencia'),
    'v': (r'return this.uploadFixture\(m.fixture\);', 'Casos de carga en un método con ramas en lugar de ternarios anidados'),
    'x': (r'm\?\.fixture', 'Encadenamiento opcional para tipo de fixture'),
    'A': (r"parts.push\(chunk\('npAD'", 'Relleno PNG dentro del bloque de tamaño objetivo, tras comprobar el espacio'),
    'B': (r'types.push\(type\);', 'Inventario de chunks separado del if que descomprime IDAT'),
    'C': (r'pixels\?\.toString', 'Encadenamiento opcional en la comprobación de contenido PNG'),
    'o': (r'execFileSync\(process.execPath', 'Sintaxis comprobada mediante --check sin cargar ni ejecutar la colección'),
}

def inspect_api(issue, source, base):
    rule = issue['rule'].split(':')[-1]
    original_line = issue['line']
    key = issue['key'][-1]
    if rule == 'CA1873':
        lines = base.splitlines()
        block = ' '.join(lines[original_line - 1:original_line + 4])
        quoted = re.search(r'"([^"]+)"', block)
        if quoted:
            literal = re.escape(quoted[1])
            match = pattern(source, r'if \(\w+.IsEnabled\(LogLevel.Information\)\)\s*\{\s*\w+.LogInformation\(\s*"' + literal,
                            'IsEnabled protege la evaluación de los argumentos de este log')
            if match[0]:
                return (*match, 'xunit-final.log')
        old_log_lines = [i + 1 for i, value in enumerate(lines) if '.LogInformation(' in value]
        occurrence = old_log_lines.index(original_line)
        calls = list(re.finditer(r'\w+\.LogInformation\(', source))
        if len(calls) == len(old_log_lines):
            target = calls[occurrence]
            guard = re.search(r'if \(\w+.IsEnabled\(LogLevel.Information\)\)\s*\{\s*$', source[:target.start()])
            if guard:
                return source.count('\n', 0, guard.start()) + 1, 'Argumentos de la llamada protegidos por IsEnabled; ordinal de la llamada conservado', 'xunit-final.log'
        return None, 'Verificar transformación específica del llamado de logging', 'xunit-final.log'
    if issue['rule'].startswith('javascript:'):
        name = issue['component'].rsplit('/',1)[-1]
        log = 'postman-auth.log' if name == 'validate-auth.cjs' else (
            'postman-delivery-points.log' if name == 'validate-delivery-points.cjs' else 'postman-profiles-catalog.log')
        if rule == 'S1848':
            regex, description = r'execFileSync\(process.execPath', 'Chequeo sintáctico explícito sustituye la construcción descartada'
        elif rule == 'S2871':
            regex, description = r'localeCompare\(second\)', 'Ordenación textual con comparador explícito'
            return (*pattern(source, regex, description, int(key in ('2','x') or issue['textRange']['startOffset'] > 200)), log)
        else:
            regex, description = postman[key]
        line, description = pattern(source, regex, description)
        if rule == 'S1523' and re.search(r'\b(?:vm\.(?:Script|runInNewContext)|eval)\b', source):
            return None, 'Persiste evaluación dinámica en el validador', log
        return line, description, log
    if rule == 'LiteralsNonPrintableCharactersCheck':
        tokens = {33:r'DECLARE @updateMunicipality', 44:r'ADD CONSTRAINT FK_DeliveryPoint_Municipality',
                  47:r'ADD NormalizedName AS', 51:r'CREATE UNIQUE INDEX UX_DeliveryPoint_MunicipalityName'}
        return (*pattern(source, tokens[original_line], 'Sentencia equivalente sin cadena SQL que contenga salto de línea'), 'sql-setup.log; xunit-final.log')
    if rule == 'S1192':
        anchors = {
            '15_DeliveryPointSchema.sql': (r'DECLARE @deliveryPointObjectId', 'OBJECT_ID almacenado una vez y referenciado por las comprobaciones del esquema'),
            '16_DeliveryPointProcedures.sql': (r'DECLARE @invalidFieldMessage', 'Mensaje repetido referenciado por variable local de T-SQL'),
            'UserRepository.cs': (r'const string UserIdParameter', 'Nombre de parámetro compartido por operaciones de usuario'),
        }
        return (*pattern(source, *anchors[issue['component'].rsplit('/',1)[-1]]), 'sql-setup.log; xunit-final.log')
    if rule == 'S6781':
        repo = workspace / 'TequioAPI'
        program = (repo / 'backend/Tequio.Api/Program.cs').read_text(encoding='utf-8-sig')
        line, description = pattern(source, r'\b_signingKey.CreateCredentials\(\)', 'Clave inyectada y obtenida por Program de entorno o user-secrets de Development')
        if not all(token in program for token in ('TEQUIO_JWT_SIGNING_KEY', 'AddUserSecrets', 'IsDevelopment()')):
            line = None
        return line, description, 'xunit-final.log; jwt-source-paths.txt; newman-development.json'
    if rule in ('S1118','ASP0027'):
        return (145 if not re.search(r'class\s+Program',source) and 'await app.RunAsync();' in source else None,
                'Se retiró la declaración parcial manual; SDK expone Program por InternalsVisibleTo', 'xunit-final.log')
    if rule == 'S3251':
        return (238 if 'OnModelCreatingPartial' not in source else None,
                'Eliminado ' + ('el llamado sin implementación' if original_line == 239 else 'el hook parcial sin implementación'), 'xunit-final.log; integration-http.json')
    if rule == 'CA1861':
        names = {33:'ApiValidationMessages.InvalidValue',77:'DuplicateKeyErrors',86:'DuplicateKeyErrors',
                 116:'CurrentStates',120:'TerminalStates'}
        name = names[original_line]
        selected = 2 if original_line == 86 else int(original_line in (77,116,120))
        return (*pattern(source,re.escape(name),'Arreglo reutilizado desde campo estático ' + name, selected), 'xunit-final.log')
    anchors = {
        'S8949': (r'context.RequestAborted\);','Cancelación del request propagada al escribir error JSON'),
        'S3220': (r"\.Split\(\['-', '_'\], 2\)",'Separadores agrupados en el overload previsto de Split'),
        'CA1859': (r'var generator = new VerificationCodeGenerator\(\);','Prueba usa el generador concreto para el contrato de seis dígitos'),
    }
    return (*pattern(source,*anchors[rule]), 'xunit-final.log')

checks = {entry['key']: entry for entry in read('comprobaciones-frontend.json')}
with (previous / 'f01-f22.tsv').open(encoding='utf-8-sig', newline='') as stream:
    focused = {row['Clave SonarCloud']: row for row in csv.DictReader(stream, delimiter='\t')}
replayed = {}
for project in ('TequioAPI','TequioApp'):
    repo = workspace / project
    origin = read(f'SweetBlue16_{project}-issues.json')
    remote = {issue['key']: issue for issue in origin['issues']}
    with (previous / f'sonar-{project}-trazabilidad.tsv').open(encoding='utf-8-sig', newline='') as stream:
        legacy = {row['Clave']: row for row in csv.DictReader(stream, delimiter='\t')}
    rows = []
    with (out / f'inventario-conciliado-{project}.tsv').open(encoding='utf-8-sig', newline='') as stream:
        inventory = list(csv.DictReader(stream, delimiter='\t'))
    assert len(inventory) == len(remote)
    for row in inventory:
        issue = remote[row['Clave']]
        prior = legacy.get(row['Clave'])
        file = row['Ruta completa']
        if prior:
            assert prior['Ruta en commit base'] == file and prior['Regla'] == row['Regla']
            assert prior['Línea'] == str(issue.get('line', ''))
        line = None
        if project == 'TequioAPI':
            source = (repo / file).read_text(encoding='utf-8-sig')
            base = git(repo,'show',row['Revisión remota'] + ':' + file)
            line, description, verification = inspect_api(issue, source, base)
            checked = bool(line)
            changed = (
                issue['rule'] in ('javascript:S1523','javascript:S6582')
                and (file.endswith('create-fixtures.cjs') or 'runInNewContext' in base and issue['line'] in (79,80,121,122,127))
            )
        else:
            check = checks[issue['key']]
            checked, line, description = check['satisfied'], check.get('line'), check['evidence']
            verification = 'comprobaciones-frontend.json; lint.log; frontend-build.log; jest.log'
            changed = row['ID previo'] in ('F12','F19')
            if row['ID previo'] in ('F12','F13','F14','F19'):
                verification += '; navigation-jest.log — aviso visible ante rechazo del router'
            if issue['key'] in focused:
                verification += '; evidencia inicial: ' + focused[issue['key']]['Evidencia']
                replayed[row['ID previo']] = (description, verification)
        disposition = 'Corregida y verificada' if changed else 'Ya corregida y verificada'
        if not checked:
            disposition = 'Pendiente'
        row.update({
            'Severidad en revisión base':issue['severity'],
            'Estado de trazabilidad anterior':prior['Estado local'] if prior else 'Fuera de la matriz anterior',
            'Rama remota':'main',
            'Estado en revisión remota':issue['status'],
            'Estado local':disposition,
            'Línea actual':line or '',
            'Cambio comprobado':description,
            'Evidencia local':verification,
            'Impedimento local':'' if checked else 'El verificador no pudo demostrar la transformación de esta ocurrencia',
            'Falta comprobar':'' if checked else 'Localizar la corrección exacta y ejecutar la comprobación de la regla',
            'Confirmación remota árbol local':'Pendiente de análisis de un commit que contenga este árbol local',
        })
        rows.append(row)
    tsv(f'sonar-{project}-trazabilidad.tsv',rows)
    print(project, dict(Counter(row['Estado local'] for row in rows)))
    for row in rows:
        if row['Estado local']=='Pendiente':
            print(row['Clave'],row['Regla'],row['Línea original'],row['Cambio comprobado'])

additional = []
for issue in read('SweetBlue16_TequioApp-resolved.json')['issues']:
    assert issue['key'] not in checks
    additional.append({
        'Repositorio':'TequioApp','Ruta':issue['component'].split(':',1)[1],
        'Línea original':issue.get('line',''),'Mensaje':issue['message'],'Clave':issue['key'],
        'Regla':issue['rule'],'Rama':'main','Commit':'7d132327f7656f5090229e2567098fd1848c522f',
        'Estado':issue['status'],'Resolución':issue['resolution'],
        'Evidencia':'SweetBlue16_TequioApp-resolved.json; fuera del conjunto de abiertos',
    })
tsv('sonar-antecedentes-remotos-TequioApp.tsv',additional)
assert len(replayed) == 22
for row in focused.values():
    row['Comprobación de seguimiento'], row['Evidencia de seguimiento'] = replayed[row['Id']]
tsv('f01-f22.tsv',list(focused.values()))

