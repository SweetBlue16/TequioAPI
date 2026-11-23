// This function supplies the corresponding standalone Postman event.
module.exports = function prepareProfilesCatalog({ pm, Date, require }) {
const pcManifest = {
  "CP-PER-01 / 01 - Consultar perfil autenticado original": {
    "id": "CP-PER-01",
    "key": "CP-PER-01.1",
    "name": "CP-PER-01 / 01 - Consultar perfil autenticado original",
    "action": "snapshot",
    "role": "buyer",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": []
  },
  "CP-PER-01 / 02 - Consultar segundo perfil de prueba como control": {
    "id": "CP-PER-01",
    "key": "CP-PER-01.2",
    "name": "CP-PER-01 / 02 - Consultar segundo perfil de prueba como control",
    "action": "snapshot",
    "role": "producer",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "CP-PER-01.1"
    ],
    "slot": "observer"
  },
  "CP-PER-01 / 03 - Actualizar campos permitidos con datos ficticios": {
    "id": "CP-PER-01",
    "key": "CP-PER-01.3",
    "name": "CP-PER-01 / 03 - Actualizar campos permitidos con datos ficticios",
    "action": "update",
    "role": "buyer",
    "method": "PUT",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "CP-PER-01.2"
    ]
  },
  "CP-PER-01 / 04 - Comparar datos persistidos e identidad": {
    "id": "CP-PER-01",
    "key": "CP-PER-01.4",
    "name": "CP-PER-01 / 04 - Comparar datos persistidos e identidad",
    "action": "profileAfter",
    "role": "buyer",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "CP-PER-01.3"
    ]
  },
  "CP-PER-01 / 05 - Comprobar que el perfil de control no cambió": {
    "id": "CP-PER-01",
    "key": "CP-PER-01.5",
    "name": "CP-PER-01 / 05 - Comprobar que el perfil de control no cambió",
    "action": "unchanged",
    "role": "producer",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "CP-PER-01.4"
    ],
    "slot": "observer"
  },
  "CP-PER-02 / 01 - Guardar perfil original": {
    "id": "CP-PER-02",
    "key": "CP-PER-02.1",
    "name": "CP-PER-02 / 01 - Guardar perfil original",
    "action": "snapshot",
    "role": "buyer",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": []
  },
  "CP-PER-02 / 02 - Rechazar nombre obligatorio vacío sin cambios parciales": {
    "id": "CP-PER-02",
    "key": "CP-PER-02.2",
    "name": "CP-PER-02 / 02 - Rechazar nombre obligatorio vacío sin cambios parciales",
    "action": "invalidRequired",
    "role": "buyer",
    "method": "PUT",
    "route": "/api/v1/users/me",
    "statuses": [
      400
    ],
    "requires": [
      "CP-PER-02.1"
    ]
  },
  "CP-PER-02 / 03 - Comparar perfil completo tras rechazo Required": {
    "id": "CP-PER-02",
    "key": "CP-PER-02.3",
    "name": "CP-PER-02 / 03 - Comparar perfil completo tras rechazo Required",
    "action": "unchanged",
    "role": "buyer",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "CP-PER-02.2"
    ]
  },
  "CP-PER-02 / 04 - Rechazar teléfono de 21 caracteres": {
    "id": "CP-PER-02",
    "key": "CP-PER-02.4",
    "name": "CP-PER-02 / 04 - Rechazar teléfono de 21 caracteres",
    "action": "invalidLength",
    "role": "buyer",
    "method": "PUT",
    "route": "/api/v1/users/me",
    "statuses": [
      400
    ],
    "requires": [
      "CP-PER-02.3"
    ]
  },
  "CP-PER-02 / 05 - Comparar perfil completo tras rechazo StringLength": {
    "id": "CP-PER-02",
    "key": "CP-PER-02.5",
    "name": "CP-PER-02 / 05 - Comparar perfil completo tras rechazo StringLength",
    "action": "unchanged",
    "role": "buyer",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "CP-PER-02.4"
    ]
  },
  "CP-MUL-01 / 01 - Guardar perfil original": {
    "id": "CP-MUL-01",
    "key": "CP-MUL-01.1",
    "name": "CP-MUL-01 / 01 - Guardar perfil original",
    "action": "snapshot",
    "role": "buyer",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": []
  },
  "CP-MUL-01 / 02 - Subir PNG ficticio dentro del límite": {
    "id": "CP-MUL-01",
    "key": "CP-MUL-01.2",
    "name": "CP-MUL-01 / 02 - Subir PNG ficticio dentro del límite",
    "action": "upload",
    "role": "buyer",
    "method": "POST",
    "route": "/api/v1/files/image",
    "statuses": [
      200
    ],
    "requires": [
      "CP-MUL-01.1"
    ],
    "fixture": "small"
  },
  "CP-MUL-01 / 03 - Asociar la URL real al perfil": {
    "id": "CP-MUL-01",
    "key": "CP-MUL-01.3",
    "name": "CP-MUL-01 / 03 - Asociar la URL real al perfil",
    "action": "associate",
    "role": "buyer",
    "method": "PUT",
    "route": "/api/v1/users/me/profile-picture",
    "statuses": [
      200
    ],
    "requires": [
      "CP-MUL-01.2"
    ]
  },
  "CP-MUL-01 / 04 - Comprobar fotografía e identidad por GET": {
    "id": "CP-MUL-01",
    "key": "CP-MUL-01.4",
    "name": "CP-MUL-01 / 04 - Comprobar fotografía e identidad por GET",
    "action": "pictureAfter",
    "role": "buyer",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "CP-MUL-01.3"
    ]
  },
  "CP-MUL-02 / 01 - Guardar perfil original": {
    "id": "CP-MUL-02",
    "key": "CP-MUL-02.1",
    "name": "CP-MUL-02 / 01 - Guardar perfil original",
    "action": "snapshot",
    "role": "buyer",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": []
  },
  "CP-MUL-02 / 02 - Exigir rechazo de texto inofensivo .txt": {
    "id": "CP-MUL-02",
    "key": "CP-MUL-02.2",
    "name": "CP-MUL-02 / 02 - Exigir rechazo de texto inofensivo .txt",
    "action": "upload",
    "role": "buyer",
    "method": "POST",
    "route": "/api/v1/files/image",
    "statuses": [
      400
    ],
    "requires": [
      "CP-MUL-02.1"
    ],
    "fixture": "txt"
  },
  "CP-MUL-02 / 03 - Comprobar que el perfil no cambió": {
    "id": "CP-MUL-02",
    "key": "CP-MUL-02.3",
    "name": "CP-MUL-02 / 03 - Comprobar que el perfil no cambió",
    "action": "unchanged",
    "role": "buyer",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "CP-MUL-02.2"
    ],
    "followRejected": true
  },
  "CP-MUL-03 / 01 - Guardar perfil original": {
    "id": "CP-MUL-03",
    "key": "CP-MUL-03.1",
    "name": "CP-MUL-03 / 01 - Guardar perfil original",
    "action": "snapshot",
    "role": "buyer",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": []
  },
  "CP-MUL-03 / 02 - Subir PNG por debajo del límite": {
    "id": "CP-MUL-03",
    "key": "CP-MUL-03.2",
    "name": "CP-MUL-03 / 02 - Subir PNG por debajo del límite",
    "action": "upload",
    "role": "buyer",
    "method": "POST",
    "route": "/api/v1/files/image",
    "statuses": [
      200
    ],
    "requires": [
      "CP-MUL-03.1"
    ],
    "fixture": "small"
  },
  "CP-MUL-03 / 03 - Subir PNG exactamente de 5 MiB": {
    "id": "CP-MUL-03",
    "key": "CP-MUL-03.3",
    "name": "CP-MUL-03 / 03 - Subir PNG exactamente de 5 MiB",
    "action": "upload",
    "role": "buyer",
    "method": "POST",
    "route": "/api/v1/files/image",
    "statuses": [
      200
    ],
    "requires": [
      "CP-MUL-03.2"
    ],
    "fixture": "exact"
  },
  "CP-MUL-03 / 04 - Rechazar PNG de 5 MiB más un byte": {
    "id": "CP-MUL-03",
    "key": "CP-MUL-03.4",
    "name": "CP-MUL-03 / 04 - Rechazar PNG de 5 MiB más un byte",
    "action": "upload",
    "role": "buyer",
    "method": "POST",
    "route": "/api/v1/files/image",
    "statuses": [
      400,
      413
    ],
    "requires": [
      "CP-MUL-03.3"
    ],
    "fixture": "over"
  },
  "CP-MUL-03 / 05 - Comprobar perfil sin asociaciones inesperadas": {
    "id": "CP-MUL-03",
    "key": "CP-MUL-03.5",
    "name": "CP-MUL-03 / 05 - Comprobar perfil sin asociaciones inesperadas",
    "action": "unchanged",
    "role": "buyer",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "CP-MUL-03.4"
    ],
    "followRejected": true
  },
  "CP-MUL-04 / 01 - Guardar perfil original": {
    "id": "CP-MUL-04",
    "key": "CP-MUL-04.1",
    "name": "CP-MUL-04 / 01 - Guardar perfil original",
    "action": "snapshot",
    "role": "buyer",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": []
  },
  "CP-MUL-04 / 02 - Observar nombre con secuencia de ruta": {
    "id": "CP-MUL-04",
    "key": "CP-MUL-04.2",
    "name": "CP-MUL-04 / 02 - Observar nombre con secuencia de ruta",
    "action": "rawUpload",
    "role": "buyer",
    "method": "POST",
    "route": "/api/v1/files/image",
    "statuses": [
      200,
      400,
      413,
      415,
      500
    ],
    "requires": [
      "CP-MUL-04.1"
    ],
    "filename": "traversal"
  },
  "CP-MUL-04 / 03 - Observar nombre con separadores y caracteres especiales": {
    "id": "CP-MUL-04",
    "key": "CP-MUL-04.3",
    "name": "CP-MUL-04 / 03 - Observar nombre con separadores y caracteres especiales",
    "action": "rawUpload",
    "role": "buyer",
    "method": "POST",
    "route": "/api/v1/files/image",
    "statuses": [
      200,
      400,
      413,
      415,
      500
    ],
    "requires": [
      "CP-MUL-04.2"
    ],
    "filename": "special"
  },
  "CP-MUL-04 / 04 - Comprobar perfil sin cambios": {
    "id": "CP-MUL-04",
    "key": "CP-MUL-04.4",
    "name": "CP-MUL-04 / 04 - Comprobar perfil sin cambios",
    "action": "unchanged",
    "role": "buyer",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "CP-MUL-04.3"
    ],
    "followRejected": true
  },
  "CP-CAT-01 / 01 - Comprobar identidad del productor de prueba": {
    "id": "CP-CAT-01",
    "key": "CP-CAT-01.1",
    "name": "CP-CAT-01 / 01 - Comprobar identidad del productor de prueba",
    "action": "snapshot",
    "role": "producer",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": []
  },
  "CP-CAT-01 / 02 - Registrar producto ficticio y capturar ID real": {
    "id": "CP-CAT-01",
    "key": "CP-CAT-01.2",
    "name": "CP-CAT-01 / 02 - Registrar producto ficticio y capturar ID real",
    "action": "create",
    "role": "producer",
    "method": "POST",
    "route": "/api/v1/products",
    "statuses": [
      201
    ],
    "requires": [
      "CP-CAT-01.1"
    ],
    "target": "created"
  },
  "CP-CAT-01 / 03 - Buscar producto creado en páginas del productor": {
    "id": "CP-CAT-01",
    "key": "CP-CAT-01.3",
    "name": "CP-CAT-01 / 03 - Buscar producto creado en páginas del productor",
    "action": "scan",
    "role": "producer",
    "method": "GET",
    "route": "/api/v1/products/producers/{{pcProducerId}}?pageIndex={{pcPageIndex}}&pageSize=100",
    "statuses": [
      200
    ],
    "requires": [
      "CP-CAT-01.2"
    ],
    "target": "created",
    "mode": "present"
  },
  "CP-CAT-03 / 01 - Comprobar productor autenticado": {
    "id": "CP-CAT-03",
    "key": "CP-CAT-03.1",
    "name": "CP-CAT-03 / 01 - Comprobar productor autenticado",
    "action": "snapshot",
    "role": "producer",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": []
  },
  "CP-CAT-03 / 02 - Comprobar producto propio creado y activo": {
    "id": "CP-CAT-03",
    "key": "CP-CAT-03.2",
    "name": "CP-CAT-03 / 02 - Comprobar producto propio creado y activo",
    "action": "scan",
    "role": "producer",
    "method": "GET",
    "route": "/api/v1/products/producers/{{pcProducerId}}?pageIndex={{pcPageIndex}}&pageSize=100",
    "statuses": [
      200
    ],
    "requires": [
      "CP-CAT-03.1",
      "CP-CAT-01.3"
    ],
    "target": "created",
    "mode": "present"
  },
  "CP-CAT-03 / 03 - Desactivar con confirmación adicional de un uso": {
    "id": "CP-CAT-03",
    "key": "CP-CAT-03.3",
    "name": "CP-CAT-03 / 03 - Desactivar con confirmación adicional de un uso",
    "action": "deactivate",
    "role": "producer",
    "method": "PATCH",
    "route": "/api/v1/products/{{pcProductId}}/deactive",
    "statuses": [
      200
    ],
    "requires": [
      "CP-CAT-03.2"
    ],
    "target": "created"
  },
  "CP-CAT-03 / 04 - Explorar todas las páginas y comprobar ausencia entre activos": {
    "id": "CP-CAT-03",
    "key": "CP-CAT-03.4",
    "name": "CP-CAT-03 / 04 - Explorar todas las páginas y comprobar ausencia entre activos",
    "action": "scan",
    "role": "producer",
    "method": "GET",
    "route": "/api/v1/products/producers/{{pcProducerId}}?pageIndex={{pcPageIndex}}&pageSize=100",
    "statuses": [
      200
    ],
    "requires": [
      "CP-CAT-03.3"
    ],
    "target": "created",
    "mode": "absent"
  },
  "CP-CAT-04 / 01 - Comprobar productor antes de preparar producto dedicado": {
    "id": "CP-CAT-04",
    "key": "CP-CAT-04.1",
    "name": "CP-CAT-04 / 01 - Comprobar productor antes de preparar producto dedicado",
    "action": "snapshot",
    "role": "producer",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [],
    "stage": "prepare-product"
  },
  "CP-CAT-04 / 02 - Crear producto exclusivo para lote autorizado": {
    "id": "CP-CAT-04",
    "key": "CP-CAT-04.2",
    "name": "CP-CAT-04 / 02 - Crear producto exclusivo para lote autorizado",
    "action": "create",
    "role": "producer",
    "method": "POST",
    "route": "/api/v1/products",
    "statuses": [
      201
    ],
    "requires": [
      "CP-CAT-04.1"
    ],
    "target": "batch",
    "stage": "prepare-product"
  },
  "CP-CAT-04 / 03 - Acreditar producto dedicado activo por API": {
    "id": "CP-CAT-04",
    "key": "CP-CAT-04.3",
    "name": "CP-CAT-04 / 03 - Acreditar producto dedicado activo por API",
    "action": "scan",
    "role": "producer",
    "method": "GET",
    "route": "/api/v1/products/producers/{{pcProducerId}}?pageIndex={{pcPageIndex}}&pageSize=100",
    "statuses": [
      200
    ],
    "requires": [
      "CP-CAT-04.2"
    ],
    "target": "batch",
    "mode": "present",
    "stage": "prepare-product"
  },
  "CP-CAT-04 / 04 - Consultar producto después de preparación manual autorizada": {
    "id": "CP-CAT-04",
    "key": "CP-CAT-04.4",
    "name": "CP-CAT-04 / 04 - Consultar producto después de preparación manual autorizada",
    "action": "scan",
    "role": "producer",
    "method": "GET",
    "route": "/api/v1/products/producers/{{pcProducerId}}?pageIndex={{pcPageIndex}}&pageSize=100",
    "statuses": [
      200
    ],
    "requires": [
      "CP-CAT-04.3"
    ],
    "target": "batch",
    "mode": "present",
    "stage": "probe"
  },
  "CP-CAT-04 / 05 - Exigir 409 al intentar baja con lote": {
    "id": "CP-CAT-04",
    "key": "CP-CAT-04.5",
    "name": "CP-CAT-04 / 05 - Exigir 409 al intentar baja con lote",
    "action": "deactivate",
    "role": "producer",
    "method": "PATCH",
    "route": "/api/v1/products/{{pcProductId}}/deactive",
    "statuses": [
      409
    ],
    "requires": [
      "CP-CAT-04.4"
    ],
    "target": "batch",
    "stage": "probe"
  },
  "CP-CAT-04 / 06 - Comprobar que el producto sigue activo": {
    "id": "CP-CAT-04",
    "key": "CP-CAT-04.6",
    "name": "CP-CAT-04 / 06 - Comprobar que el producto sigue activo",
    "action": "scan",
    "role": "producer",
    "method": "GET",
    "route": "/api/v1/products/producers/{{pcProducerId}}?pageIndex={{pcPageIndex}}&pageSize=100",
    "statuses": [
      200
    ],
    "requires": [
      "CP-CAT-04.5"
    ],
    "target": "batch",
    "mode": "present",
    "stage": "probe",
    "followRejected": true
  }
};
// Shared guard for CP-PER, CP-MUL and CP-CAT. No hidden HTTP or SQL calls.
const pe=pm.environment, pg=k=>String(pe.get(k)||'');
const ps=(k,v)=>{pe.set(k,String(v));pm.variables.set(k,String(v));};
const m=pcManifest[pm.info.requestName];
let st;try{st=JSON.parse(pg('_pcState')||'null');}catch(_){st=null;}
const run=pg('runId');
const ids=['CP-PER-01','CP-PER-02','CP-MUL-01','CP-MUL-02','CP-MUL-03','CP-MUL-04','CP-CAT-01','CP-CAT-02','CP-CAT-03','CP-CAT-04','CP-CAT-05'];
function initial(id){return ['CP-CAT-02','CP-CAT-05'].includes(id)?'NO IMPLEMENTADO':['CP-PER-02','CP-MUL-04','CP-CAT-04'].includes(id)?'BLOQUEADO':'NO EJECUTADO';}
if(!st || st.runId!==run){st={runId:run,origin:'',cases:{},steps:{},owners:{},snapshots:{},expected:{},uploads:{},products:{},scans:{}};ids.forEach(id=>st.cases[id]={status:initial(id),reason:initial(id)==='BLOQUEADO'?'Alcance o precondición pendiente; ver informe':''});}
function save(){ps('_pcState',JSON.stringify(st));ps('pcResults',JSON.stringify({runId:run,cases:st.cases}));ps('pcEvidence',JSON.stringify(Object.values(st.steps).filter(x=>x.responses).map(x=>({caseId:x.caseId,step:x.key,method:x.method,path:x.path,responses:x.responses,status:x.status,reason:x.reason||''}))));}
function skip(reason,blocked=true){if(blocked&&m&&st.cases[m.id].status!=='FALLIDO')st.cases[m.id]={status:'BLOQUEADO',reason};save();pm.test.skip((m?m.id:'CP')+' — '+(blocked?'BLOQUEADO: ':'OMITIDO: ')+reason,function(){});pm.execution.skipRequest();throw new Error('PC request skipped');}
const need=k=>{if(!pg(k).trim())skip('Configurar '+k);return pg(k);};
if(!m)skip('Solicitud desconocida');
if(pg('pcSelectedCase')!==m.id)skip('Seleccionar explícitamente un solo caso en pcSelectedCase',false);
if(!/^[a-f0-9]{32}$/.test(run))skip('Conservar el runId de las cuentas ficticias autenticadas');
const base=need('baseUrl');
if(!/^https?:\/\/(?:[A-Za-z0-9][A-Za-z0-9.-]*|\[[a-fA-F0-9:]+\])(?::[0-9]{1,5})?$/.test(base)||pg('authorizedDevelopmentUrl')!==base||pg('developmentEnvironmentConfirmed')!=='true')skip('Confirmar el origen exacto autorizado de desarrollo');
if(pg('allowMutatingTests')!=='true')skip('Se requiere allowMutatingTests=true autorizado');
if(st.origin&&st.origin!==base)skip('No cambiar de origen durante la ejecución');st.origin=base;
if(m.method!==pm.request.method)skip('Método modificado');
if(m.id==='CP-CAT-04'){
  if(!['prepare-product','probe'].includes(pg('pcBatchStage')))skip('No existe preparación de lotes autorizada; pcBatchStage está deshabilitado');
  if(pg('pcBatchPreparationApprovedRunId')!==run||!pg('pcBatchPreparationMechanism').trim())skip('El equipo debe aprobar un mecanismo de desarrollo para preparar el lote; no ejecutar SQL');
  if(m.stage!==pg('pcBatchStage'))skip('Paso de otra etapa del lote',false);
  if(m.stage==='probe'&&(pg('pcBatchLinkedConfirmedRunId')!==run||!pg('pcBatchEvidenceRef').trim()||!['Draft','Active','Confirmed'].includes(pg('pcBatchState'))))skip('Falta evidencia autorizada de asociación a lote de prueba');
}
const prior=st.steps[m.key];
if(prior&&prior.done)skip('Paso ya respondido; conservar evidencia y no repetir escrituras',false);
if(prior&&!prior.done&&m.method!=='GET')skip('Escritura previa sin respuesta concluyente; no reintentar automáticamente');
for(const dep of m.requires){const r=st.steps[dep];if(!r||!r.done||(!r.flowReady&&!(m.followRejected&&r.observed)))skip('Falta precondición API: '+dep);}
if(pg('testInboxConfirmed')!=='true'||pg('emailRoutingConfirmed')!=='true')skip('Conservar la procedencia de las cuentas y buzón autorizado');
if(pg('buyerRoleId')!=='1'||pg('producerRoleId')!=='2')skip('Conservar los roles reales Comprador 1 y Productor 2');
const role=m.role;
let auth;try{auth=JSON.parse(pg('_authState')||'null');}catch(_){auth=null;}
const template=need(role+'EmailTemplate');if(!template.includes('{{runId}}'))skip('Plantilla sin runId');
const email=template.split('{{runId}}').join(run).toLowerCase();
const expectedId=need(role+'UserId');if(!/^[1-9][0-9]*$/.test(expectedId))skip('ID real requerido');
let token='';
if(auth&&auth.runId===run&&auth.origin===base&&auth.ids[role]===expectedId&&auth.claims[role]&&String(auth.claims[role].sub)===expectedId)token=pg(role==='buyer'?'authBuyerJwt':'authProducerJwt');
if(!token&&pg('_jwtRunId')===run&&pg('_jwtAccount')===role&&pg('_jwtUserId')===expectedId&&pg('_profileRunId')===run&&pg('_profileAccount')===role&&pg('_profileUserId')===expectedId)token=pg('jwt');
let claims;try{if(token.split('.').length!==3)throw new Error();claims=JSON.parse(require('buffer').Buffer.from(token.split('.')[1].replace(/-/g,'+').replace(/_/g,'/'),'base64').toString('utf8'));}catch(_){claims=null;}
if(!claims||String(claims.sub)!==expectedId||Number(claims.roleId)!==(role==='producer'?2:1)||!Number.isFinite(claims.exp)||claims.exp*1000<=Date.now())skip('Falta JWT vigente con procedencia e identidad de esta cuenta de prueba');
if(pg('buyerUserId')&&pg('producerUserId')&&pg('buyerUserId')===pg('producerUserId'))skip('Las cuentas de control deben ser distintas');
const binding=require('crypto-js').SHA256(JSON.stringify([run,base,pg('buyerEmailTemplate'),pg('producerEmailTemplate'),pg('buyerUserId'),pg('producerUserId')])).toString();
if(st.binding&&st.binding!==binding)skip('No cambiar cuentas, enrutamiento o destino de almacenamiento al reanudar');st.binding=binding;
pm.variables.set('baseUrl',base);pm.variables.set('pcRequestJwt',token);pm.variables.set('pcIdentity',JSON.stringify({id:Number(expectedId),email,role}));
const snapshotKey=m.id+'_'+(m.slot||'actor');
const snap=st.snapshots[snapshotKey];
if(m.method!=='GET'&&(!snap||snap.id!==Number(expectedId)||snap.email!==email))skip('Consultar y acreditar el perfil ficticio antes de cualquier escritura');
let body;
const fields=['firstName','paternalLastName','maternalLastName','birthDate','phoneNumber','locality','biography'];
if(['update','invalidRequired','invalidLength'].includes(m.action)){
  body=Object.fromEntries(fields.map(k=>[k,snap[k]]));
  if(m.action==='update')Object.assign(body,{firstName:'TEST_'+run+'_PER',paternalLastName:'TEST_Apellido',maternalLastName:'TEST_Materno',phoneNumber:'5551234567',locality:'TEST_'+run,biography:'TEST_'+run+' perfil actualizado'});
  else if(m.action==='invalidRequired')Object.assign(body,{firstName:'',locality:'TEST_'+run+'_NO_GUARDAR'});
  else Object.assign(body,{firstName:'TEST_'+run+'_NO_GUARDAR',phoneNumber:'X'.repeat(21)});
  if(m.action==='update')st.expected[m.id]=body;
}
if(['upload','rawUpload'].includes(m.action)){
  if(pg('pcAllowExternalUploads')!=='true'||pg('pcExternalUploadConfirmedRunId')!==run)skip('Las cargas externas requieren consentimiento separado por ejecución');
  const origin=need('pcStorageOrigin'),prefix=need('pcStoragePathPrefix');
  if(!/^https:\/\/[A-Za-z0-9.-]+$/.test(origin)||!prefix.startsWith('/')||!prefix.endsWith('/')||prefix.includes('..')||prefix.includes('\\')||prefix.includes('?'))skip('Configurar origen HTTPS y prefijo públicos del almacenamiento de pruebas autorizado');
  const storageBinding=require('crypto-js').SHA256(JSON.stringify([origin,prefix])).toString();if(st.storageBinding&&st.storageBinding!==storageBinding)skip('No cambiar almacenamiento entre cargas de una ejecución');st.storageBinding=storageBinding;
}
if(m.action==='upload'){
  if(pg('pcFixturesConfirmedRunId')!==run)skip('Generar, verificar y confirmar fixtures locales para este runId');
  let manifest;try{manifest=JSON.parse(pg('pcFixtureManifest'));}catch(_){skip('Copiar el manifiesto generado y verificado');}
  const f=manifest.files&&manifest.files[m.fixture];
  const sizes={exact:5242880,over:5242881};
  if(manifest.runId!==run||!f||!/^[a-f0-9]{64}$/.test(f.sha256)||!f.name.startsWith('TEST_'+run+'_'))skip('Fixture sin procedencia o hash esperado');
  if(sizes[m.fixture]&&f.size!==sizes[m.fixture])skip('El tamaño del fixture no corresponde al límite');
  if(m.fixture==='small'&&(!Number.isInteger(f.size)||f.size<=0||f.size>=5242880))skip('El fixture positivo debe ser no vacío y estar dentro del límite');
  if(m.fixture==='txt'&&!f.name.endsWith('.txt'))skip('Seleccionar texto inofensivo .txt');
  const field=pm.request.body.formdata.all().find(p=>p.key==='file'&&p.type==='file');const src=field&&field.src;
  if(typeof src!=='string'||src.split(/[\\/]/).at(-1)!==f.name)skip('Seleccionar en Body el archivo local exacto del manifiesto');
}
if(m.action==='rawUpload'){
  const boundary='TEQUIO_'+run;
  const filename=m.filename==='traversal'?'../../TEST_'+run+'.txt':'..\\..\\TEST_'+run+'_<>&.txt';
  pm.variables.set('pcBoundary',boundary);
  pm.variables.set('pcMultipartBody','--'+boundary+'\r\nContent-Disposition: form-data; name="file"; filename="'+filename.replace(/\\/g,'\\\\')+'"\r\nContent-Type: text/plain\r\n\r\nTEST_'+run+' harmless text only.\r\n--'+boundary+'--\r\n');
}
if(m.action==='associate'){
  const u=st.uploads[m.id];if(!u||u.runId!==run||u.ownerId!==Number(expectedId))skip('Falta URL real subida por esta cuenta durante el caso');body={pictureUrl:u.url};
}
if(m.action==='create'){
  const category=need('categoryId');if(!/^[1-9][0-9]*$/.test(category))skip('Categoría existente válida requerida');
  if(st.products[m.target])skip('El producto dedicado ya fue creado; no duplicarlo');
  body={name:'TEST_'+run+'_'+m.target,categoryId:Number(category),shortDescription:'TEST_'+run+' producto ficticio',measurementUnit:'kg',imageUrl:null};st.expected[m.key]=body;
}
if(['scan','deactivate'].includes(m.action)){
  const product=st.products[m.target];if(!product||product.runId!==run||product.ownerId!==Number(expectedId))skip('Solo se admite un ID real creado por esta cuenta en esta ejecución');
  pm.variables.set('pcProducerId',expectedId);pm.variables.set('pcProductId',String(product.id));
  if(m.action==='scan'){
    const max=Number(pg('pcMaxPages'));if(!Number.isInteger(max)||max<1||max>1000)skip('pcMaxPages debe estar entre 1 y 1000');
    const scan=st.scans[m.key]||(st.scans[m.key]={next:1,rows:[],total:null});if(scan.next>max)skip('Exploración incompleta: límite de páginas; no aprobar ausencia ni persistencia');pm.variables.set('pcPageIndex',String(scan.next));
  }else{
    if(pg('_productRunId')!==run||pg('_productOwnerId')!==expectedId||pg('_createdProductId')!==String(product.id)||pg('productId')!==String(product.id)||pg('_productObservedRunId')!==run)skip('Conservar las verificaciones existentes de creación y observación del producto');
    if(pg('confirmDeactivationRunId')!==run)skip('Confirmación adicional de desactivación requerida');ps('confirmDeactivationRunId','');
  }
}
const expectedUrl=pm.variables.replaceIn(base+m.route);if(pm.variables.replaceIn(pm.request.url.toString())!==expectedUrl)skip('Ruta HTTP modificada');
pm.variables.set('pcRequestBody',body?JSON.stringify(body):'');pm.variables.set('pcStepMeta',JSON.stringify(m));pm.variables.set('pcCaseKeys',JSON.stringify(Object.values(pcManifest).filter(s=>s.id===m.id).map(s=>s.key)));
st.steps[m.key]={...(prior||{}),key:m.key,caseId:m.id,method:m.method,path:pm.variables.replaceIn(m.route),done:false,flowReady:false};save();

};
