const { test } = require('node:test');
const assert = require('node:assert/strict');
const { Readable } = require('node:stream');
const { createApp } = require('../src/app');
const folderId = 'carpeta_prueba_123';
const id = 'video_prueba_123';
const body = Buffer.from('video-de-prueba');
const video = { id, name: 'Clase.mp4', mimeType: 'video/mp4', size: String(body.length), parents: [folderId], modifiedTime: '2026-01-01T00:00:00Z' };
async function server(t, drive, options = {}) {
  const app = createApp({ folderId, drive, ...options });
  const s = app.listen(0, '127.0.0.1');
  await new Promise(resolve => s.once('listening', resolve));
  t.after(() => new Promise(resolve => { app.locals.shutdown(); s.close(resolve); s.closeAllConnections(); }));
  return 'http://127.0.0.1:' + s.address().port;
}
function fake(overrides = {}) {
  return { files: {
    list: async () => ({ data: { files: [video] } }),
    get: async args => args.alt ? { data: Readable.from([body]) } : { data: video }, ...overrides
  } };
}
test('health sin consultar Drive', async t => {
  const url = await server(t, fake({ list: () => { throw Error('No debe llamarse'); } }));
  assert.deepEqual(await (await fetch(url + '/health')).json(), { status: 'ok' });
});

