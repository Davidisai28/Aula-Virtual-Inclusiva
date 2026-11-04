"use strict";
const { createApp, createDrive } = require("./src/app");
function start() {
  const production = process.env.NODE_ENV === "production";
  if (production && process.env.ALLOW_PUBLIC_CATALOG !== "true") throw new Error("Publicacion bloqueada: aprobar autenticacion o autorizar ALLOW_PUBLIC_CATALOG=true para material publico.");
  const folderId = process.env.GOOGLE_DRIVE_FOLDER_ID || (!production && "13AqU1QOzQg6dBn8fNnp9Dj7NElW1vUXT");
  if (!folderId || !/^[A-Za-z0-9_-]{10,200}$/.test(folderId)) throw new Error("Configura GOOGLE_DRIVE_FOLDER_ID.");
  const port = Number(process.env.PORT || 3000);
  if (!Number.isInteger(port) || port < 1 || port > 65535) throw new Error("PORT invalido.");
  const app = createApp({ drive: createDrive(), folderId, behindProxy: Boolean(process.env.RENDER) });
  const server = app.listen(port, "0.0.0.0", () => console.log("[AULA-BACKEND] Puerto: " + port));
  server.requestTimeout = 600000; server.headersTimeout = 30000;
  const stop = () => {
    app.locals.shutdown(); server.close(() => process.exit(0)); server.closeIdleConnections();
    setTimeout(() => { server.closeAllConnections(); process.exit(1); }, 10000).unref();
  };
  process.once("SIGTERM", stop); process.once("SIGINT", stop); return server;
}
if (require.main === module) {
  try { start(); } catch { console.error("[AULA-BACKEND] Inicio bloqueado: revisa configuracion, credenciales y autorizacion de publicacion. No se registran valores privados."); process.exitCode = 1; }
}
module.exports = { start };
