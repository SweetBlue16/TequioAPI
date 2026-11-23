// This function supplies the corresponding standalone Postman event.
module.exports = function prepareCollection({ pm, Date, require }) {
if (!/^(CP-AUT-|CP-PER-|CP-MUL-|CP-CAT-)/.test(pm.info.requestName)) {
// Shared safety and data preparation. No HTTP calls or SQL are made by scripts.
const definitions = {
  "Read development OpenAPI document": {
    "method": "GET",
    "route": "/swagger/v1/swagger.json",
    "status": 200,
    "op": "diagnostics",
    "protectedRoute": false
  },
  "Register active test account": {
    "method": "POST",
    "route": "/api/v1/auth/register",
    "status": 201,
    "op": "register",
    "protectedRoute": false
  },
  "Log in active test account": {
    "method": "POST",
    "route": "/api/v1/auth/login",
    "status": 200,
    "op": "login",
    "protectedRoute": false
  },
  "Verify account with mailbox OTP": {
    "method": "POST",
    "route": "/api/v1/auth/verify",
    "status": 200,
    "op": "verify",
    "protectedRoute": false
  },
  "Resend account verification OTP": {
    "method": "POST",
    "route": "/api/v1/auth/resend-verification-code",
    "status": 200,
    "op": "resend",
    "protectedRoute": false
  },
  "Request password recovery OTP": {
    "method": "POST",
    "route": "/api/v1/auth/recover",
    "status": 200,
    "op": "recover",
    "protectedRoute": false
  },
  "Reset password with mailbox OTP": {
    "method": "POST",
    "route": "/api/v1/auth/reset-password",
    "status": 200,
    "op": "reset",
    "protectedRoute": false
  },
  "Read my test profile and capture real user ID": {
    "method": "GET",
    "route": "/api/v1/users/me",
    "status": 200,
    "op": "profile",
    "protectedRoute": true
  },
  "Update my test profile": {
    "method": "PUT",
    "route": "/api/v1/users/me",
    "status": 200,
    "op": "updateProfile",
    "protectedRoute": true
  },
  "Associate uploaded test profile picture": {
    "method": "PUT",
    "route": "/api/v1/users/me/profile-picture",
    "status": 200,
    "op": "picture",
    "protectedRoute": true
  },
  "Promote my test buyer to producer - explicit confirmation": {
    "method": "PATCH",
    "route": "/api/v1/users/me/role/producer",
    "status": 200,
    "op": "promote",
    "protectedRoute": true
  },
  "Upload a local test image": {
    "method": "POST",
    "route": "/api/v1/files/image",
    "status": 200,
    "op": "upload",
    "protectedRoute": true
  },
  "Create a test base product": {
    "method": "POST",
    "route": "/api/v1/products",
    "status": 201,
    "op": "createProduct",
    "protectedRoute": true
  },
  "Read active products from test producer": {
    "method": "GET",
    "route": "/api/v1/products/producers/{{producerUserId}}?pageIndex={{pageIndex}}&pageSize={{pageSize}}",
    "status": 200,
    "op": "producerProducts",
    "protectedRoute": false
  },
  "Read active products by category": {
    "method": "GET",
    "route": "/api/v1/products/category/{{categoryId}}?pageIndex={{pageIndex}}&pageSize={{pageSize}}",
    "status": 200,
    "op": "categoryProducts",
    "protectedRoute": false
  },
  "Deactivate created test product - explicit confirmation": {
    "method": "PATCH",
    "route": "/api/v1/products/{{productId}}/deactive",
    "status": 200,
    "op": "deactivate",
    "protectedRoute": true
  }
};
const env = pm.environment;
function set(key, value) { env.set(key, String(value)); pm.variables.set(key, String(value)); }
function get(key) { return String(env.get(key) || '').trim(); }
function block(reason) {
  pm.test('Safety gate: ' + reason, function () { pm.expect(false, 'Request blocked; see postman/README.md.').to.equal(true); });
  pm.execution.skipRequest();
  throw new Error('TEQUIO request blocked.');
}
function required(key) { if (!get(key)) block('Configure ' + key); return get(key); }
function positive(key) { if (!/^[1-9][0-9]*$/.test(required(key))) block('Invalid ' + key); return Number(get(key)); }
// Environment values take precedence over global, iteration and collection variables.
["baseUrl","authorizedDevelopmentUrl","developmentEnvironmentConfirmed","allowMutatingTests","runId","activeAccount","buyerEmail","producerEmail","buyerEmailTemplate","producerEmailTemplate","buyerPassword","producerPassword","newPassword","buyerUserId","producerUserId","buyerRoleId","producerRoleId","jwt","expiredJwt","verificationOtp","recoveryOtp","testInboxAddress","testInboxConfirmed","emailRoutingConfirmed","pageIndex","pageSize","categoryId","uploadedImageUrl","productId","confirmPromotionRunId","confirmDeactivationRunId","_stateRunId","_jwtRunId","_jwtAccount","_jwtUserId","_jwtRoleId","_profileRunId","_profileAccount","_profileUserId","_uploadRunId","_uploadAccount","_uploadUserId","_productRunId","_createdProductId","_productOwnerId","_productObservedRunId","_buyerRegisteredRunId","_producerRegisteredRunId","_buyerPromotedRunId"].forEach(key => pm.variables.set(key, env.get(key) || ''));
const meta = definitions[pm.info.requestName];
if (!meta || pm.request.method !== meta.method) block('Unknown or modified request');
const base = required('baseUrl');
if (!/^https?:\/\/(?:[a-zA-Z0-9][a-zA-Z0-9.-]*|\[[a-fA-F0-9:]+\])(?::[0-9]{1,5})?$/.test(base)) block('baseUrl must be an origin without path, credentials or trailing slash');
if (get('developmentEnvironmentConfirmed') !== 'true' || required('authorizedDevelopmentUrl') !== base) block('Confirm the exact authorized development URL');
if (meta.method !== 'GET' && get('allowMutatingTests') !== 'true') block('allowMutatingTests must be true');
let runId = get('runId');
if (!runId) { runId = pm.variables.replaceIn('{{$guid}}').replace(/-/g, '').toLowerCase(); set('runId', runId); }
if (!/^[a-f0-9]{32}$/.test(runId)) block('runId must contain 32 lowercase hexadecimal characters');
if (get('_stateRunId') !== runId) {
  ["buyerEmail","producerEmail","buyerUserId","producerUserId","jwt","expiredJwt","verificationOtp","recoveryOtp","uploadedImageUrl","productId","confirmPromotionRunId","confirmDeactivationRunId","_stateRunId","_jwtRunId","_jwtAccount","_jwtUserId","_jwtRoleId","_profileRunId","_profileAccount","_profileUserId","_uploadRunId","_uploadAccount","_uploadUserId","_productRunId","_createdProductId","_productOwnerId","_productObservedRunId","_buyerRegisteredRunId","_producerRegisteredRunId","_buyerPromotedRunId"].forEach(key => set(key, ''));
  set('_stateRunId', runId);
}
pm.variables.set('operation', meta.op);
pm.variables.set('expectedStatus', meta.status);
const actor = get('activeAccount');
const accountOperation = !['diagnostics', 'categoryProducts'].includes(meta.op);
const emailPattern = /^[A-Za-z0-9_+.-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$/;
if (accountOperation) {
  if (!['buyer', 'producer'].includes(actor)) block('activeAccount must be buyer or producer');
  if (get('buyerRoleId') !== '1' || get('producerRoleId') !== '2') block('Role values must match the repository seed');
  if (get('testInboxConfirmed') !== 'true' || get('emailRoutingConfirmed') !== 'true') block('Confirm ownership and routing of test mail');
  if (!emailPattern.test(required('testInboxAddress'))) block('Invalid testInboxAddress');
  ['buyer', 'producer'].forEach(account => {
    const template = required(account + 'EmailTemplate');
    if (!template.includes('{{runId}}')) block('Email templates must include {{runId}}');
    const email = template.split('{{runId}}').join(runId).toLowerCase();
    if (!emailPattern.test(email) || email.length > 80 || !email.includes(runId)) block('Invalid generated test email');
    set(account + 'Email', email);
  });
  if (get('buyerEmail') === get('producerEmail')) block('Test account addresses must differ');
}
const firstName = 'TEST_' + runId + '_' + actor;
const productName = 'TEST_' + runId;
const profile = { firstName, paternalLastName: 'TEST_Apellido', maternalLastName: null,
  birthDate: '1990-01-02T00:00:00Z', phoneNumber: null, locality: 'TEST_Localidad', biography: 'TEST_' + runId };
let body;
if (['register', 'login'].includes(meta.op)) {
  const password = required(actor + 'Password');
  if (password.length < 8 || password.length > 64) block('Configure a fictitious password of 8 to 64 characters');
  body = { email: get(actor + 'Email'), password: env.get(actor + 'Password') };
  if (meta.op === 'register') {
    if (get('_' + actor + 'RegisteredRunId') === runId) block('This test account was already registered in this run');
    Object.assign(body, { firstName, paternalLastName: 'TEST_Apellido', maternalLastName: null,
      birthDate: profile.birthDate, phoneNumber: null, roleId: Number(get(actor + 'RoleId')) });
  } else {
    ['jwt', '_jwtRunId', '_jwtAccount', '_jwtUserId', '_jwtRoleId', '_profileRunId', '_profileAccount', '_profileUserId'].forEach(key => set(key, ''));
  }
}
if (['verify', 'resend', 'recover', 'reset'].includes(meta.op)) {
  body = { email: get(actor + 'Email') };
  if (meta.op === 'verify' || meta.op === 'reset') {
    const otp = required(meta.op === 'verify' ? 'verificationOtp' : 'recoveryOtp');
    if (!/^[0-9]{6}$/.test(otp)) block('Supply the six-digit code from the authorized test inbox');
    if (meta.op === 'verify') body.verificationCode = otp;
    else {
      const password = required('newPassword');
      if (password.length < 8 || password.length > 64) block('newPassword must contain 8 to 64 characters');
      body.code = otp; body.newPassword = env.get('newPassword');
    }
  }
}
if (meta.protectedRoute) {
  required('jwt');
  if (get('_jwtRunId') !== runId || get('_jwtAccount') !== actor || !get('_jwtUserId')) block('Log in this run and account using this collection');
  let claims;
  try { claims = JSON.parse(require('buffer').Buffer.from(get('jwt').split('.')[1].replace(/-/g, '+').replace(/_/g, '/'), 'base64').toString('utf8')); } catch (_) { claims = null; }
  if (!claims || String(claims.sub) !== get('_jwtUserId') || String(claims.roleId) !== get('_jwtRoleId') || !Number.isFinite(claims.exp) || claims.exp * 1000 <= Date.now()) block('The current JWT must match the captured test identity and be unexpired');
  if (meta.op === 'profile') ['_profileRunId', '_profileAccount', '_profileUserId'].forEach(key => set(key, ''));
  if (meta.op !== 'profile' && (get('_profileRunId') !== runId || get('_profileAccount') !== actor || get('_profileUserId') !== get('_jwtUserId'))) block('Read and validate the test profile before modifying data');
}
if (meta.op === 'updateProfile') body = profile;
if (['picture', 'createProduct'].includes(meta.op)) {
  if (get('_uploadRunId') !== runId || get('_uploadAccount') !== actor || get('_uploadUserId') !== get('_jwtUserId')) block('Upload a test image with the current account first');
  required('uploadedImageUrl');
}
if (meta.op === 'picture') body = { pictureUrl: get('uploadedImageUrl') };
if (meta.op === 'upload') {
  const file = pm.request.body.formdata.all().find(item => item.key === 'file' && item.type === 'file');
  if (!file || !file.src || (Array.isArray(file.src) && !file.src.length)) block('Select a local fictitious image in Body > form-data > file');
}
if (['createProduct', 'deactivate'].includes(meta.op)) {
  if (actor !== 'producer' || get('_jwtRoleId') !== '2' || get('_jwtUserId') !== get('producerUserId')) block('Use the test producer and read its profile');
}
if (meta.op === 'createProduct') {
  if (get('_productRunId') === runId) block('A sample product has already been created for this run');
  body = { categoryId: positive('categoryId'), name: productName, shortDescription: 'TEST_Producto ficticio ' + runId,
    measurementUnit: 'kg', imageUrl: get('uploadedImageUrl') };
}
if (['producerProducts', 'categoryProducts'].includes(meta.op)) {
  const index = positive('pageIndex'), size = positive('pageSize');
  if (index > 21474736 || size < 10 || size > 100) block('Pagination requires pageIndex >= 1 and pageSize 10..100');
  positive(meta.op === 'producerProducts' ? 'producerUserId' : 'categoryId');
}
if (meta.op === 'promote') {
  if (actor !== 'buyer' || get('_jwtRoleId') !== '1' || get('confirmPromotionRunId') !== runId) block('Explicit confirmation is required to promote this test buyer');
  set('confirmPromotionRunId', '');
}
if (meta.op === 'deactivate') {
  positive('productId');
  if (get('productId') !== get('_createdProductId') || get('_productRunId') !== runId || get('_productOwnerId') !== get('_jwtUserId') || get('_productObservedRunId') !== runId) block('Only the product created and observed through the API in this run can be deactivated');
  if (get('confirmDeactivationRunId') !== runId) block('Explicit confirmation is required to deactivate this test product');
  set('confirmDeactivationRunId', '');
}
if (body) pm.variables.set('requestBody', JSON.stringify(body));
const expectedUrl = pm.variables.replaceIn(base + meta.route);
if (pm.variables.replaceIn(pm.request.url.toString()) !== expectedUrl) block('The request URL differs from the audited route');
// No automatic setNextRequest, sendRequest, inbox access or cleanup.
}
};
