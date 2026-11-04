"use strict";
const express = require("express");
const { pipeline } = require("node:stream/promises");
const { Transform } = require("node:stream");
const ID = /^[A-Za-z0-9_-]{10,200}$/;
function createDrive() {
  const { google } = require("googleapis");
  const options = { scopes: ["https://www.googleapis.com/auth/drive.readonly"] };
  if (process.env.GOOGLE_SERVICE_ACCOUNT_JSON) {
    const credentials = JSON.parse(process.env.GOOGLE_SERVICE_ACCOUNT_JSON);
    if (credentials.type !== "service_account" || !credentials.client_email || !credentials.private_key) throw new Error("Credenciales invalidas");
    options.credentials = credentials;
  }
  return google.drive({ version: "v3", auth: new google.auth.GoogleAuth(options) });
}
function createApp({ drive, folderId, behindProxy = false, maxRequests = 30, maxDownloads = 2,
  maxVideoBytes = 512 * 1024 * 1024, hourlyBytes = 256 * 1024 * 1024 }) {
  if (!ID.test(folderId)) throw new Error("Carpeta invalida");
  const app = express(); app.disable("x-powered-by");
  if (behindProxy) app.set("trust proxy", 1);
  const clients = new Map(), downloads = new Map(), controllers = new Set();
  let cache = null, cacheUntil = 0, pending = null, active = 0, spent = 0, reset = Date.now() + 3600000;
  app.locals.shutdown = () => { for (const controller of controllers) controller.abort(); };
  app.use((req, res, next) => { res.set("X-Content-Type-Options", "nosniff"); res.set("Cache-Control", "no-store"); next(); });
  app.get("/health", (req, res) => res.json({ status: "ok" }));
  app.use("/api", (req, res, next) => {
    const now = Date.now(), key = req.ip;
    for (const [ip, entry] of clients) if (entry.until <= now) clients.delete(ip);
    let entry = clients.get(key);
    if (!entry) {
      if (clients.size >= 4096) return res.status(503).json({ error: "Servidor ocupado" });
      entry = { count: 0, until: now + 60000 }; clients.set(key, entry);
    }
    if (++entry.count > maxRequests) { res.set("Retry-After", "60"); return res.status(429).json({ error: "Demasiadas solicitudes; reintenta mas tarde" }); }
    next();
  });
  async function loadCatalog() {
    if (cache && Date.now() < cacheUntil) return cache;
    if (pending) return pending;
    pending = (async () => {
      const videos = [], ids = new Set(), pages = new Set(); let pageToken;
      do {
        const response = await drive.files.list({ q: `'${folderId}' in parents and trashed = false and mimeType = 'video/mp4'`,
          fields: "nextPageToken,files(id,name,mimeType,size,modifiedTime)", pageSize: 100, pageToken }, { timeout: 30000, retry: false });
        for (const v of response.data.files || []) {
          if (!ID.test(v.id) || ids.has(v.id)) continue;
          ids.add(v.id); videos.push({ id: v.id, name: v.name, mimeType: v.mimeType, size: v.size, modifiedTime: v.modifiedTime });
        }
        pageToken = response.data.nextPageToken;
        if (videos.length > 10000 || pages.size >= 100 || (pageToken && pages.has(pageToken))) throw new Error("Catalogo excedido");
        if (pageToken) pages.add(pageToken);
      } while (pageToken);
      cache = { total: videos.length, videos }; cacheUntil = Date.now() + 60000; return cache;
    })();
    try { return await pending; } finally { pending = null; }
  }
  app.get("/api/videos", async (req, res) => {
    try { res.json(await loadCatalog()); }
    catch { res.status(503).json({ error: "No se pudo consultar la biblioteca; reintenta mas tarde" }); }
  });
  app.get("/api/videos/:id/download", async (req, res) => {
    const fileId = req.params.id, key = req.ip;
    if (!ID.test(fileId)) return res.status(400).json({ error: "Identificador de video invalido" });
    if (active >= maxDownloads || downloads.has(key)) { res.set("Retry-After", "30"); return res.status(429).json({ error: "Descarga en curso; reintenta mas tarde" }); }
    active++; downloads.set(key, true);
    const controller = new AbortController(); controllers.add(controller);
    const abort = () => { if (!res.writableFinished) controller.abort(); };
    req.once("aborted", abort); res.once("close", abort);
    try {
      const catalog = await loadCatalog();
      if (!catalog.videos.some(v => v.id === fileId)) return res.status(404).json({ error: "Video no disponible en la biblioteca" });
      const metadata = await drive.files.get({ fileId, fields: "id,name,mimeType,size,parents,trashed,capabilities(canDownload)" }, { signal: controller.signal, timeout: 30000, retry: false });
      const video = metadata.data;
      if (video.trashed || video.mimeType !== "video/mp4" || !video.parents?.includes(folderId) || video.capabilities?.canDownload === false) return res.status(403).json({ error: "Video no autorizado" });
      const size = Number(video.size);
      if (!Number.isSafeInteger(size) || size <= 0 || size > maxVideoBytes) return res.status(413).json({ error: "Tamano de video no permitido" });
      if (Date.now() >= reset) { spent = 0; reset = Date.now() + 3600000; }
      // Reserve before transfer. Failed/cancelled transfers consume the budget too.
      if (spent + size > hourlyBytes) { res.set("Retry-After", String(Math.ceil((reset - Date.now()) / 1000))); return res.status(429).json({ error: "Limite temporal de transferencia alcanzado" }); }
      spent += size;
      const response = await drive.files.get({ fileId, alt: "media" }, { responseType: "stream", signal: controller.signal, timeout: 300000, retry: false });
      res.set("Content-Type", "video/mp4"); res.set("Content-Length", String(size));
      res.set("Content-Disposition", "attachment; filename*=UTF-8''" + encodeURIComponent(video.name || "video.mp4").replace(/['()*]/g, c => "%" + c.charCodeAt(0).toString(16)));
      res.set("Accept-Ranges", "none");
      let bytes = 0;
      const verify = new Transform({ transform(chunk, encoding, callback) {
        bytes += chunk.length; callback(bytes > size ? new Error("Tamano excedido") : null, chunk);
      }, flush(callback) { callback(bytes === size ? null : new Error("Transferencia incompleta")); } });
      await pipeline(response.data, verify, res, { signal: controller.signal });
    } catch (error) {
      if (controller.signal.aborted) { if (!res.destroyed) res.destroy(); }
      else if (!res.headersSent && !res.destroyed) {
        const code = Number(error?.response?.status || error?.code);
        res.status(code === 404 ? 404 : code === 403 ? 403 : 502).json({ error: "No se pudo descargar el video" });
      } else if (!res.destroyed) res.destroy();
    } finally {
      controller.abort(); controllers.delete(controller); active--; downloads.delete(key);
      req.off("aborted", abort); res.off("close", abort);
    }
  });
  app.use((req, res) => res.status(404).json({ error: "Ruta no encontrada" }));
  app.use((err, req, res, next) => { if (res.headersSent) return next(err); res.status(500).json({ error: "Error interno" }); });
  return app;
}
module.exports = { createApp, createDrive };
