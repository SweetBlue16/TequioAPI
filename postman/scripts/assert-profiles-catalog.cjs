// This function supplies the corresponding standalone Postman event.
module.exports = function assertProfilesCatalog({ pm, Date, require }) {
{
  const pe=pm.environment,m=JSON.parse(pm.variables.get('pcStepMeta')),st=JSON.parse(pe.get('_pcState')),who=JSON.parse(pm.variables.get('pcIdentity'));
  const run=st.runId,code=pm.response.code,rec=st.steps[m.key],checks=[];
  let data;try{data=pm.response.json();}catch(_){data=null;}
  const obj=data&&typeof data==='object'&&!Array.isArray(data);
  function check(name,value){const passed=Boolean(value);checks.push({name,passed});pm.test(m.id+' / '+m.key+' — '+name,function(){pm.expect(passed,'Contract failed; values omitted.').to.equal(true);});return passed;}
  function save(){pe.set('_pcState',JSON.stringify(st));pe.set('pcResults',JSON.stringify({runId:run,cases:st.cases}));pe.set('pcEvidence',JSON.stringify(Object.values(st.steps).filter(x=>x.responses).map(x=>({caseId:x.caseId,step:x.key,method:x.method,path:x.path,responses:x.responses,status:x.status,reason:x.reason||''}))));}
  const secretKeys=/^(password|passwordHash|newPassword|token|jwt|accessToken|refreshToken|otp|verificationCode|code|apiKey|apiSecret|secret)$/i;
  const secrets=['buyerPassword','producerPassword','newPassword','jwt','expiredJwt','verificationOtp','recoveryOtp','authBuyerJwt','authProducerJwt','authAttemptsJwt','authRecoveryJwt','authExpiredCandidateJwt'].map(k=>String(pe.get(k)||'')).filter(Boolean);
  function leaks(v){if(Array.isArray(v))return v.some(leaks);if(v&&typeof v==='object')return Object.entries(v).some(([k,x])=>secretKeys.test(k)||leaks(x));return typeof v==='string'&&secrets.some(secret=>v===secret||(secret.length>=8&&v.includes(secret)));}
  const empty413=m.action==='upload'&&m.fixture==='over'&&code===413&&!obj&&pm.response.text().trim()==='';
  const safe=check('Response follows the JSON contract or an empty 413 and exposes no secrets',(obj&&/application\/(?:[a-z.+-]*\+)?json/i.test(pm.response.headers.get('Content-Type')||'')&&!leaks(data))||empty413);
  if(m.action!=='rawUpload')check('HTTP status matches the expected contract',m.statuses.includes(code));
  function urlOK(url){if(typeof url!=='string')return false;const match=url.match(/^(https:\/\/[^/?#]+)(\/[^?#]*)$/);if(!match||match[1]!==pe.get('pcStorageOrigin'))return false;let p;try{p=decodeURIComponent(match[2]);}catch(_){return false;}return p.startsWith(pe.get('pcStoragePathPrefix'))&&p.includes('/tequio-profiles/')&&!p.includes('\\')&&!p.includes('\0')&&!p.split('/').some(part=>part==='..'||part==='.');}
  const keys=['id','email','firstName','paternalLastName','maternalLastName','birthDate','phoneNumber','locality','biography','profilePictureUrl','isVerified','registrationDate'];
  const canonical=d=>Object.fromEntries(keys.map(k=>[k,k==='birthDate'&&typeof d[k]==='string'?d[k].slice(0,10):d[k]]));
  const equal=(a,b)=>JSON.stringify(a)===JSON.stringify(b);
  const slot=m.id+'_'+(m.slot||'actor');let semantic=false,continueRead=false,incomplete=false;
  if(['snapshot','profileAfter','unchanged','pictureAfter'].includes(m.action)){
    const valid=check('Profile contract matches the authenticated fictitious user',code===200&&obj&&keys.every(k=>Object.hasOwn(data,k))&&data.id===who.id&&data.email===who.email&&typeof data.firstName==='string'&&data.firstName.startsWith('TEST_'+run)&&typeof data.isVerified==='boolean');
    if(valid&&safe){const current=canonical(data);semantic=true;st.owners[who.role]={id:who.id,email:who.email};
      if(m.action==='snapshot')st.snapshots[slot]=current;
      else{
        let expected=st.snapshots[slot];
        if(m.action==='profileAfter')expected={...expected,...st.expected[m.id],birthDate:String(st.expected[m.id].birthDate).slice(0,10)};
        if(m.action==='pictureAfter')expected={...expected,profilePictureUrl:st.uploads[m.id].url};
        semantic=check('All profile fields match the expected state without partial or foreign changes',equal(current,expected));
      }
    }
  }
  if(['update','associate'].includes(m.action))semantic=check('Mutation returns HTTP 200 with a confirmation message',code===200&&obj&&typeof data.mensaje==='string');
  if(['invalidRequired','invalidLength'].includes(m.action)){
    const field=m.action==='invalidRequired'?'firstname':'phonenumber';
    const errors=obj&&data.errors;
    semantic=check('HTTP 400 exposes a validation message for the actual DTO constraint',code===400&&errors&&Object.keys(errors).some(k=>k.toLowerCase().split('.').at(-1)===field&&Array.isArray(errors[k])&&errors[k].some(v=>typeof v==='string'&&v.length>0)));
    continueRead=Boolean(obj);
  }
  if(m.action==='upload'){
    if(m.fixture==='txt'||m.fixture==='over'){
      semantic=check('Rejected upload has no success URL and returns validation error or 413',empty413||(obj&&!Object.hasOwn(data,'url')&&typeof data.error==='string'&&(m.fixture==='txt'?code===400:[400,413].includes(code))));
      if(m.fixture==='over'&&code===400)semantic=check('Size rejection identifies the implemented 5 MiB limit',data.error==='La imagen supera el límite máximo permitido de 5MB.')&&semantic;
      continueRead=Boolean(obj||empty413);
    }else{
      semantic=check('Successful upload returns an authorized public URL',code===200&&obj&&typeof data.mensaje==='string'&&urlOK(data.url));
      if(semantic&&safe){st.uploads[m.id]={runId:run,ownerId:who.id,url:data.url};if(m.id==='CP-MUL-01'){pe.set('uploadedImageUrl',data.url);pe.set('_uploadRunId',run);pe.set('_uploadAccount',who.role);pe.set('_uploadUserId',String(who.id));}}
    }
  }
  if(m.action==='rawUpload'){
    semantic=check('Filename probe records an image API contract; a 200 alone proves no confinement',obj&&((code===200&&urlOK(data.url)&&typeof data.mensaje==='string')||([400,413,415,500].includes(code)&&typeof data.error==='string'&&!Object.hasOwn(data,'url'))));
    continueRead=Boolean(obj);
  }
  if(m.action==='create'){
    semantic=check('Creation returns HTTP 201 and a real positive product ID',code===201&&obj&&Number.isSafeInteger(data.productId)&&data.productId>0&&typeof data.mensaje==='string');
    if(semantic&&safe){const other=st.products[m.target==='created'?'batch':'created'];semantic=check('Dedicated products never reuse another captured product ID',!other||other.id!==data.productId)&&semantic;
      if(semantic)st.products[m.target]={id:data.productId,runId:run,ownerId:who.id,fields:st.expected[m.key]};
    }
  }
  if(m.action==='deactivate'){
    if(m.id==='CP-CAT-04'){
      semantic=check('RN-10 rejects deactivation with HTTP 409 Conflict, never 500',code===409&&obj&&typeof data.error==='string');continueRead=Boolean(obj&&[409,500].includes(code));
    }else semantic=check('Own product deactivation returns HTTP 200',code===200&&obj&&typeof data.mensaje==='string');
  }
  if(m.action==='scan'){
    const scan=st.scans[m.key],page=scan.next,target=st.products[m.target];
    const contract=check('Page has coherent zero-based pagination, item count and active owned products',code===200&&obj&&data.pageIndex===page-1&&data.pageSize===100&&Number.isSafeInteger(data.itemCount)&&data.itemCount>=0&&Array.isArray(data.products)&&data.products.length===Math.min(100,Math.max(0,data.itemCount-(page-1)*100))&&data.products.every(p=>Number.isSafeInteger(p.id)&&p.id>0&&p.producerId===who.id&&p.isActive===true));
    if(contract&&safe){
      if(scan.total===null)scan.total=data.itemCount;
      const stable=check('Catalog count remains stable throughout this full scan',scan.total===data.itemCount);
      const previousIds=new Set(scan.rows.map(p=>p.id));const unique=check('Pagination does not repeat product IDs',new Set(data.products.map(p=>p.id)).size===data.products.length&&data.products.every(p=>!previousIds.has(p.id)));
      if(stable&&unique){scan.rows.push(...data.products);
        if(scan.rows.length<scan.total){scan.next++;incomplete=true;pm.execution.setNextRequest(pm.info.requestName);}
        else{
          const found=scan.rows.filter(p=>p.id===target.id);
          if(m.mode==='absent'){
            const before=st.scans['CP-CAT-03.2'];
            semantic=check('Deactivated product is absent across every active page and all other products are unchanged',found.length===0&&before&&equal(scan.rows,before.rows.filter(p=>p.id!==target.id)));
          }else{
            const p=found[0],f=target.fields;
            semantic=check('Captured product persists with exact owner, name, category and payload fields',found.length===1&&p.producerId===target.ownerId&&p.name===f.name&&p.categoryId===f.categoryId&&p.shortDescription===f.shortDescription&&p.measurementUnit===f.measurementUnit&&p.imageUrl===f.imageUrl&&p.isActive===true);
            if(m.id==='CP-CAT-04'&&m.key==='CP-CAT-04.6')semantic=check('Rejected RN-10 operation leaves every visible active product field unchanged',equal(scan.rows,st.scans['CP-CAT-04.4'].rows))&&semantic;
            if(semantic){pe.set('productId',String(target.id));pe.set('_createdProductId',String(target.id));pe.set('_productRunId',run);pe.set('_productOwnerId',String(who.id));pe.set('_productObservedRunId',run);}
          }
        }
      }
    }
  }
  rec.responses=rec.responses||[];rec.responses.push({at:new Date().toISOString(),httpStatus:code,page:m.action==='scan'?Number(pm.variables.get('pcPageIndex')):null,checks});
  const passed=checks.every(c=>c.passed);
  rec.observed=Boolean(safe&&(obj||empty413));rec.done=!incomplete;rec.flowReady=Boolean(passed&&semantic&&safe);
  rec.status=passed?(incomplete?'BLOQUEADO':'APROBADO'):'FALLIDO';
  if(!passed)st.cases[m.id]={status:'FALLIDO',reason:m.id==='CP-CAT-04'&&code===500?'RN-10 respondió 500; esperado 409':'Una o más aserciones HTTP o de postcondición fallaron'};
  if(continueRead&&safe)rec.observed=true;
  const caseKeys=JSON.parse(pm.variables.get('pcCaseKeys'));
  const complete=caseKeys.every(key=>st.steps[key]&&st.steps[key].done&&st.steps[key].flowReady);
  if(complete&&st.cases[m.id].status!=='FALLIDO'){
    const partial={'CP-PER-02':'Se comprobaron restricciones DTO; el formato de email/teléfono requerido no está implementado','CP-MUL-04':'URL y perfil observados; no existe evidencia API del confinamiento físico en el proveedor','CP-CAT-04':'HTTP y producto observados; no existe consulta API para demostrar asociación al lote sin cambios'};
    st.cases[m.id]={status:partial[m.id]?'BLOQUEADO':'APROBADO',reason:partial[m.id]||'Todos los pasos y postcondiciones se ejecutaron con respuestas reales'};
  }else if(st.cases[m.id].status!=='FALLIDO')st.cases[m.id]={status:'BLOQUEADO',reason:incomplete?'Faltan páginas; no aprobar por una página aislada':'Flujo incompleto o preparación manual pendiente'};
  save();

}
}