const jpeg = Buffer.from([255, 216, 255, 224, 0, 1]);
test('catalogo expone ruta relativa, nunca el enlace privado de Drive', async t => {
  const url = await server(t, fake({ list: async () => ({ data: { files: [{ ...video, hasThumbnail: true, thumbnailLink: 'enlace-privado' }] } }) }));
  const r = await (await fetch(url + '/api/videos')).json();
  assert.equal(r.videos[0].thumbnailPath, `/api/videos/${id}/thumbnail`);
  assert.ok(!JSON.stringify(r).includes('enlace-privado'));
});
test('miniatura verifica carpeta, limita memoria y reutiliza imagen sin saltarse permisos', async t => {
  let transfers = 0, checks = 0;
  const drive = fake({ get: async () => { checks++; return { data: { ...video, thumbnailLink: 'privado' } }; } });
  drive.fetchThumbnail = async () => { transfers++; return { bytes: jpeg }; };
  const url = await server(t, drive);
  for (let i = 0; i < 2; i++) {
    const r = await fetch(`${url}/api/videos/${id}/thumbnail`);
    assert.equal(r.status, 200); assert.equal(r.headers.get('content-type'), 'image/jpeg');
    assert.deepEqual(Buffer.from(await r.arrayBuffer()), jpeg);
  }
  assert.equal(transfers, 1); assert.equal(checks, 2);
  assert.equal((await fetch(url + '/api/videos/otro_video_123/thumbnail')).status, 404);
  assert.equal(checks, 2);
});
test('miniaturas ausentes o denegadas no rompen el catalogo', async t => {
  const absent = await server(t, fake());
  assert.equal((await fetch(`${absent}/api/videos/${id}/thumbnail`)).status, 404);
  assert.equal((await fetch(absent + '/api/videos')).status, 200);
  const drive = fake({ get: async () => ({ data: { ...video, parents: ['otra_carpeta_123'], thumbnailLink: 'privado' } }) });
  drive.fetchThumbnail = async () => { throw Error('No debe pedirse'); };
  const denied = await server(t, drive);
  assert.equal((await fetch(`${denied}/api/videos/${id}/thumbnail`)).status, 403);
});
test('rechaza imagenes grandes o contenido que no sea PNG/JPEG sin filtrar errores', async t => {
  for (const bytes of [Buffer.alloc(256*1024+1), Buffer.from('<html>SECRETO_PRUEBA</html>')]) {
    const drive = fake({ get: async () => ({ data: { ...video, thumbnailLink: 'privado' } }) });
    drive.fetchThumbnail = async () => ({ bytes });
    const url = await server(t, drive); const r = await fetch(`${url}/api/videos/${id}/thumbnail`);
    assert.equal(r.status, 502); assert.ok(!(await r.text()).includes('SECRETO_PRUEBA'));
  }
});
test('proxy solo admite HTTPS googleusercontent y no acepta credenciales ni destinos arbitrarios', () => {
  const { thumbnailUrl } = require('../src/thumbnails');
  assert.equal(thumbnailUrl('https://lh3.googleusercontent.com/test'), 'https://lh3.googleusercontent.com/test');
  for (const value of ['http://lh3.googleusercontent.com/test','https://localhost/test',
    'https://lh3.googleusercontent.com.evil.invalid/test','https://user:pass@lh3.googleusercontent.com/test'])
    assert.throws(() => thumbnailUrl(value));
});
test('la cache de miniaturas no evita comprobar permisos revocados ni el presupuesto', async t => {
  let denied = false;
  const drive = fake({ get: async () => ({ data: { ...video, thumbnailLink: 'privado', capabilities: { canDownload: !denied } } }) });
  drive.fetchThumbnail = async () => ({ bytes: jpeg });
  const url = await server(t, drive);
  assert.equal((await fetch(`${url}/api/videos/${id}/thumbnail`)).status, 200);
  denied = true;
  assert.equal((await fetch(`${url}/api/videos/${id}/thumbnail`)).status, 403);
  denied = false;
  const limited = await server(t, drive, { hourlyBytes: 1 });
  assert.equal((await fetch(`${limited}/api/videos/${id}/thumbnail`)).status, 429);
});
test('catalogo pagina, conserva formato y comparte consultas simultaneas', async t => {
  let calls = 0;
  const url = await server(t, fake({ list: async args => {
    calls++; await new Promise(resolve => setTimeout(resolve, 10));
    return { data: args.pageToken ? { files: [{ ...video, id: 'video_segundo_123' }] } : { files: [video], nextPageToken: 'pagina2' } };
  } }));
  const [a, b] = await Promise.all([fetch(url + '/api/videos').then(r => r.json()), fetch(url + '/api/videos').then(r => r.json())]);
  assert.equal(a.total, 2); assert.deepEqual(a, b); assert.equal(calls, 2);
  await fetch(url + '/api/videos'); assert.equal(calls, 2);
});
test('descarga completa con longitud y tipo correctos', async t => {
  const url = await server(t, fake()); const r = await fetch(url + '/api/videos/' + id + '/download');
  assert.equal(r.status, 200); assert.equal(r.headers.get('content-type'), 'video/mp4');
  assert.equal(r.headers.get('content-length'), video.size); assert.deepEqual(Buffer.from(await r.arrayBuffer()), body);
});
test('rechaza identificadores y archivos fuera de carpeta antes de pedir media', async t => {
  let media = 0; const url = await server(t, fake({ get: async args => {
    if (args.alt) media++; return { data: { ...video, parents: ['otra_carpeta_123'] } };
  } }));
  assert.equal((await fetch(url + '/api/videos/x/download')).status, 400);
  assert.equal((await fetch(url + '/api/videos/' + id + '/download')).status, 403); assert.equal(media, 0);
});
test('no expone errores privados de Google', async t => {
  const url = await server(t, fake({ list: async () => { throw Error('SECRETO_PRUEBA'); } }));
  const r = await fetch(url + '/api/videos'); assert.equal(r.status, 503); assert.ok(!(await r.text()).includes('SECRETO_PRUEBA'));
});
test('un ID ajeno al catalogo no consulta siquiera sus metadatos', async t => {
  let metadata = 0; const url = await server(t, fake({ get: async () => { metadata++; return { data: video }; } }));
  assert.equal((await fetch(url + '/api/videos/video_ajeno_123/download')).status, 404);
  assert.equal(metadata, 0);
});
test('limita peticiones sin bloquear health', async t => {
  const url = await server(t, fake(), { maxRequests: 1 });
  await fetch(url + '/api/videos'); assert.equal((await fetch(url + '/api/videos')).status, 429);
  assert.equal((await fetch(url + '/health')).status, 200);
});
test('presupuesto reserva antes de transferir', async t => {
  let media = 0; const url = await server(t, fake({ get: async args => {
    if (args.alt) { media++; return { data: Readable.from([body]) }; } return { data: video };
  } }), { hourlyBytes: 1 });
  assert.equal((await fetch(url + '/api/videos/' + id + '/download')).status, 429); assert.equal(media, 0);
});
test('corte de stream no se presenta como archivo completo', async t => {
  const url = await server(t, fake({ get: async args => args.alt ? { data: Readable.from([body.subarray(0, 3)]) } : { data: video } }));
  await assert.rejects(async () => { const r = await fetch(url + '/api/videos/' + id + '/download'); await r.arrayBuffer(); });
});
test('cancelacion del cliente cancela la transferencia upstream', async t => {
  let cancelled; const signalSeen = new Promise(resolve => { cancelled = resolve; });
  const url = await server(t, fake({ get: async (args, options) => {
    if (!args.alt) return { data: { ...video, size: '1000000' } };
    options.signal.addEventListener('abort', cancelled, { once: true });
    const stream = new Readable({ read() {} }); stream.push(Buffer.alloc(1024)); return { data: stream };
  } }));
  const controller = new AbortController(); const r = await fetch(url + '/api/videos/' + id + '/download', { signal: controller.signal });
  const reader = r.body.getReader(); await reader.read(); controller.abort();
  await Promise.race([signalSeen, new Promise((resolve, reject) => { const timer = setTimeout(() => reject(Error('No cancelo upstream')), 1500); timer.unref(); })]);
});
