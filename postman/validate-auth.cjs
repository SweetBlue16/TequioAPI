/* Offline validation of Postman artifacts and scripts, NOT integration evidence.
 * No HTTP, SMTP or SQL is performed. All response fixtures exist only in memory.
 * Optional: --schema PATH_TO_OFFICIAL_COLLECTION_V2_1_JSON (uses existing Newman/Ajv).
 */
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const { createRequire } = require('node:module');
const { execFileSync } = require('node:child_process');
const root = __dirname;
const collection = JSON.parse(fs.readFileSync(path.join(root,'Tequio.postman_collection.json'),'utf8'));
const environment = JSON.parse(fs.readFileSync(path.join(root,'Tequio.Development.postman_environment.json'),'utf8'));
const suite = collection.item.find(item=>item.name.startsWith('05 -'));
const all = collection.item.flatMap(folder=>folder.item.flatMap(item=>item.request?[item]:item.item||[]));
const additions = suite.item.flatMap(folder=>folder.item);
const originals = collection.item.slice(0,5).flatMap(folder=>folder.item);
const envDefaults = Object.fromEntries(environment.values.map(v=>[v.key,v.value]));
const pre = suite.event.find(e=>e.listen==='prerequest').script.exec.join('\n');
const post = suite.event.find(e=>e.listen==='test').script.exec.join('\n');
const manifest = JSON.parse(pre.match(/^const authManifest = ([\s\S]*?);\n/)[1]);
let checks = 0;
function test(label, callback) { callback(); checks++; process.stdout.write('PASS (offline): '+label+'\n'); }
test('JSON, unique names, 13 scenario folders and 16+49 requests',()=>{
  assert.equal(suite.item.length,13);assert.equal(originals.length,16);assert.equal(additions.length,49);assert.equal(all.length,65);
  assert.equal(new Set(all.map(i=>i.name)).size,65);
  for(let i=1;i<=13;i++) assert.ok(suite.item[i-1].name.startsWith('CP-AUT-'+String(i).padStart(2,'0')+' — '));
  assert.equal(suite.item[4].item.length,0);assert.equal(suite.item[9].item.length,0);
});
test('original 16 request definitions remain identical to committed activity 6.0',()=>{
  const baseline=JSON.parse(execFileSync('git',['show','f701003:postman/Tequio.postman_collection.json'],{cwd:path.dirname(root),encoding:'utf8'}));
  assert.deepEqual(collection.item.slice(0,5),baseline.item);
});
test('script syntax, variable references, secret defaults and audited methods/routes',()=>{
  const walk=o=>{for(const event of o.event||[])new vm.Script(event.script.exec.join('\n'));for(const item of o.item||[])walk(item);};walk(collection);
  assert.equal(new Set(environment.values.map(v=>v.key)).size,environment.values.length);
  for(const value of environment.values) if(value.type==='secret')assert.equal(value.value,'');
  for(const key of ['baseUrl','authorizedDevelopmentUrl','_authState','authResults','authEvidence'])assert.equal(envDefaults[key],'');
  assert.equal(envDefaults.allowMutatingTests,'false');assert.equal(envDefaults.developmentEnvironmentConfirmed,'false');
  const variables=new Set([...Object.keys(envDefaults),'requestBody','authRequestBody','authRequestJwt']);
  for(const item of all)for(const match of JSON.stringify(item.request).matchAll(/\{\{([^{}]+)\}\}/g))assert.ok(variables.has(match[1]),'Defined variable: '+match[1]);
  for(const meta of Object.values(manifest)){
    assert.ok(['GET','POST'].includes(meta.method));
    assert.ok(['/api/v1/users/me','/api/v1/auth/register','/api/v1/auth/login','/api/v1/auth/verify','/api/v1/auth/resend-verification-code','/api/v1/auth/recover','/api/v1/auth/reset-password'].includes(meta.route));
    for(const dependency of meta.requires)assert.ok(Object.values(manifest).some(m=>m.key===dependency));
  }
  assert.equal(/pm\.sendRequest\s*\(|console\.(log|warn|error)\s*\(/.test(pre+'\n'+post),false);
});
let officialSchema=false,sdkImport=false;
let newmanRequire;
try {newmanRequire=createRequire(require.resolve('newman/package.json',{paths:[path.join(process.env.APPDATA||'','npm/node_modules')]}));}catch{/* optional installed tools */}
if(newmanRequire){
  test('installed Postman SDK deserializes the collection and environment',()=>{
    const sdk=newmanRequire('postman-collection');assert.equal(new sdk.Collection(collection).items.count(),6);
    assert.equal(new sdk.VariableScope(environment).values.count(),environment.values.length);sdkImport=true;
  });
}
const schemaIndex=process.argv.indexOf('--schema');
if(schemaIndex>=0){
  assert.ok(newmanRequire,'Schema validation requires the already-installed Newman/Ajv; do not install automatically.');
  test('official Collection v2.1 schema validation',()=>{
    const Ajv=newmanRequire('ajv');const ajv=new Ajv({schemaId:'id',meta:false,allErrors:true,logger:false});
    ajv.addMetaSchema(newmanRequire('ajv/lib/refs/json-schema-draft-04.json'));
    const validate=ajv.compile(JSON.parse(fs.readFileSync(process.argv[schemaIndex+1],'utf8')));
    assert.equal(validate(collection),true,JSON.stringify(validate.errors));officialSchema=true;
  });
}
const runId='a'.repeat(32);
class OfflineRunner {
  constructor(configured=true){
    this.clock=Date.parse('2026-10-28T18:00:00Z');this.tokens=0;this.responses=0;
    this.env={...envDefaults};
    if(configured)Object.assign(this.env,{baseUrl:'http://localhost:12345',authorizedDevelopmentUrl:'http://localhost:12345',developmentEnvironmentConfirmed:'true',allowMutatingTests:'true',runId,
      testInboxAddress:'inbox@example.test',testInboxConfirmed:'true',emailRoutingConfirmed:'true',buyerEmailTemplate:'TEST_buyer_{{runId}}@example.test',producerEmailTemplate:'TEST_producer_{{runId}}@example.test',
      buyerPassword:'TEST_Fixture_Buyer123!',producerPassword:'TEST_Fixture_Producer123!',newPassword:'TEST_Fixture_New123!'});
  }
  state(){return JSON.parse(this.env._authState||'null');}
  jwt(persona){
    const id={buyer:7,producer:8,attempts:9,recovery:10}[persona];
    const claims={sub:String(id),roleId:persona==='producer'?'2':'1',exp:Math.floor(this.clock/1000)+7200,jti:String(++this.tokens)};
    return 'OFFLINE.'+Buffer.from(JSON.stringify(claims)).toString('base64url')+'.OFFLINE';
  }
  profile(meta){
    const state=this.state();const suffix={buyer:'',producer:'',attempts:'-otp',recovery:'-rec'}[meta.persona];
    const template=this.env[meta.persona==='producer'?'producerEmailTemplate':'buyerEmailTemplate'];
    return {id:Number(state.claims[meta.persona].sub),email:template.replace('{{runId}}',runId+suffix).toLowerCase(),firstName:'TEST_'+runId+'_'+meta.persona,paternalLastName:'TEST_Apellido',isVerified:typeof meta.verified==='boolean'?meta.verified:(state.verified[meta.persona]??false)};
  }
  fixture(meta){
    let status=meta.statuses[0],data={mensaje:'Confirmación ficticia para validación offline.'};
    if(meta.action==='login')data.token=this.jwt(meta.persona);
    if(['profile','attest'].includes(meta.action))data=this.profile(meta);
    if(meta.action==='duplicate')data={error:'El correo electrónico ya se encuentra registrado.'};
    if(meta.action==='minor')data={error:'Debes ser mayor de 18 años para registrarte.'};
    if(['badLogin','unknownLogin','oldLogin','alternateLogin'].includes(meta.action))data={error:'El correo o la contraseña son incorrectos.'};
    if(['wrongOtp','originalOtp','reuseOtp','invalidRecovery'].includes(meta.action)){status=400;data={error:'El código de verificación es incorrecto.'};}
    if(meta.action==='expired')data=null;
    return {status,data};
  }
  run(key,response='fixture',options={}){
    const meta=Object.values(manifest).find(m=>m.key===key || m.name===key);
    const item=meta?additions.find(i=>i.name===meta.name):all.find(i=>i.name===key);
    assert.ok(item,'Known fixture step');
    const local={};const assertions=[];const skipped=[];
    const replace=text=>String(text).replace(/\{\{([^{}]+)\}\}/g,(_,name)=>name==='$guid'?'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb':String(local[name]??this.env[name]??'{{'+name+'}}'));
    const test=(label,callback)=>{try{callback();assertions.push({label,passed:true});}catch{assertions.push({label,passed:false});}};
    test.skip=label=>skipped.push(label);
    const pm={environment:{get:k=>this.env[k],set:(k,v)=>{this.env[k]=String(v);}},variables:{get:k=>local[k]??this.env[k],set:(k,v)=>{local[k]=v;},replaceIn:replace},
      info:{requestName:item.name},request:{method:options.method||item.request.method,url:options.url||item.request.url},
      execution:{skipRequest:()=>{throw new Error('OFFLINE_SKIP');}},test,expect:(value,message)=>({to:{equal:expected=>assert.equal(value,expected,message)}})};
    const that=this;
    class ClockDate extends Date{constructor(...args){super(...(args.length?args:[that.clock]));}static now(){return that.clock;}}
    const context=()=>({pm,Date:ClockDate,require:name=>{
      if(name==='buffer')return require('node:buffer');
      if(name==='crypto-js')return {SHA256:text=>({toString:()=>crypto.createHash('sha256').update(text).digest('hex')})};
      throw new Error('Offline validator forbids module: '+name);
    }});
    let blocked=false;
    try{
      vm.runInNewContext(collection.event.find(e=>e.listen==='prerequest').script.exec.join('\n'),context());
      if(meta)vm.runInNewContext(pre,context());
    }catch(error){if(error.message==='OFFLINE_SKIP')blocked=true;else throw error;}
    if(!blocked && response && meta){
      const fixture=response==='fixture'?this.fixture(meta):response;
      pm.response={code:fixture.status,json:()=>{if(fixture.data===null || fixture.invalidJson)throw new Error('No JSON');return fixture.data;},headers:{get:()=>fixture.contentType||'application/json'},text:()=>fixture.data===null?'':JSON.stringify(fixture.data)};
      this.responses++;vm.runInNewContext(post,context());
    }
    return {blocked,assertions,skipped,local,meta};
  }
  stage(stage){this.env.authStage=stage;for(const meta of Object.values(manifest).filter(m=>m.stage===stage))this.run(meta.key);}
}
test('all 65 requests blocked with the sanitized exported defaults',()=>{
  for(const item of all){const runner=new OfflineRunner(false);assert.equal(runner.run(item.name).blocked,true);assert.equal(runner.responses,0);}
});
test('origin confirmation, mutation permission, mail routing, method and route gates',()=>{
  for(const overrides of [{allowMutatingTests:'false'},{developmentEnvironmentConfirmed:'false'},{authorizedDevelopmentUrl:'https://production.invalid'},{emailRoutingConfirmed:'false'},{buyerEmailTemplate:'thirdparty@example.test'}]){
    const runner=new OfflineRunner();Object.assign(runner.env,overrides);assert.equal(runner.run('01.1').blocked,true);
  }
  assert.equal(new OfflineRunner().run('01.1','fixture',{method:'DELETE'}).blocked,true);
  assert.equal(new OfflineRunner().run('01.1','fixture',{url:'https://production.invalid/api/v1/auth/register'}).blocked,true);
});
test('register has no fabricated ID and a duplicate step cannot be sent twice',()=>{
  const runner=new OfflineRunner();runner.run('01.1');assert.equal(runner.state().ids.buyer,undefined);
  assert.equal(runner.run('01.1').blocked,true);assert.equal(runner.responses,1);
});
test('SMTP 503 is blocked, may have persisted, and registration is never retried blindly',()=>{
  const runner=new OfflineRunner();runner.run('01.1',{status:503,data:{error:'No fue posible enviar el correo en este momento.'}});
  assert.equal(runner.state().cases['CP-AUT-01'].status,'BLOQUEADO');assert.match(runner.state().cases['CP-AUT-01'].reason,/SMTP/);
  runner.run('01.1');assert.equal(runner.responses,1);
});
test('an interrupted request remains indeterminate and cannot be resent automatically',()=>{
  const runner=new OfflineRunner();assert.equal(runner.run('01.1',null).blocked,false);
  assert.equal(runner.run('01.1').blocked,true);assert.match(runner.state().cases['CP-AUT-01'].reason,/indeterminado/);
});
test('a malformed or wrong-role login cannot populate the secure JWT variable',()=>{
  for(const token of ['invalid','OFFLINE.'+Buffer.from(JSON.stringify({sub:'7',roleId:'4',exp:4102444800})).toString('base64url')+'.OFFLINE']){
    const runner=new OfflineRunner();runner.run('01.1');runner.run('01.2',{status:200,data:{mensaje:'OK',token}});
    assert.equal(runner.env.authBuyerJwt,'');assert.equal(runner.state().cases['CP-AUT-01'].status,'FALLIDO');
  }
});
test('a leaked password makes the contract fail without copying it into evidence',()=>{
  const runner=new OfflineRunner();runner.run('01.1',{status:201,data:{mensaje:'OK',password:runner.env.buyerPassword}});
  assert.equal(runner.state().cases['CP-AUT-01'].status,'FALLIDO');assert.equal(runner.env.authEvidence.includes(runner.env.buyerPassword),false);
});
const flow=new OfflineRunner();
test('prepare phase records only completed scenarios and leaves OTP-dependent cases incomplete',()=>{
  flow.stage('prepare');const state=flow.state();
  for(const id of ['01','02','03','04'])assert.equal(state.cases['CP-AUT-'+id].status,'APROBADO');
  assert.equal(state.cases['CP-AUT-05'].status,'NO IMPLEMENTADO');assert.equal(state.cases['CP-AUT-10'].status,'NO IMPLEMENTADO');
  assert.notEqual(state.cases['CP-AUT-12'].status,'APROBADO');assert.equal(state.ids.buyer,'7');assert.equal(state.ids.producer,'8');
});
test('OTP missing is a blocked precondition, never a passed verification',()=>{
  flow.env.authStage='execute';flow.run('06.1');flow.run('06.2');const result=flow.run('06.3');
  assert.equal(result.blocked,true);assert.equal(flow.state().cases['CP-AUT-06'].status,'BLOQUEADO');
});
test('execute phase preserves the CP-AUT-07 plan failure and blocks pending mailbox/expiration checkpoints',()=>{
  Object.assign(flow.env,{verificationOtp:'000123',authAttemptsOtp:'000456',recoveryOtp:'000789',authOtpConfirmedRunId:runId});
  flow.stage('execute');const state=flow.state();
  for(const id of ['06','08','09','12'])assert.equal(state.cases['CP-AUT-'+id].status,'APROBADO');
  assert.equal(state.cases['CP-AUT-07'].status,'FALLIDO');assert.equal(state.steps['07.6'].httpStatus,400);assert.equal(state.steps['07.7'].flowReady,true);
  assert.equal(state.cases['CP-AUT-11'].status,'BLOQUEADO');assert.equal(state.cases['CP-AUT-13'].status,'BLOQUEADO');
  assert.equal(flow.env.recoveryOtp,'');assert.equal(flow.env.authReplacementOtp,'');
});
test('resume does not repeat OTP attempts; replacement OTP and recovery postconditions finish their flows',()=>{
  Object.assign(flow.env,{authReplacementOtp:'001234',recoveryOtp:'002345',authOtpConfirmedRunId:runId});
  const attempts=Object.keys(flow.state().steps).filter(k=>['07.4','07.5','07.6'].includes(k)).length;
  flow.stage('execute');const state=flow.state();
  assert.equal(attempts,3);assert.equal(state.steps['07.12'].flowReady,true);
  assert.equal(state.cases['CP-AUT-07'].status,'FALLIDO');assert.equal(state.cases['CP-AUT-13'].status,'APROBADO');
  for(const key of ['12.8','12.9','12.10','12.11'])assert.equal(state.steps[key].status,'APROBADO');
});
test('expired JWT requires the exact previously accepted token and a natural wait',()=>{
  flow.env.authStage='execute';const original=flow.env.authExpiredCandidateJwt;
  flow.env.authExpiredCandidateJwt=flow.jwt('buyer');assert.equal(flow.run('11.2').blocked,true);
  flow.env.authExpiredCandidateJwt=original;assert.equal(flow.run('11.2').blocked,true);
  flow.clock+=(7200+301)*1000;assert.equal(flow.run('11.2').blocked,false);
  assert.equal(flow.state().cases['CP-AUT-11'].status,'APROBADO');assert.equal(flow.env.expiredJwt,original);
});
test('evidence is sanitized and no integration status is inferred from omitted steps',()=>{
  const evidence=JSON.parse(flow.env.authEvidence);
  for(const item of evidence)assert.deepEqual(Object.keys(item).sort(),['caseId','step','at','method','path','httpStatus','checks','status','reason'].sort());
  for(const key of environment.values.filter(v=>v.type==='secret').map(v=>v.key))if(flow.env[key])assert.equal(flow.env.authEvidence.includes(flow.env[key]),false);
  assert.equal(flow.state().cases['CP-AUT-05'].status,'NO IMPLEMENTADO');
  assert.equal(flow.state().cases['CP-AUT-10'].status,'NO IMPLEMENTADO');
});
test('all collection HTTP routes exist in controller attributes; Swagger is configured',()=>{
  const sourceRoot=path.join(root,'../backend/Tequio.Api');const routes=new Set();
  for(const file of fs.readdirSync(path.join(sourceRoot,'Controllers')).filter(name=>name.endsWith('.cs'))){
    const source=fs.readFileSync(path.join(sourceRoot,'Controllers',file),'utf8');
    const baseRoute=source.match(/\[Route\("([^"]+)"\)\]/)[1];
    for(const match of source.matchAll(/\[Http(Get|Post|Put|Patch)(?:\("([^"]*)"\))?\]/g))routes.add(match[1].toUpperCase()+' /'+baseRoute+(match[2]?'/'+match[2]:''));
  }
  for(const item of all){
    if(item.request.url.includes('/swagger/'))continue;
    const route=item.request.url.replace('{{baseUrl}}','').split('?')[0].replace('{{producerUserId}}','{producerId}').replace('{{categoryId}}','{categoryId}').replace('{{productId}}','{productId}');
    assert.ok(routes.has(item.request.method+' '+route));
  }
  assert.ok(fs.readFileSync(path.join(sourceRoot,'Program.cs'),'utf8').includes('app.UseSwagger()'));
});
test('verification fixtures generate deliberately different wrong codes, never a fabricated correct OTP',()=>{
  const runner=new OfflineRunner();runner.stage('prepare');runner.env.authStage='execute';
  runner.env.authAttemptsOtp='000456';runner.env.authOtpConfirmedRunId=runId;
  const result=runner.run('07.4');const body=JSON.parse(result.local.authRequestBody);
  assert.equal(body.verificationCode,'000457');assert.equal(runner.env.authAttemptsOtp,'000456');
  assert.equal(body.email,runner.env.buyerEmailTemplate.replace('{{runId}}',runId+'-otp').toLowerCase());
});
test('CP-AUT-13 expired mode waits naturally and requires the specific expiration response',()=>{
  const runner=new OfflineRunner();runner.stage('prepare');
  Object.assign(runner.env,{verificationOtp:'000123',authAttemptsOtp:'000456',recoveryOtp:'000789',authOtpConfirmedRunId:runId});runner.stage('execute');
  Object.assign(runner.env,{authRecoveryCodeMode:'expired',recoveryOtp:'004567'});
  assert.equal(runner.run('13.2').blocked,true);
  runner.clock+=16*60*1000;
  const result=runner.run('13.2',{status:400,data:{error:'El código de verificación ha expirado. Solicita uno nuevo.'}});
  assert.equal(result.blocked,false);assert.equal(JSON.parse(result.local.authRequestBody).code,'004567');
  for(const key of ['13.3','13.4','13.5'])runner.run(key);
  assert.equal(runner.state().cases['CP-AUT-13'].status,'APROBADO');
});
test('resuming cannot silently replace mailbox routing or the original fictitious credentials',()=>{
  for(const replacement of [{buyerPassword:'TEST_Changed123!'},{buyerEmailTemplate:'other_{{runId}}@example.test'},{newPassword:'TEST_ChangedNew123!'}]){
    const runner=new OfflineRunner();runner.run('01.1');Object.assign(runner.env,replacement);
    assert.equal(runner.run('01.2').blocked,true);assert.equal(runner.responses,1);
  }
});
process.stdout.write(JSON.stringify({kind:'OFFLINE_SCRIPT_VALIDATION_ONLY',offlineChecksPassed:checks,officialSchema,sdkImport,collectionRequests:all.length,realHttpRequests:0,sqlOperations:0})+'\n');
