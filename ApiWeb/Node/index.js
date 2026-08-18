const express = require("express");
const multer = require("multer");
const cors = require("cors");
const QRCode = require("qrcode");
const pino = require("pino");
const fs = require("fs");
const path = require("path");

const upload = multer({
    storage: multer.memoryStorage(),
    limits: { fileSize: 8 * 1024 * 1024 } // 8MB
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

// -------------------- Helpers --------------------
function normalizeJid(to) {
    const digits = String(to || "").replace(/\D/g, "");
    return digits.includes("@s.whatsapp.net") ? digits : `${digits}@s.whatsapp.net`;
}

async function qrToBase64(qrText) {
    const dataUrl = await QRCode.toDataURL(qrText, { margin: 1, scale: 6 });
    return dataUrl.split(",")[1];
}

function emptyState() {
    return {
        connected: false,
        state: "stopped", // stopped | connecting | qr | open | close | error
        me: null,
        lastQrText: null,
        lastQrPngBase64: null,
        lastQrAt: null,
        lastError: null,
    };
}

// -------------------- Multi-session store --------------------
/**
 * sessions[id] = {
 *   sock,
 *   stateInfo,
 *   saveCreds,
 *   authPath
 * }
 */
const sessions = new Map();

function getSession(id) {
    const sid = (id || "default").trim();
    if (!sid) return null;
    return sessions.get(sid) || null;
}

function ensureSession(id) {
    const sid = (id || "default").trim();
    if (!sid) throw new Error("sessionId requerido");
    let s = sessions.get(sid);
    if (!s) {
        const authPath = path.join(__dirname, "auth_info", sid);
        s = {
            sock: null,
            stateInfo: emptyState(),
            saveCreds: null,
            authPath
        };
        sessions.set(sid, s);
    }
    return { sid, s };
}

async function startSession(sessionId) {
    const { sid, s } = ensureSession(sessionId);

    if (s.sock) return; // ya existe

    s.stateInfo.state = "connecting";
    s.stateInfo.connected = false;
    s.stateInfo.lastError = null;

    // Auth por sesión
    const { state, saveCreds } = await useMultiFileAuthState(s.authPath);
    s.saveCreds = saveCreds;

    const { version } = await fetchLatestBaileysVersion();

    const sock = makeWASocket({
        version,
        auth: state,
        printQRInTerminal: true,
        logger: pino({ level: "silent" }),
    });

    s.sock = sock;

    sock.ev.on("creds.update", saveCreds);

    sock.ev.on("connection.update", async (update) => {
        const { connection, lastDisconnect, qr } = update;

        if (qr) {
            s.stateInfo.state = "qr";
            s.stateInfo.connected = false;
            s.stateInfo.lastQrText = qr;
            s.stateInfo.lastQrAt = new Date().toISOString();
            s.stateInfo.lastQrPngBase64 = await qrToBase64(qr);
        }

        if (connection === "open") {
            s.stateInfo.state = "open";
            s.stateInfo.connected = true;
            s.stateInfo.me = sock.user || null;
            s.stateInfo.lastQrText = null;
            s.stateInfo.lastQrPngBase64 = null;
            s.stateInfo.lastQrAt = null;
        }

        if (connection === "close") {
            s.stateInfo.state = "close";
            s.stateInfo.connected = false;

            const statusCode = lastDisconnect?.error?.output?.statusCode;
            const reason =
                statusCode === DisconnectReason.loggedOut
                    ? "loggedOut"
                    : statusCode || "unknown";

            s.stateInfo.lastError = `Connection closed. Reason: ${reason}`;

            // Si NO se deslogueó, reintenta
            if (statusCode !== DisconnectReason.loggedOut) {
                s.sock = null;
                setTimeout(() => startSession(sid).catch(() => { }), 2000);
            } else {
                s.sock = null;
            }
        }
    });
}

async function stopSession(sessionId) {
    const { sid, s } = ensureSession(sessionId);

    try {
        if (s.sock) {
            // best effort cerrar
            try { await s.sock.logout(); } catch { }
            try { s.sock.end?.(); } catch { }
        }
    } catch { }
    s.sock = null;
    s.stateInfo = emptyState();
}

async function resetSession(sessionId) {
    const { sid, s } = ensureSession(sessionId);

    await stopSession(sid);

    // borrar auth
    try {
        if (fs.existsSync(s.authPath)) {
            fs.rmSync(s.authPath, { recursive: true, force: true });
        }
    } catch { }

    // volver a iniciar
    await startSession(sid);
}

// -------------------- Routes (multi-session) --------------------
app.get("/sessions", (req, res) => {
    res.json({ ok: true, sessions: Array.from(sessions.keys()) });
});

app.post("/session/:id/start", async (req, res) => {
    try {
        await startSession(req.params.id);
        res.json({ ok: true });
    } catch (e) {
        res.status(500).json({ ok: false, error: String(e?.message || e) });
    }
});

app.post("/session/:id/stop", async (req, res) => {
    try {
        await stopSession(req.params.id);
        res.json({ ok: true });
    } catch (e) {
        res.status(500).json({ ok: false, error: String(e?.message || e) });
    }
});

app.post("/session/:id/reset", async (req, res) => {
    try {
        await resetSession(req.params.id);
        res.json({ ok: true });
    } catch (e) {
        res.status(500).json({ ok: false, error: String(e?.message || e) });
    }
});

app.get("/session/:id/status", (req, res) => {
    const { s } = ensureSession(req.params.id);
    const me = s.stateInfo?.me?.id || s.stateInfo?.me?.jid || null;
    const mePhone = me ? String(me).split("@")[0] : null;

    res.json({ ok: true, me: mePhone, ...s.stateInfo });
});

app.get("/session/:id/qr", (req, res) => {
    const { s } = ensureSession(req.params.id);
    res.json({
        ok: true,
        available: s.stateInfo.state === "qr" && !!s.stateInfo.lastQrPngBase64,
        qrPngBase64: s.stateInfo.lastQrPngBase64,
        qrAt: s.stateInfo.lastQrAt,
    });
});

app.post("/session/:id/send", async (req, res) => {
    try {
        const { s } = ensureSession(req.params.id);

        if (!s.sock || !s.stateInfo.connected) {
            return res.status(400).json({ ok: false, error: "Not connected" });
        }

        const toRaw = String(req.body?.to || "").trim();
        const message = String(req.body?.message || "").trim();
        if (!toRaw) return res.status(400).json({ ok: false, error: "Invalid 'to'" });
        if (!message) return res.status(400).json({ ok: false, error: "Empty message" });

        const jid = normalizeJid(toRaw);
        const r = await s.sock.sendMessage(jid, { text: message });

        res.json({ ok: true, jid, result: r?.key || null });
    } catch (e) {
        res.status(500).json({ ok: false, error: String(e?.message || e) });
    }
});

app.post("/session/:id/send-image", upload.single("image"), async (req, res) => {
    try {
        const { s } = ensureSession(req.params.id);

        if (!s.sock || !s.stateInfo.connected) {
            return res.status(400).json({ ok: false, error: "Not connected" });
        }

        const to = String(req.body?.to || "").trim();
        const caption = String(req.body?.caption || "");
        if (!to) return res.status(400).json({ ok: false, error: "to requerido" });
        if (!req.file) return res.status(400).json({ ok: false, error: "image requerida" });

        const jid = normalizeJid(to);

        await s.sock.sendMessage(jid, {
            image: req.file.buffer,
            caption
        });

        res.json({ ok: true });
    } catch (e) {
        res.status(500).json({ ok: false, error: String(e?.message || e) });
    }
});

// -------------------- Backward compat (default session) --------------------
app.post("/start", (req, res) => app._router.handle({ ...req, url: "/session/default/start" }, res, () => { }));
app.post("/stop", (req, res) => app._router.handle({ ...req, url: "/session/default/stop" }, res, () => { }));
app.post("/reset", (req, res) => app._router.handle({ ...req, url: "/session/default/reset" }, res, () => { }));
app.get("/status", (req, res) => app._router.handle({ ...req, url: "/session/default/status" }, res, () => { }));
app.get("/qr", (req, res) => app._router.handle({ ...req, url: "/session/default/qr" }, res, () => { }));
app.post("/send", (req, res) => app._router.handle({ ...req, url: "/session/default/send" }, res, () => { }));
app.post("/send-image", (req, res) => app._router.handle({ ...req, url: "/session/default/send-image" }, res, () => { }));

const PORT = 3001;
app.listen(PORT, () => console.log(`Bridge OK: http://127.0.0.1:${PORT}`));
