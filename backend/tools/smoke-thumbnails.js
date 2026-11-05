"use strict";
// Prueba explícita con credenciales locales por variable de entorno; no imprime sus valores.
const { createApp, createDrive } = require("../src/app");
const { createHash } = require("node:crypto");
let etapa = "inicio";
async function main() {
  const folderId = process.env.GOOGLE_DRIVE_FOLDER_ID || "13AqU1QOzQg6dBn8fNnp9Dj7NElW1vUXT";
  etapa = "inicializar cliente Drive";
  const drive = createDrive();
  etapa = "inicializar servidor local de prueba";
  const app = createApp({ drive, folderId });
  const server = app.listen(0, "127.0.0.1");
  await new Promise(resolve => server.once("listening", resolve));
  try {
    const root = "http://127.0.0.1:" + server.address().port;
    etapa = "consulta del catalogo";
    const response = await fetch(root + "/api/videos");
    if (response.status !== 200) throw Error("Catalogo: HTTP " + response.status);
    const catalog = await response.json();
    const results = [];
    for (const video of catalog.videos.slice(0, 2)) {
      if (!video.thumbnailPath) { results.push({ disponible: false }); continue; }
      etapa = "descarga de miniatura";
      const image = await fetch(root + video.thumbnailPath);
      const bytes = Buffer.from(await image.arrayBuffer());
      if (image.status !== 200) throw Error("Miniatura: HTTP " + image.status);
      results.push({ disponible: true, http: image.status, tipo: image.headers.get("content-type"),
        bytes: bytes.length, sha256: createHash("sha256").update(bytes).digest("hex") });
    }
    console.log(JSON.stringify({ catalogoHTTP: response.status, videos: catalog.total, miniaturas: results }, null, 2));
  } finally { app.locals.shutdown(); server.closeAllConnections(); await new Promise(resolve => server.close(resolve)); }
}
main().catch(error => {
  // Solo errores propios y códigos numéricos, nunca mensajes/URL de Google.
  const http = /^Catalogo: HTTP \d+$|^Miniatura: HTTP \d+$/.test(error.message) ? error.message : "Sin respuesta valida";
  console.error("Prueba fallida en " + etapa + ": " + http + " (" + error.name + ")"); process.exitCode = 1;
});
