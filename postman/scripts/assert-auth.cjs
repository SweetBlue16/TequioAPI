// This function supplies the corresponding standalone Postman event.
module.exports = function assertAuth({ pm, Date, require }) {
{
  // Record only actual HTTP responses. Never include a body, email, password, OTP or JWT in evidence.
  const authEnv=pm.environment;
  const authMeta=JSON.parse(pm.variables.get('authStepMeta'));
  const authState=JSON.parse(authEnv.get('_authState'));
  const record=authState.steps[authMeta.key];
  const persona=authMeta.persona, action=authMeta.action;
  const code=pm.response.code;
  const checks=[];
  function check(label,condition) {
    const passed=Boolean(condition); checks.push({name:label,passed});
    pm.test(authMeta.id+' / '+authMeta.key+' — '+label,function(){pm.expect(passed,'Contract not met; sensitive values omitted.').to.equal(true);});
    return passed;
  }
  function save() {
    authEnv.set('_authState',JSON.stringify(authState));
    authEnv.set('authResults',JSON.stringify({runId:authState.runId,cases:authState.cases}));
    authEnv.set('authEvidence',JSON.stringify(Object.values(authState.steps).filter(step=>step.done).map(step=>({caseId:step.caseId,step:step.key,at:step.at,method:step.method,path:step.path,httpStatus:step.httpStatus,checks:step.checks,status:step.status,reason:step.reason||''}))));
  }
  let data=null;
  try { data=pm.response.json(); } catch (_) { /* A JWT challenge may have an empty body. */ }
  const obj=data && typeof data==='object' && !Array.isArray(data);
  const smtp=code===503 && obj && data.error==='No fue posible enviar el correo en este momento.';
  const expiredOtp=code===400 && obj && data.error==='El código de verificación ha expirado. Solicita uno nuevo.' && ['verify','wrongOtp','originalOtp','reset'].includes(action);
  record.done=true;record.httpStatus=code;record.checks=checks;record.at=new Date().toISOString();
  if (smtp || expiredOtp) {
    check('Dependency failure HTTP status is recorded; this is not a passed scenario',code===(smtp?503:400));
    record.status='BLOQUEADO';record.reason=smtp?'BLOQUEADO POR SMTP; el registro puede haber persistido':'OTP vencido: precondición de vigencia no satisfecha';record.flowReady=false;
    if(authState.cases[authMeta.id].status!=='FALLIDO') authState.cases[authMeta.id]={status:'BLOQUEADO',reason:record.reason};
    pm.test.skip(authMeta.id+' — '+record.reason,function(){});
    save();
  } else {
    const secretKeys=/^(password|passwordHash|newPassword|verificationCode|verificationOtp|recoveryOtp|otp|code|secret|apiKey|apiSecret|jwt|accessToken|refreshToken)$/i;
    const secrets=['buyerPassword','producerPassword','newPassword','verificationOtp','recoveryOtp','authAttemptsOtp','authReplacementOtp','jwt','expiredJwt','authBuyerJwt','authProducerJwt','authAttemptsJwt','authRecoveryJwt','authExpiredCandidateJwt'].map(key=>String(authEnv.get(key)||'')).filter(Boolean);
    function leaked(value,path) {
      if(Array.isArray(value)) return value.some((v,i)=>leaked(v,path+'.'+i));
      if(value && typeof value==='object') return Object.keys(value).some(key=>{
        if(key==='token' && path==='$' && action==='login' && code===200) return false;
        return secretKeys.test(key) || key.toLowerCase()==='token' || leaked(value[key],path+'.'+key);
      });
      return typeof value==='string' && secrets.some(secret=>value===secret || (secret.length>=8 && value.includes(secret)));
    }
    check('HTTP status matches the plan',authMeta.statuses.includes(code));
    const jsonRequired=action!=='expired';
    check('Response format is valid',!jsonRequired || (obj && /application\/(?:[a-z.+-]*\+)?json/i.test(pm.response.headers.get('Content-Type')||'')));
    const safe=check('No passwords, OTP or unintended tokens are exposed',obj?!leaked(data,'$'):!jsonRequired && !secrets.some(secret=>pm.response.text().includes(secret)));
    const expectedRole=persona==='producer'?2:1;
    const identity=value=>Number.isSafeInteger(Number(value)) && Number(value)>0;
    let semantic=false;
    if(['register','resend','recover','verify','reset'].includes(action)) {
      semantic=check('Success includes only a confirmation contract',code===authMeta.statuses[0] && obj && typeof data.mensaje==='string' && !Object.hasOwn(data,'token'));
    }
    if(action==='register' && semantic && safe) authState.issued[persona]=Date.now();
    if(['recover','resend'].includes(action) && semantic && safe) authState.issued[persona]=Date.now();
    if(action==='reset' && semantic && safe) authState.passwordChanged=true;
    if(action==='duplicate') semantic=check('Duplicate registration is rejected without replacing credentials', [400,409].includes(code) && obj && data.error==='El correo electrónico ya se encuentra registrado.');
    if(action==='minor') semantic=check('Underage registration is rejected for the actual age restriction',code===400 && obj && data.error==='Debes ser mayor de 18 años para registrarte.');
    if(['badLogin','unknownLogin','oldLogin','alternateLogin'].includes(action)) semantic=check('Invalid credentials return the same generic 401 and no token',code===401 && obj && data.error==='El correo o la contraseña son incorrectos.' && !Object.hasOwn(data,'token'));
    if(['wrongOtp','originalOtp','reuseOtp','invalidRecovery'].includes(action)) {
      const expired=action==='invalidRecovery' && authEnv.get('authRecoveryCodeMode')==='expired';
      const message=expired?'El código de verificación ha expirado. Solicita uno nuevo.':'El código de verificación es incorrecto.';
      semantic=check('The intended code is rejected for the correct reason', [400,403,422].includes(code) && obj && data.error===message);
    }
    if(action==='login') {
      let claims=null;
      try { if(typeof data.token!=='string' || data.token.split('.').length!==3) throw new Error(); claims=JSON.parse(require('buffer').Buffer.from(data.token.split('.')[1].replace(/-/g,'+').replace(/_/g,'/'),'base64').toString('utf8')); } catch (_) { claims=null; }
      semantic=check('Login issues a live JWT identifying the expected user role',code===200 && obj && typeof data.mensaje==='string' && claims && identity(claims.sub) && Number(claims.roleId)===expectedRole && Number.isFinite(claims.exp) && claims.exp*1000>Date.now());
      if(semantic && safe) {
        const previous=authState.ids[persona];
        const same=check('Login keeps the previously observed API identity',!previous || String(previous)===String(claims.sub));
        semantic=semantic && same;
        if(same) {
          const key={buyer:'authBuyerJwt',producer:'authProducerJwt',attempts:'authAttemptsJwt',recovery:'authRecoveryJwt'}[persona];
          authEnv.set(key,data.token);authState.claims[persona]={sub:String(claims.sub),roleId:Number(claims.roleId),exp:claims.exp};
          if(authMeta.captureCandidate) {
            authEnv.set('authExpiredCandidateJwt',data.token);
            authState.candidate={digest:require('crypto-js').SHA256(data.token).toString(),runId:authState.runId,origin:authState.origin,exp:claims.exp,acceptedAt:null};
            authEnv.set('jwt',data.token);authEnv.set('_stateRunId',authState.runId);authEnv.set('_jwtRunId',authState.runId);authEnv.set('_jwtAccount','buyer');authEnv.set('_jwtUserId',String(claims.sub));authEnv.set('_jwtRoleId','1');
          }
        }
      }
    }
    if(['profile','attest'].includes(action)) {
      const claims=authState.claims[persona];
      semantic=check('Protected API profile matches the real login identity and fictitious run data',code===200 && obj && identity(data.id) && claims && String(data.id)===String(claims.sub) && data.email===pm.variables.get('authExpectedEmail') && data.firstName===pm.variables.get('authExpectedName') && data.paternalLastName==='TEST_Apellido');
      if(typeof authMeta.verified==='boolean') semantic=check('Profile verification state matches the required postcondition',obj && data.isVerified===authMeta.verified) && semantic;
      if(semantic && safe) {
        const previous=authState.ids[persona];
        semantic=check('The persisted user ID remains unchanged',!previous || String(previous)===String(data.id)) && semantic;
        if(semantic) {
          authState.ids[persona]=String(data.id);authState.verified[persona]=data.isVerified;
          if(['buyer','producer'].includes(persona)) authEnv.set(persona+'UserId',String(data.id));
          if(action==='attest' && authState.candidate) authState.candidate.acceptedAt=new Date().toISOString();
        }
      }
    }
    if(action==='expired') semantic=check('A previously accepted unchanged JWT is rejected after natural expiration',code===401 && Boolean(authState.candidate && authState.candidate.acceptedAt));
    const passed=checks.every(check=>check.passed);
    record.status=passed?'APROBADO':'FALLIDO';
    record.flowReady=Boolean(semantic && safe && (authMeta.statuses.includes(code) || (authMeta.planLockout && code===400)));
    if(!passed) authState.cases[authMeta.id]={status:'FALLIDO',reason:authMeta.planLockout && code===400?'La API respondió 400; el plan exige 403/422 al agotar intentos':'Una o más aserciones no se cumplieron; consultar authEvidence'};
    const keys=JSON.parse(pm.variables.get('authCaseSteps'));
    const complete=keys.every(key=>authState.steps[key] && authState.steps[key].done && authState.steps[key].status==='APROBADO');
    if(complete) authState.cases[authMeta.id]={status:'APROBADO',reason:'Todas las solicitudes y postcondiciones del caso se ejecutaron con respuestas HTTP reales'};
    else if(authState.cases[authMeta.id].status!=='FALLIDO') authState.cases[authMeta.id]={status:'BLOQUEADO',reason:'Flujo incompleto; faltan pasos, OTP o espera natural. Ningún paso aislado aprueba el caso'};
    if(complete && authMeta.id==='CP-AUT-06') authEnv.set('verificationOtp','');
    if(authMeta.key==='12.11' && record.flowReady) authEnv.set('recoveryOtp','');
    if(complete && authMeta.id==='CP-AUT-13') authEnv.set('recoveryOtp','');
    save();
  }

}
}
