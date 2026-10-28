/* Offline script/structure tests only. Synthetic responses never count as integration.
 * No HTTP, Cloudinary, SMTP or SQL. Fixtures are checked in memory, not written.
 * Optional --schema LOCAL_OFFICIAL_V2_1_SCHEMA_JSON uses existing Newman/Ajv.
 */
const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const {execFileSync}=require('node:child_process');const {createRequire}=require('node:module');
const fixtures=require('./create-fixtures.cjs');const root=__dirname;
const c=JSON.parse(fs.readFileSync(path.join(root,'Tequio.postman_collection.json'),'utf8'));
const e=JSON.parse(fs.readFileSync(path.join(root,'Tequio.Development.postman_environment.json'),'utf8'));
const folder=c.item.find(f=>f.name.startsWith('06 -')),auth=c.item.find(f=>f.name.startsWith('05 -'));
const pre=folder.event.find(v=>v.listen==='prerequest').script.exec.join('\n'),post=folder.event.find(v=>v.listen==='test').script.exec.join('\n');
const manifest=JSON.parse(pre.match(/^const pcManifest = ([\s\S]*?);\n/)[1]);
const flatten=o=>o.request?[o]:(o.item||[]).flatMap(flatten);const all=flatten(c),added=flatten(folder);
const defaults=Object.fromEntries(e.values.map(v=>[v.key,v.value]));
let checks=0;function test(name,fn){fn();checks++;process.stdout.write('PASS (offline): '+name+'\n');}
test('104 requests, 11 new scenarios, all original 65 requests and 64 variables preserved',()=>{
  assert.equal(all.length,104);assert.equal(added.length,39);assert.equal(folder.item.length,11);assert.equal(e.values.length,81);
  const old=JSON.parse(execFileSync('git',['show','8bc3846:postman/Tequio.postman_collection.json'],{cwd:path.dirname(root),encoding:'utf8'}));
  const oldEnv=JSON.parse(execFileSync('git',['show','8bc3846:postman/Tequio.Development.postman_environment.json'],{cwd:path.dirname(root),encoding:'utf8'}));
  assert.deepEqual(c.item.slice(0,6),old.item);assert.deepEqual(e.values.slice(0,64),oldEnv.values);
  assert.equal(folder.item.find(f=>f.name.startsWith('CP-CAT-02')).item.length,0);assert.equal(folder.item.find(f=>f.name.startsWith('CP-CAT-05')).item.length,0);
  for(const event of old.event){const prior=event.script.exec.join('\n').split('\n').slice(1).join('\n');const current=c.event.find(v=>v.listen===event.listen).script.exec.join('\n').split('\n').slice(1).join('\n');assert.equal(current,prior);}
});
test('valid JavaScript, defined variables, no live secrets or automatic external requests',()=>{
  const visit=o=>{for(const event of o.event||[])new vm.Script(event.script.exec.join('\n'));for(const item of o.item||[])visit(item);};visit(c);
  const names=new Set([...e.values.map(v=>v.key),'requestBody','authRequestBody','authRequestJwt','pcRequestBody','pcRequestJwt','pcProducerId','pcProductId','pcPageIndex','pcBoundary','pcMultipartBody']);
  for(const item of all)for(const match of JSON.stringify(item.request).matchAll(/\{\{([^{}]+)\}\}/g))assert.ok(names.has(match[1]));
  for(const value of e.values)if(value.type==='secret')assert.equal(value.value,'');
  assert.equal(defaults.pcSelectedCase,'');assert.equal(defaults.pcAllowExternalUploads,'false');assert.equal(defaults.pcBatchStage,'disabled');
  assert.equal(defaults.baseUrl,'');assert.equal(defaults.allowMutatingTests,'false');assert.equal(defaults.developmentEnvironmentConfirmed,'false');
  assert.equal(/pm\.sendRequest\s*\(|console\.(log|warn|error)\s*\(/.test(pre+post),false);
  assert.ok(post.includes('pm.execution.setNextRequest(pm.info.requestName)'));
});
test('PNG signature, pixel content, CRCs and exact fixture sizes are valid without writing large files',()=>{
  const samples=fixtures.fixtures('a'.repeat(32));
  for(const key of ['small','exact','over'])assert.equal(fixtures.verifyPng(samples[key]),true);
  assert.ok(samples.small.length<fixtures.LIMIT);assert.equal(samples.exact.length,5242880);assert.equal(samples.over.length,5242881);
  assert.ok(samples.txt.toString().includes('harmless plain text'));assert.equal(samples.txt.toString().includes('<script'),false);
});
let schemaChecked=false,sdkChecked=false,requireNewman;
try{requireNewman=createRequire(require.resolve('newman/package.json',{paths:[path.join(process.env.APPDATA||'','npm/node_modules')]}));}catch{/* optional existing tool */}
if(requireNewman)test('Postman SDK deserializes the expanded collection and environment',()=>{const sdk=requireNewman('postman-collection');assert.equal(new sdk.Collection(c).items.count(),7);assert.equal(new sdk.VariableScope(e).values.count(),81);sdkChecked=true;});
const si=process.argv.indexOf('--schema');if(si>=0)test('official Collection v2.1 schema',()=>{assert.ok(requireNewman);const Ajv=requireNewman('ajv'),ajv=new Ajv({schemaId:'id',meta:false,allErrors:true,logger:false});ajv.addMetaSchema(requireNewman('ajv/lib/refs/json-schema-draft-04.json'));const validate=ajv.compile(JSON.parse(fs.readFileSync(process.argv[si+1],'utf8')));assert.equal(validate(c),true,JSON.stringify(validate.errors));schemaChecked=true;});
const run='a'.repeat(32),origin='http://localhost:12345',storage='https://images.example.test';
class Runner{
  constructor(configured=true){
    this.env={...defaults};this.count=0;this.next=null;
    this.profiles={buyer:{id:7,email:'test_buyer_'+run+'@example.test',firstName:'TEST_'+run+'_buyer',paternalLastName:'TEST_Apellido',maternalLastName:null,birthDate:'1990-01-02',phoneNumber:null,locality:null,biography:null,profilePictureUrl:null,isVerified:true,registrationDate:'2025-01-01T00:00:00'},producer:{id:8,email:'test_producer_'+run+'@example.test',firstName:'TEST_'+run+'_producer',paternalLastName:'TEST_Apellido',maternalLastName:null,birthDate:'1990-01-02',phoneNumber:null,locality:null,biography:null,profilePictureUrl:null,isVerified:true,registrationDate:'2025-01-01T00:00:00'}};
    this.products=[];this.sequence=1000;
    if(configured){const jwt=(id,role)=>'OFFLINE.'+Buffer.from(JSON.stringify({sub:String(id),roleId:String(role),exp:4102444800})).toString('base64url')+'.OFFLINE';
      const buffers=fixtures.fixtures(run);const fm={runId:run,files:Object.fromEntries(Object.entries(buffers).map(([k,b])=>[k,{name:fixtures.filename(run,k),size:b.length,sha256:fixtures.digest(b)}]))};
      Object.assign(this.env,{baseUrl:origin,authorizedDevelopmentUrl:origin,developmentEnvironmentConfirmed:'true',allowMutatingTests:'true',runId:run,buyerEmailTemplate:'TEST_buyer_{{runId}}@example.test',producerEmailTemplate:'TEST_producer_{{runId}}@example.test',testInboxConfirmed:'true',emailRoutingConfirmed:'true',buyerUserId:'7',producerUserId:'8',authBuyerJwt:jwt(7,1),authProducerJwt:jwt(8,2),pcStorageOrigin:storage,pcStoragePathPrefix:'/test/image/upload/',pcFixtureManifest:JSON.stringify(fm),pcFixturesConfirmedRunId:run,categoryId:'2',_authState:JSON.stringify({runId:run,origin,ids:{buyer:'7',producer:'8'},claims:{buyer:{sub:'7'},producer:{sub:'8'}}})});
    }
  }
  state(){return JSON.parse(this.env._pcState||'null');}
  consent(){this.env.pcAllowExternalUploads='true';this.env.pcExternalUploadConfirmedRunId=run;}
  url(){return storage+'/test/image/upload/v1/tequio-profiles/TEST_'+run+'.png';}
  fixture(m,local){const data=JSON.parse(local.pcRequestBody||'null');
    if(['snapshot','unchanged','profileAfter','pictureAfter'].includes(m.action))return {status:200,data:{...this.profiles[m.role]}};
    if(m.action==='update'){Object.assign(this.profiles[m.role],data);return {status:200,data:{mensaje:'Guardado.'}};}
    if(m.action==='invalidRequired')return {status:400,data:{errors:{FirstName:['El nombre es obligatorio.']}}};
    if(m.action==='invalidLength')return {status:400,data:{errors:{PhoneNumber:['El número de teléfono no puede exceder los 20 caracteres.']}}};
    if(m.action==='upload')return m.fixture==='txt'?{status:400,data:{error:'Formato no permitido.'}}:m.fixture==='over'?{status:400,data:{error:'La imagen supera el límite máximo permitido de 5MB.'}}:{status:200,data:{mensaje:'Subido.',url:this.url()}};
    if(m.action==='rawUpload')return {status:500,data:{error:'Ocurrió un error al procesar la solicitud en la base de datos.'}};
    if(m.action==='associate'){this.profiles[m.role].profilePictureUrl=data.pictureUrl;return {status:200,data:{mensaje:'Asociado.'}};}
    if(m.action==='create'){const id=++this.sequence;this.products.push({id,producerId:8,...data,isActive:true});return {status:201,data:{mensaje:'Creado.',productId:id}};}
    if(m.action==='deactivate'){if(m.id==='CP-CAT-04')return {status:409,data:{error:'No se puede dar de baja el producto porque tiene lotes activos o confirmados.'}};this.products.find(p=>p.id===Number(local.pcProductId)).isActive=false;return {status:200,data:{mensaje:'Desactivado.'}};}
    if(m.action==='scan'){const rows=this.products.filter(p=>p.producerId===8&&p.isActive).sort((a,b)=>a.id-b.id);const index=Number(local.pcPageIndex);return {status:200,data:{pageIndex:index-1,pageSize:100,itemCount:rows.length,products:rows.slice((index-1)*100,index*100)}};}
    throw new Error('Unknown offline fixture action');
  }
  run(key,response='fixture',options={}){
    const m=Object.values(manifest).find(v=>v.key===key||v.name===key);const item=m?added.find(i=>i.name===m.name):all.find(i=>i.name===key);assert.ok(item);
    const local={},results=[];this.next=null;
    const replace=text=>String(text).replace(/\{\{([^{}]+)\}\}/g,(_,k)=>k==='$guid'?'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb':String(local[k]??this.env[k]??'{{'+k+'}}'));
    const test=(name,fn)=>{try{fn();results.push({name,passed:true});}catch{results.push({name,passed:false});}};test.skip=()=>{};
    const pm={environment:{get:k=>this.env[k],set:(k,v)=>{this.env[k]=String(v);}},variables:{get:k=>local[k]??this.env[k],set:(k,v)=>{local[k]=v;},replaceIn:replace},info:{requestName:item.name},request:{method:options.method||item.request.method,url:options.url||item.request.url,body:{formdata:{all:()=>options.noFile?[]:[{key:'file',type:'file',src:options.file||'C:/OFFLINE/'+(m&&m.fixture?fixtures.filename(run,m.fixture):'unused')}]}}},test,expect:(value,message)=>({to:{equal:expected=>assert.equal(value,expected,message)}}),execution:{skipRequest:()=>{throw new Error('OFFLINE_SKIP');},setNextRequest:name=>{this.next=name;}}};
    const context=()=>({pm,require:name=>{if(name==='buffer')return require('node:buffer');if(name==='crypto-js')return {SHA256:text=>({toString:()=>crypto.createHash('sha256').update(text).digest('hex')})};throw new Error('Forbidden offline module');}});
    let blocked=false;
    try{vm.runInNewContext(c.event.find(v=>v.listen==='prerequest').script.exec.join('\n'),context());if(m)vm.runInNewContext(pre,context());else if(item.name.startsWith('CP-AUT-'))vm.runInNewContext(auth.event.find(v=>v.listen==='prerequest').script.exec.join('\n'),context());}catch(error){if(error.message==='OFFLINE_SKIP')blocked=true;else throw error;}
    if(!blocked&&response&&m){const r=response==='fixture'?this.fixture(m,local):response;pm.response={code:r.status,json:()=>{if(r.invalidJson)throw new Error('Invalid JSON');return r.data;},headers:{get:()=>r.contentType||'application/json'},text:()=>r.data===null?'':JSON.stringify(r.data)};this.count++;vm.runInNewContext(post,context());}
    return {blocked,local,results,next:this.next};
  }
  case(id){this.env.pcSelectedCase=id;for(const m of Object.values(manifest).filter(v=>v.id===id)){let output,loops=0;do{output=this.run(m.key);if(++loops>100)throw new Error('Offline paging loop');}while(output.next&&!output.blocked);}}
}
test('all 104 requests are skipped with exported safe defaults',()=>{for(const item of all){const r=new Runner(false);assert.equal(r.run(item.name).blocked,true);assert.equal(r.count,0);}});
test('explicit selection, URL authorization, mutation flag and provenance cannot be bypassed',()=>{
  for(const changes of [{pcSelectedCase:''},{allowMutatingTests:'false'},{developmentEnvironmentConfirmed:'false'},{authorizedDevelopmentUrl:'https://production.invalid'},{_authState:''},{buyerUserId:'99'}]){const r=new Runner();r.env.pcSelectedCase='CP-PER-01';Object.assign(r.env,changes);assert.equal(r.run('CP-PER-01.1').blocked,true);}
});
test('CP-PER-01 compares every updated field and an independent controlled profile',()=>{const r=new Runner();r.case('CP-PER-01');assert.equal(r.state().cases['CP-PER-01'].status,'APROBADO');assert.equal(r.profiles.producer.firstName,'TEST_'+run+'_producer');assert.equal(r.profiles.buyer.firstName,'TEST_'+run+'_PER');});
test('CP-PER-01 detects unintended writes to the observer profile',()=>{const r=new Runner();r.env.pcSelectedCase='CP-PER-01';for(const key of [1,2,3,4])r.run('CP-PER-01.'+key);r.profiles.producer.biography='Unexpected change';r.run('CP-PER-01.5');assert.equal(r.state().cases['CP-PER-01'].status,'FALLIDO');});
test('DTO rejection and unchanged GET remain partial, not approval of missing format validation',()=>{const r=new Runner();r.case('CP-PER-02');assert.equal(r.state().steps['CP-PER-02.5'].flowReady,true);assert.equal(r.state().cases['CP-PER-02'].status,'BLOQUEADO');});
test('a rejected profile PUT with partial persistence fails the following GET comparison',()=>{const r=new Runner();r.env.pcSelectedCase='CP-PER-02';r.run('CP-PER-02.1');r.run('CP-PER-02.2');r.profiles.buyer.locality='TEST_UNEXPECTED';r.run('CP-PER-02.3');assert.equal(r.state().cases['CP-PER-02'].status,'FALLIDO');});
test('external uploads require separate consent and exact local fixture selection',()=>{const r=new Runner();r.env.pcSelectedCase='CP-MUL-01';r.run('CP-MUL-01.1');assert.equal(r.run('CP-MUL-01.2').blocked,true);r.consent();assert.equal(r.run('CP-MUL-01.2','fixture',{noFile:true}).blocked,true);assert.equal(r.run('CP-MUL-01.2','fixture',{file:'C:/OFFLINE/other.png'}).blocked,true);});
test('a successful real-shaped image response is associated and verified by profile GET',()=>{const r=new Runner();r.consent();r.case('CP-MUL-01');assert.equal(r.state().cases['CP-MUL-01'].status,'APROBADO');assert.equal(r.env.uploadedImageUrl,r.url());assert.equal(r.profiles.buyer.profilePictureUrl,r.url());});
test('an upload URL outside the authorized origin or path fails and cannot be associated',()=>{for(const url of ['https://other.example.test/image.png',storage+'/test/image/upload/%2e%2e/tequio-profiles/image.png']){const r=new Runner();r.consent();r.env.pcSelectedCase='CP-MUL-01';r.run('CP-MUL-01.1');r.run('CP-MUL-01.2',{status:200,data:{mensaje:'OK',url}});assert.equal(r.state().cases['CP-MUL-01'].status,'FALLIDO');assert.equal(r.run('CP-MUL-01.3').blocked,true);}});
test('accepting a .txt file or returning provider 500 cannot pass CP-MUL-02',()=>{for(const response of [{status:200,data:{mensaje:'OK',url:'https://images.example.test/upload.png'}},{status:500,data:{error:'Error interno.'}}]){const r=new Runner();r.consent();r.env.pcSelectedCase='CP-MUL-02';r.run('CP-MUL-02.1');r.run('CP-MUL-02.2',response);r.run('CP-MUL-02.3');assert.equal(r.state().cases['CP-MUL-02'].status,'FALLIDO');}});
test('CP-MUL-03 validates both exact limits and the over-limit 400 response',()=>{const r=new Runner();r.consent();r.case('CP-MUL-03');assert.equal(r.state().cases['CP-MUL-03'].status,'APROBADO');assert.equal(r.state().steps['CP-MUL-03.4'].responses[0].httpStatus,400);});
test('an empty HTTP 413 is accepted for the excessive file and still followed by an unchanged GET',()=>{const r=new Runner();r.consent();r.env.pcSelectedCase='CP-MUL-03';for(const n of [1,2,3])r.run('CP-MUL-03.'+n);r.run('CP-MUL-03.4',{status:413,data:null});r.run('CP-MUL-03.5');assert.equal(r.state().cases['CP-MUL-03'].status,'APROBADO');assert.equal(r.state().steps['CP-MUL-03.4'].responses[0].httpStatus,413);});
test('filename probes transmit harmless multipart names but never approve physical confinement',()=>{const r=new Runner();r.consent();r.env.pcSelectedCase='CP-MUL-04';r.run('CP-MUL-04.1');const result=r.run('CP-MUL-04.2');assert.ok(result.local.pcMultipartBody.includes('filename="../../TEST_'));assert.ok(result.local.pcMultipartBody.includes('name="file"'));r.run('CP-MUL-04.3');r.run('CP-MUL-04.4');assert.equal(r.state().cases['CP-MUL-04'].status,'BLOQUEADO');});
test('creation captures a real ID and exact persisted product fields',()=>{const r=new Runner();r.case('CP-CAT-01');assert.equal(r.state().cases['CP-CAT-01'].status,'APROBADO');assert.equal(r.env.productId,'1001');});
test('missing productId cannot be invented or followed by deactivation',()=>{const r=new Runner();r.env.pcSelectedCase='CP-CAT-01';r.run('CP-CAT-01.1');r.run('CP-CAT-01.2',{status:201,data:{mensaje:'Creado.'}});assert.equal(r.env.productId,'');assert.equal(r.state().products.created,undefined);});
test('deactivation requires the captured own product and the original one-use confirmation',()=>{const r=new Runner();r.case('CP-CAT-01');r.env.pcSelectedCase='CP-CAT-03';r.run('CP-CAT-03.1');r.run('CP-CAT-03.2');assert.equal(r.run('CP-CAT-03.3').blocked,true);r.env.confirmDeactivationRunId=run;r.env.productId='99999';assert.equal(r.run('CP-CAT-03.3').blocked,true);r.env.productId='1001';r.run('CP-CAT-03.3');assert.equal(r.env.confirmDeactivationRunId,'');r.run('CP-CAT-03.4');assert.equal(r.state().cases['CP-CAT-03'].status,'APROBADO');});
test('pagination follows every page before declaring absence and preserves other active products',()=>{const r=new Runner();for(let i=1;i<=105;i++)r.products.push({id:i,producerId:8,categoryId:2,name:'TEST_OTHER_'+i,shortDescription:'TEST',measurementUnit:'kg',imageUrl:null,isActive:true});r.case('CP-CAT-01');r.env.confirmDeactivationRunId=run;r.case('CP-CAT-03');assert.equal(r.state().cases['CP-CAT-03'].status,'APROBADO');assert.equal(r.state().steps['CP-CAT-03.4'].responses.length,2);assert.equal(r.state().scans['CP-CAT-03.4'].rows.length,105);});
test('a page cap leaves absence unproven and blocks approval',()=>{const r=new Runner();for(let i=1;i<=105;i++)r.products.push({id:i,producerId:8,categoryId:2,name:'TEST_OTHER_'+i,shortDescription:'TEST',measurementUnit:'kg',imageUrl:null,isActive:true});r.env.pcMaxPages='1';r.case('CP-CAT-01');assert.equal(r.state().cases['CP-CAT-01'].status,'BLOQUEADO');assert.equal(r.state().steps['CP-CAT-01.3'].done,false);});
test('CAT-04 is blocked without an approved batch preparation mechanism',()=>{const r=new Runner();r.env.pcSelectedCase='CP-CAT-04';assert.equal(r.run('CP-CAT-04.1').blocked,true);assert.equal(r.count,0);});
function batchRunner(){const r=new Runner();Object.assign(r.env,{pcSelectedCase:'CP-CAT-04',pcBatchStage:'prepare-product',pcBatchPreparationApprovedRunId:run,pcBatchPreparationMechanism:'OFFLINE_ONLY_APPROVAL'});r.case('CP-CAT-04');Object.assign(r.env,{pcBatchStage:'probe',pcBatchLinkedConfirmedRunId:run,pcBatchState:'Draft',pcBatchEvidenceRef:'OFFLINE_ONLY_ASSOCIATION',confirmDeactivationRunId:run});r.run('CP-CAT-04.4');return r;}
test('RN-10 HTTP 500 fails the 409 assertion while GET still confirms active product',()=>{const r=batchRunner();r.run('CP-CAT-04.5',{status:500,data:{error:'Ocurrió un error al procesar la solicitud en la base de datos.'}});r.run('CP-CAT-04.6');assert.equal(r.state().steps['CP-CAT-04.6'].flowReady,true);assert.equal(r.state().cases['CP-CAT-04'].status,'FALLIDO');});
test('even a 409 cannot prove the unavailable batch association postcondition',()=>{const r=batchRunner();r.run('CP-CAT-04.5');r.run('CP-CAT-04.6');assert.equal(r.state().cases['CP-CAT-04'].status,'BLOQUEADO');});
test('HTTP evidence never stores credentials, tokens, request bodies or fixture content',()=>{const r=new Runner();r.case('CP-PER-01');const evidence=JSON.parse(r.env.pcEvidence);for(const item of evidence)assert.deepEqual(Object.keys(item).sort(),['caseId','step','method','path','responses','status','reason'].sort());assert.equal(r.env.pcEvidence.includes(r.env.authBuyerJwt),false);assert.equal(r.env.pcEvidence.includes(r.env.authProducerJwt),false);});
process.stdout.write(JSON.stringify({kind:'OFFLINE_SCRIPT_VALIDATION_ONLY',offlineChecksPassed:checks,schemaChecked,sdkChecked,totalRequests:all.length,newScenarios:11,realHttpRequests:0,sqlOperations:0,largeFixtureFilesWritten:0})+'\n');
