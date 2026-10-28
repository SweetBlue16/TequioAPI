// Local harmless fixtures only. Does not upload files or access the API/database.
const fs=require('node:fs');
const path=require('node:path');
const crypto=require('node:crypto');
const zlib=require('node:zlib');
const LIMIT=5*1024*1024;
function crc32(buffer){let crc=0xffffffff;for(const byte of buffer){crc^=byte;for(let i=0;i<8;i++)crc=(crc>>>1)^((crc&1)?0xedb88320:0);}return (crc^0xffffffff)>>>0;}
function chunk(type,data){const tag=Buffer.from(type);const output=Buffer.alloc(data.length+12);output.writeUInt32BE(data.length);tag.copy(output,4);data.copy(output,8);output.writeUInt32BE(crc32(Buffer.concat([tag,data])),output.length-4);return output;}
function image(runId,target){
  const ihdr=Buffer.alloc(13);ihdr.writeUInt32BE(1,0);ihdr.writeUInt32BE(1,4);ihdr[8]=8;ihdr[9]=6;
  const parts=[Buffer.from('89504e470d0a1a0a','hex'),chunk('IHDR',ihdr),chunk('tEXt',Buffer.from('Description\0TEST_'+runId)),chunk('IDAT',zlib.deflateSync(Buffer.from([0,255,255,255,255])))];
  const length=parts.reduce((sum,b)=>sum+b.length,0)+12;
  if(target){if(target<length+12)throw new Error('Target too small');parts.push(chunk('npAD',Buffer.alloc(target-length-12)));}
  parts.push(chunk('IEND',Buffer.alloc(0)));return Buffer.concat(parts);
}
function fixtures(runId){
  if(!/^[a-f0-9]{32}$/.test(runId))throw new Error('Supply the current 32-character hexadecimal runId.');
  return {small:image(runId),exact:image(runId,LIMIT),over:image(runId,LIMIT+1),txt:Buffer.from('TEST_'+runId+' harmless plain text. No executable content.\n')};
}
function filename(runId,key){return 'TEST_'+runId+'_'+({small:'small.png',exact:'exact_5MiB.png',over:'over_5MiB.png',txt:'rejected.txt'}[key]);}
function digest(buffer){return crypto.createHash('sha256').update(buffer).digest('hex');}
function verifyPng(buffer){
  if(buffer.subarray(0,8).toString('hex')!=='89504e470d0a1a0a')throw new Error('Invalid PNG signature');
  let offset=8;const types=[];let pixels;
  while(offset<buffer.length){const size=buffer.readUInt32BE(offset),type=buffer.toString('ascii',offset+4,offset+8);const data=buffer.subarray(offset+8,offset+8+size);
    if(offset+12+size>buffer.length || buffer.readUInt32BE(offset+8+size)!==crc32(buffer.subarray(offset+4,offset+8+size)))throw new Error('Invalid PNG chunk');
    if(type==='IDAT')pixels=zlib.inflateSync(data);types.push(type);offset+=size+12;
  }
  if(types[0]!=='IHDR' || types.at(-1)!=='IEND' || !pixels || pixels.toString('hex')!=='00ffffffff')throw new Error('Unexpected fixture image');
  return true;
}
if(require.main===module){
  const [action,runId]=process.argv.slice(2);const buffers=fixtures(runId);const directory=path.resolve(__dirname,'.fixtures',runId);
  if(!directory.startsWith(path.resolve(__dirname,'.fixtures')+path.sep))throw new Error('Unsafe output path');
  const manifest={runId,files:{}};
  for(const [key,buffer] of Object.entries(buffers)){
    if(key!=='txt')verifyPng(buffer);
    manifest.files[key]={name:filename(runId,key),size:buffer.length,sha256:digest(buffer)};
  }
  if(action==='generate'){
    fs.mkdirSync(directory,{recursive:true});for(const [key,buffer] of Object.entries(buffers))fs.writeFileSync(path.join(directory,filename(runId,key)),buffer);
    fs.writeFileSync(path.join(directory,'manifest.json'),JSON.stringify(manifest,null,2)+'\n');
    process.stdout.write('Generated local fixtures in '+directory+'\n');
  }else if(action==='verify'){
    for(const [key,buffer] of Object.entries(buffers)){const actual=fs.readFileSync(path.join(directory,filename(runId,key)));if(actual.length!==buffer.length || digest(actual)!==digest(buffer))throw new Error('Fixture mismatch: '+key);}
    process.stdout.write('All local fixture sizes, contents and hashes match.\n');
  }else if(action!=='self-test')throw new Error('Usage: node postman/create-fixtures.cjs generate|verify|self-test RUN_ID');
  process.stdout.write(JSON.stringify(manifest)+'\n');
}
module.exports={LIMIT,fixtures,filename,digest,verifyPng};
