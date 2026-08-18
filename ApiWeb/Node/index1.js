const express = require("express");
const multer = require("multer");
const cors = require("cors");
const QRCode = require("qrcode");
const pino = require("pino");

const upload = multer({
  storage: multer.memoryStorage(),
  limits: { fileSize: 8 * 1024 * 1024 } // 8MB (ajusta)
});

const {
  default: makeWASocket,
  useMultiFileAuthState,
  DisconnectReason,
  fetchLatestBaileysVersion,
} = require("@whiskeysockets/baileys");

const app = express();
app.use(cors());
app.use(express.json({ limit: "1mb" }));

let sock = null;
let stateInfo = {
  connected: false,
  state: "stopped", // stopped | connecting | qr | open | close | error
  me: null,
  lastQrText: null,
  lastQrPngBase64: null,
  lastQrAt: null,
  lastError: null,
};
let me = null;
function formatMe() {
  // me puede venir como { id: '5917xxxx@s.whatsapp.net', ... }
  const id = me?.id || me?.jid || "";
  return id ? id.split("@")[0] : null;
}
function normalizeJid(to) {
  const digits = String(to || "").replace(/\D/g, "");
  return digits.includes("@s.whatsapp.net") ? digits : `${digits}@s.whatsapp.net`;
}
function normalizePhone(input) {
  return (input || "").replace(/[^\d]/g, "");
}

async function setQr(qrText) {
  stateInfo.lastQrText = qrText;
  stateInfo.lastQrAt = new Date().toISOString();
  const dataUrl = await QRCode.toDataURL(qrText, { margin: 1, scale: 6 });
  stateInfo.lastQrPngBase64 = dataUrl.split(",")[1];
}

async function startWhatsApp() {
  if (sock) return;

  stateInfo.state = "connecting";
  stateInfo.connected = false;
  stateInfo.lastError = null;

  const { state, saveCreds } = await useMultiFileAuthState("./auth_info");
  const { version } = await fetchLatestBaileysVersion();

  sock = makeWASocket({
    version,
    auth: state,
    printQRInTerminal: true,
    logger: pino({ level: "silent" }),
  });

  sock.ev.on("creds.update", saveCreds);

  sock.ev.on("connection.update", async (update) => {
    const { connection, lastDisconnect, qr } = update;

    if (qr) {
      stateInfo.state = "qr";
      stateInfo.connected = false;
      await setQr(qr);
    }

    if (connection === "open") {
      stateInfo.state = "open";
      stateInfo.connected = true;
      stateInfo.me = sock.user || null;
      stateInfo.lastQrText = null;
      stateInfo.lastQrPngBase64 = null;
      stateInfo.lastQrAt = null;
	   me = sock.user || update?.user || sock?.authState?.creds?.me || null;
    }

    if (connection === "close") {
      stateInfo.state = "close";
      stateInfo.connected = false;

      const statusCode = lastDisconnect?.error?.output?.statusCode;
      const reason =
        statusCode === DisconnectReason.loggedOut
          ? "loggedOut"
          : statusCode || "unknown";

      stateInfo.lastError = `Connection closed. Reason: ${reason}`;

      if (statusCode !== DisconnectReason.loggedOut) {
        sock = null;
        setTimeout(() => startWhatsApp().catch(() => {}), 2000);
      } else {
        sock = null;
      }
    }
  });
}

app.get("/status", (req, res) => res.json({ ok: true,me: formatMe(), ...stateInfo }));

app.post("/start", async (req, res) => {
  try {
    await startWhatsApp();
    res.json({ ok: true });
  } catch (e) {
    stateInfo.state = "error";
    stateInfo.lastError = String(e?.message || e);
    res.status(500).json({ ok: false, error: stateInfo.lastError });
  }
});

app.get("/qr", (req, res) => {
  res.json({
    ok: true,
    available: stateInfo.state === "qr" && !!stateInfo.lastQrPngBase64,
    qrPngBase64: stateInfo.lastQrPngBase64,
    qrAt: stateInfo.lastQrAt,
  });
});

// app.post("/send", async (req, res) => {
  // try {
    // if (!sock || !stateInfo.connected) {
      // return res.status(400).json({ ok: false, error: "Not connected" });
    // }

    // const to = normalizePhone(req.body?.to);
    // const message = String(req.body?.message || "").trim();
    // if (!to) return res.status(400).json({ ok: false, error: "Invalid 'to'" });
    // if (!message) return res.status(400).json({ ok: false, error: "Empty message" });

    // const jid = `${to}@s.whatsapp.net`;
    // const r = await sock.sendMessage(jid, { text: message });
    // res.json({ ok: true, jid, result: r?.key || null });
  // } catch (e) {
    // res.status(500).json({ ok: false, error: String(e?.message || e) });
  // }
// });
app.post("/send", async (req, res) => {
  try {
    if (!sock || !stateInfo.connected) {
      return res.status(400).json({ ok: false, error: "Not connected" });
    }

    const toRaw = String(req.body?.to || "").trim();
    const message = String(req.body?.message || "").trim();

    if (!toRaw) return res.status(400).json({ ok: false, error: "Invalid 'to'" });
    if (!message) return res.status(400).json({ ok: false, error: "Empty message" });

    const jid = normalizeJid(toRaw);  // ✅ MISMA lógica que imagen
    const r = await sock.sendMessage(jid, { text: message });

    res.json({ ok: true, jid, result: r?.key || null });
  } catch (e) {
    res.status(500).json({ ok: false, error: String(e?.message || e) });
  }
});

app.post("/reset", async (req, res) => {
  try {
    await stopWhatsApp();
    if (fs.existsSync("./auth_info")) {
      fs.rmSync("./auth_info", { recursive: true, force: true });
    }
    res.json({ ok: true });
  } catch (e) {
    res.status(500).json({ ok: false, error: String(e?.message || e) });
  }
});
app.post("/send-image", upload.single("image"), async (req, res) => {
  try {
    if (!sock) return res.status(400).json({ error: "Bridge no iniciado" });

    const to = req.body.to;
    const caption = req.body.caption || "";
    if (!to) return res.status(400).json({ error: "to requerido" });
    if (!req.file) return res.status(400).json({ error: "image requerida" });

    const jid = normalizeJid(to);

    await sock.sendMessage(jid, {
      image: req.file.buffer,
      caption: caption
    });

    return res.json({ ok: true });
  } catch (e) {
    return res.status(500).json({ error: String(e?.message || e) });
  }
});
const PORT = 3001;
app.listen(PORT, () => console.log(`Bridge OK: http://127.0.0.1:${PORT}`));
