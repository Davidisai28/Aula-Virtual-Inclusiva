// Prueba real explicita: una consulta y una descarga completa, sin guardar MP4.
const { createApp, createDrive } = require('../src/app');
async function main() {
  const folderId = process.env.GOOGLE_DRIVE_FOLDER_ID || '13AqU1QOzQg6dBn8fNnp9Dj7NElW1vUXT';
  const app = createApp({ drive: createDrive(), folderId });
  const server = app.listen(0, '127.0.0.1');
  await new Promise(resolve => server.once('listening', resolve));
  const url = 'http://127.0.0.1:' + server.address().port;
  const result = {};
  try {
    const health = await fetch(url + '/health'); result.health = health.status;
    const catalog = await fetch(url + '/api/videos', { signal: AbortSignal.timeout(45000) });
    result.catalog = catalog.status;
    if (catalog.status !== 200) throw Error('CATALOGO_NO_DISPONIBLE');
    const data = await catalog.json(); result.videos = data.total;
    const video = data.videos.filter(v => Number(v.size) > 0).sort((a,b) => Number(a.size)-Number(b.size))[0];
    if (!video) throw Error('SIN_VIDEOS');
    const response = await fetch(url + '/api/videos/' + encodeURIComponent(video.id) + '/download', { signal: AbortSignal.timeout(180000) });
    result.download = response.status; if (response.status !== 200) throw Error('DESCARGA_NO_DISPONIBLE');
    let bytes = 0, header = Buffer.alloc(0);
    for await (const part of response.body) { bytes += part.length; if (header.length < 32) header = Buffer.concat([header, part]).subarray(0, 32); }
    result.bytes = bytes; result.complete = bytes === Number(video.size); result.mp4Header = header.includes(Buffer.from('ftyp'));
    if (!result.complete || !result.mp4Header) throw Error('ARCHIVO_NO_VALIDADO');
    result.ok = true;
  } catch { result.ok = false; process.exitCode = 1; }
  finally { console.log(JSON.stringify(result)); app.locals.shutdown(); server.close(); server.closeAllConnections(); }
}
main().catch(() => { console.log(JSON.stringify({ ok: false, error: 'CONFIGURACION_O_CREDENCIALES_NO_DISPONIBLES' })); process.exitCode = 1; });
