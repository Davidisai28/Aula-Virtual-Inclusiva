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
