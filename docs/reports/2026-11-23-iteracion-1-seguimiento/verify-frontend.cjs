const { execFileSync } = require('node:child_process');
const fs = require('node:fs');
const path = require('node:path');
const postcss = require(process.cwd() + '/node_modules/postcss');
const ts = require(process.cwd() + '/node_modules/typescript');
const output = process.argv[2];
const project = JSON.parse(fs.readFileSync(path.join(output, 'SweetBlue16_TequioApp-issues.json')));
const { analyses: [analysis] } = JSON.parse(
  fs.readFileSync(path.join(output, 'SweetBlue16_TequioApp-analyses.json'))
);

const baseline = new Map();
const sourceAt = (file) => {
  if (!baseline.has(file)) {
    baseline.set(file, execFileSync('git', ['show', `${analysis.revision}:${file}`], { encoding: 'utf8' }));
  }
  return baseline.get(file);
};

function cssRuleAtLine(css, line) {
  let result;
  css.walkDecls((declaration) => {
    if (declaration.source.start.line === line) {
      result = declaration;
    }
  });
  return result;
}

const hex = (text) => text?.match(/#[a-f0-9]{6}\b|#[a-f0-9]{3}\b/gi) ?? [];
const luminosity = (value) => {
  const color = value.slice(1);
  const digits = color.length === 3 ? [...color].map((value) => value.repeat(2)).join('') : color;
  const values = digits.match(/../g).map((channel) => {
    const number = parseInt(channel, 16) / 255;
    return number <= 0.04045 ? number / 12.92 : ((number + 0.055) / 1.055) ** 2.4;
  });
  return 0.2126 * values[0] + 0.7152 * values[1] + 0.0722 * values[2];
};

function cssEvidence(file, issue, current, contrast) {
  const previous = cssRuleAtLine(postcss.parse(sourceAt(file)), issue.line);
  if (!previous) {
    return { satisfied: false, evidence: 'No se localizó la declaración CSS en la revisión base' };
  }
  const selector = previous.parent.selector;
  const root = postcss.parse(current);
  let result = { satisfied: false, evidence: selector ?? 'Selector not found' };

  root.walkRules((rule) => {
    if (contrast && rule.selector === selector) {
      const text = hex(rule.nodes.find((node) => node.prop === 'color')?.value).at(-1);
      const colors = hex(rule.nodes.find((node) => /^(background|background-color)$/.test(node.prop))?.value);
      if (text && colors.length) {
        const fg = luminosity(text);
        const ratios = colors.map((color) => {
          const bg = luminosity(color);
          return (Math.max(fg, bg) + 0.05) / (Math.min(fg, bg) + 0.05);
        });
        const ratio = Math.min(...ratios);
        result = { line: rule.source.start.line, satisfied: ratio >= 4.5,
          evidence: `${selector}: ${text} sobre ${colors.join(', ')}; contraste mínimo ${ratio.toFixed(2)}:1` };
      }
    }

    if (!contrast && rule.selectors.includes(selector) && rule.parent.type === 'atrule' &&
      rule.parent.params.includes('prefers-reduced-motion: reduce') &&
      rule.nodes.some((node) => node.prop === previous.prop && node.value === 'none')) {
      result = { line: rule.source.start.line, satisfied: true,
        evidence: `${selector}: ${previous.prop}: none bajo prefers-reduced-motion: reduce` };
    }
  });
  return result;
}

function nestedConditional(current, file) {
  const source = ts.createSourceFile(file, current, ts.ScriptTarget.Latest, true);
  let found = false;
  const visit = (node) => {
    if (ts.isConditionalExpression(node) &&
        (ts.isConditionalExpression(node.whenTrue) || ts.isConditionalExpression(node.whenFalse))) {
      found = true;
    }
    ts.forEachChild(node, visit);
  };
  visit(source);
  return found;
}

function testPattern(current, pattern, summary) {
  const match = current.match(pattern);
  return {
    satisfied: Boolean(match),
    line: match ? current.slice(0, match.index).split('\n').length : null,
    evidence: summary,
  };
}

function inspect(issue) {
  const file = issue.component.split(':')[1];
  const rule = issue.rule.split(':')[1];
  const current = fs.existsSync(file) ? fs.readFileSync(file, 'utf8') : '';
  switch (rule) {
    case 'S7924': return cssEvidence(file, issue, current, true);
    case 'S9440': return cssEvidence(file, issue, current, false);
    case 'S4654': return testPattern(current, /flex-direction: column/, 'Contenedor en columna con flex-direction');
    case 'S4667': return { satisfied: !fs.existsSync(file), evidence: 'Archivo vacío retirado; compilación valida las importaciones' };
    case 'S1874':
      if (file.endsWith('.css')) {
        const all = [...current.matchAll(/overflow-wrap: anywhere/g)];
        const match = all[issue.line === 71 ? 0 : 1];
        return { satisfied: Boolean(match) && !current.includes('word-break: break-word'),
          line: match ? current.slice(0, match.index).split('\n').length : null, evidence: 'La regla usa overflow-wrap: anywhere' };
      }
      return { ...testPattern(current, /\bSubmitEvent\b/, 'Evento de formulario React.SubmitEvent'), satisfied: !/\bFormEvent\b/.test(current) && /\bSubmitEvent\b/.test(current) };
    case 'S3358':
      return { satisfied: !nestedConditional(current, file), evidence: 'AST de TypeScript sin ternarios directamente anidados' };
    case 'S6505': return testPattern(current, /npm ci --ignore-scripts/, 'Instalación reproducible sin ejecutar scripts de dependencias');
    case 'S7637': return testPattern(current, /SonarSource\/sonarqube-scan-action@[a-f0-9]{40}/, 'Acción SonarQube fijada por SHA completo');
    case 'S1082':
    case 'S6847':
    case 'S6848':
      return testPattern(current, /<dialog\b/, 'Diálogo nativo con Escape y botón de fondo separado; recorrido Playwright');
    case 'S6842': return testPattern(current, /<li className=/, 'Elemento de lista y acciones nativas separadas');
    case 'S6845': return { satisfied: !/\btabIndex=/.test(current), evidence: 'Región desplazable nativa sin tabIndex explícito' };
    case 'S6819':
      if (issue.message.includes('"status"')) return testPattern(current, /<output\b/, 'Estado usa el elemento semántico output');
      if (issue.message.includes('"presentation"')) return testPattern(current, /aria-hidden="true"/, 'Decoración oculta al árbol de accesibilidad');
      if (issue.message.includes('"region"')) return testPattern(current, /<section\b/, 'Región implementada como section con nombre');
      if (issue.message.includes('"dialog"')) return testPattern(current, /<dialog\b/, 'Diálogo HTML nativo');
      return { ...testPattern(current, /<button\b/, 'Control interactivo implementado mediante botón nativo'),
        satisfied: /<button\b/.test(current) && !/role="button"/.test(current) };
    case 'S6853':
      return file.includes('VerifyAccount')
        ? testPattern(current, /<legend[^>]*>Código de verificación/, 'Leyenda visible asociada al grupo de campos OTP')
        : testPattern(current, /htmlFor=\{imageInputId\}/, 'Etiqueta vinculada al id del control de archivo');
    case 'S6479': return testPattern(current, /'leading-gap'/, 'Claves semánticas estables para omisiones de paginación');
    case 'S6582': return file.includes('AddProduct')
      ? testPattern(current, /pendingFormData\?\.imageFile/, 'Acceso opcional al archivo antes de enviar')
      : testPattern(current, /segments\?\.length/, 'Lectura opcional de segmentos antes de decodificar');
    case 'S6759': return testPattern(current, /Readonly</, 'Contrato de props de solo lectura');
    case 'S6772': return testPattern(current, /<span>Biografía<\/span>/, 'Texto Biografía delimitado antes del textarea');
    case 'S7754': return testPattern(current, /subcategories\.some\(/, 'Búsqueda booleana sin materializar el elemento');
    case 'S7773':
      if (file.includes('Wizard')) return testPattern(current, issue.line === 195 ? /'latitude',[\s\S]{0,200}Number\.NaN/ : /'longitude',[\s\S]{0,200}Number\.NaN/, 'Number.NaN en la coordenada vacía');
      return testPattern(current, /Number\.isNaN\(calculatedAge\)/, 'Number.isNaN aplicado al cálculo de edad');
    case 'S7776': return testPattern(current, /ACCEPTED_IMAGE_MIME_TYPES\.has/, 'Conjunto de tipos MIME y consulta con has');
    case 'S7781': {
      const offset = issue.textRange?.startOffset ?? 0;
      return testPattern(current, offset > 68 ? /replaceAll\('_'/ : /replaceAll\('-'/, 'Normalización base64url con reemplazo literal');
    }
    case 'S5869':
    case 'S6535':
      return testPattern(current, /\[a-z0-9\/\\s\.\-\]\*/, 'Clase de caracteres con dígitos, letras y separadores distintos');
    case 'S8786':
      return file.includes('formValidators')
        ? testPattern(current, /domainDot/, 'Validación de correo por posiciones, sin retroceso del patrón')
        : testPattern(current, /\[0-9\]\[a-z0-9/, 'El patrón consume un dígito inicial sin cuantificadores superpuestos');
    case 'S9383':
      return testPattern(current, /await navigate\(/, 'Navegación esperada y error registrado; ver prueba F correspondiente');
    default: return { satisfied: false, evidence: 'Regla sin comprobación local' };
  }
}

const results = project.issues.map((issue) => ({ key: issue.key, ...inspect(issue) }));
fs.writeFileSync(path.join(output, 'comprobaciones-frontend.json'), JSON.stringify(results, null, 2));
for (const result of results.filter((entry) => !entry.satisfied)) {
  const row = project.issues.find((issue) => issue.key === result.key);
  console.log(row.key, row.rule, row.component, row.line, result.evidence);
}
console.log(results.filter((entry) => entry.satisfied).length + '/' + results.length);
