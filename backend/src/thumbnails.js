"use strict";
const MAX_BYTES = 256 * 1024;
function thumbnailUrl(value) {
  const url = new URL(value);
  // El enlace procede exclusivamente de los metadatos de Drive, nunca del cliente.
  if (url.protocol !== "https:" || url.username || url.password || url.port ||
      !url.hostname.endsWith(".googleusercontent.com")) throw Error("Origen no permitido");
  return url.href;
}
function imageType(bytes) {
  if (bytes.length >= 3 && bytes[0] === 255 && bytes[1] === 216 && bytes[2] === 255) return "image/jpeg";
  if (bytes.length >= 8 && bytes.subarray(0, 8).equals(Buffer.from([137,80,78,71,13,10,26,10]))) return "image/png";
  throw Error("Imagen no compatible");
}
async function fetchThumbnail(auth, link, signal) {
  const url = thumbnailUrl(link);
  const token = await auth.getAccessToken();
  if (!token) throw Error("Autorizacion no disponible");
  const response = await fetch(url, { headers: { Authorization: "Bearer " + token }, redirect: "error", signal });
  if (!response.ok || Number(response.headers.get("content-length")) > MAX_BYTES) {
    await response.body?.cancel(); throw Error("Miniatura no disponible");
  }
  const chunks = []; let size = 0;
  for await (const chunk of response.body) {
    size += chunk.length;
    if (size > MAX_BYTES) throw Error("Miniatura demasiado grande");
    chunks.push(chunk);
  }
  const bytes = Buffer.concat(chunks);
  return { bytes, type: imageType(bytes) };
}
module.exports = { fetchThumbnail, thumbnailUrl, imageType, MAX_BYTES };
