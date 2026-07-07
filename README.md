# SIP Softphone — Provider Setup & Testing Guide

This app includes a browser-based softphone (WebRTC over SIP) that registers
to any standard SIP provider and handles inbound/outbound voice calls.

The softphone lives at the **/softphone** route inside the app.

---

## 1. What you need from your SIP provider

To register, you need five things:

| Field | Description | Example |
|---|---|---|
| **SIP URI** | Your SIP address | `sip:alice@sip.example.com` |
| **Auth ID** | Auth username (often the same as the SIP user) | `alice` |
| **Password** | SIP password | `••••••••` |
| **WebSocket URL** | Provider's secure SIP-over-WebSocket endpoint | `wss://sip.example.com:7443` |
| **Display name** *(optional)* | Name shown to the callee | `Alice Smith` |

> **The WebSocket URL must use `wss://` (TLS).** Plain `ws://` is rejected by
> the app — modern browsers will not let a secure page open insecure
> WebSockets, and SIP credentials should never travel in clear text.

### Where to find these on common providers

- **Twilio Programmable Voice (SIP)** — Create a SIP Domain → use the domain
  for the URI host. Twilio supports SIP-over-WSS at
  `wss://<your-domain>.sip.twilio.com`. The Auth ID/password come from the
  domain's Credential List.
- **Telnyx** — Create a Credential connection → use
  `wss://sip.telnyx.com:443` (or your region equivalent). The username and
  password from the connection map to Auth ID and Password.
- **FreePBX / Asterisk** — Enable the WebSocket transport in
  `pjsip.conf` (or `http.conf` + `res_http_websocket`). Use TLS on port
  `8089` by default → `wss://your-pbx.example.com:8089/ws`. Each
  extension's secret is the SIP password.
- **3CX** — Web Client extensions expose a WSS URL in the welcome email,
  typically `wss://<fqdn>:5001/ws`.

If your provider gives you a port other than 443, include it in the URL
(`wss://host:7443`).

### STUN / TURN

A public STUN server is preconfigured (`stun:stun.l.google.com:19302`) and
works for the vast majority of networks.

If your callers sit behind strict NATs (corporate firewalls, mobile
carriers, double-NAT), add a TURN server. Most SIP providers offer one;
otherwise use a service like Twilio Network Traversal Service or self-host
[coturn](https://github.com/coturn/coturn). The Account tab accepts a TURN
URL plus username/password.

---

## 2. Configure the softphone

1. Sign in to the app and open **Softphone → Settings → Account**.
2. Fill in the fields above.
3. Click **Register**.
4. Watch the status pill in the top-right of the softphone:
   - 🟡 **Connecting** — opening the WebSocket and sending the SIP REGISTER.
   - 🟢 **Registered** — you are online and reachable.
   - 🔴 **Failed** — the toast shows the reason (bad credentials, wrong
     WSS URL, network blocked, etc.). The full error is also in
     **Settings → Debug**.

### Where credentials are stored

| Field | Stored in |
|---|---|
| Display name, SIP URI, Auth ID, WebSocket URL, STUN/TURN | Your account database row, protected by row-level security so only you can read it. |
| **Password** | **Browser `localStorage` only.** Never sent to or stored on the server. You will need to re-enter it on a new device or after clearing site data. |

This is intentional — SIP passwords grant outbound calling rights, so we
keep them off the server entirely.

---

## 3. Audio setup

Open **Settings → Audio** to:

- Pick your **microphone**.
- Pick your **speaker** (Chromium browsers only — Firefox/Safari use the
  system default).
- Pick a separate **ringtone output** (useful for headsets where you want
  the ringtone on speakers but the call in the headset).
- Toggle echo cancellation, noise suppression, and auto gain.
- Preview ringtones.

The first time you open this tab the browser will prompt for microphone
permission. If you decline, calls will fail at answer/dial time with a
clear toast — re-grant permission via the address-bar lock icon.

---

## 4. Testing end-to-end

### A. No SIP provider yet — Demo mode

You can exercise the entire UI without provisioning anything:

1. **Settings → Account** → toggle **Demo mode** on.
2. Click **Simulate register** — the status pill goes 🟢.
3. Click **Simulate inbound call** — the full-screen incoming-call modal
   appears. Test:
   - **Space** answers, **Esc** rejects.
   - Mute/Hold/Keypad/Transfer buttons in the active-call view.
   - **M** toggles mute, **Esc** hangs up.
4. Place a fake outbound call from the Dialer — it auto-connects after
   ~2 s and appears in **Recents** when you hang up.

Demo mode only fakes the SIP signalling; the microphone is still acquired
so you can verify the input level meter.

### B. Outbound call to a real number

1. Register with real credentials (Demo mode off).
2. On the Dialer, type the destination — either:
   - A number (`+15551234567`) — it will be dialed against your SIP
     domain, e.g. `sip:+15551234567@sip.example.com`.
   - A full SIP URI (`sip:bob@otherdomain.com`).
3. Click the green call button. You should hear ringback and then the
   remote party.
4. Use the in-call controls:
   - **Mute** silences your microphone (the icon changes and a "Muted"
     badge appears under the timer).
   - **Hold** plays hold music if your provider supports it.
   - **Keypad** sends DTMF tones (RFC 4733).
   - **Transfer** does a blind transfer (SIP REFER).
5. Hang up. The call appears in **Recents** with duration and is
   persisted to your account so it survives reload.

### C. Inbound call to your extension

1. Stay registered.
2. From any other phone, dial your SIP URI / DID.
3. The full-screen incoming-call modal appears with caller ID. The
   ringtone plays on your selected ringtone device. If the tab is in the
   background and you have granted notification permission, a browser
   notification also fires.
4. Press **Space** or click **Answer** — audio is established.
5. Hang up from either side; the call lands in **Recents**.

### D. Call quality

While a call is connected, the active-call view shows four signal bars
next to the duration. Hover them for tooltip detail:

- **RTT** — round-trip time in milliseconds (target < 150 ms).
- **Jitter** — under 30 ms is good.
- **Packet loss** — under 1.5 % is good.

Bars drop to amber/red when these thresholds are exceeded; if you
consistently see < 4 bars, switch networks or add a TURN server.

---

## 5. Troubleshooting

| Symptom | Likely cause / fix |
|---|---|
| Status stays 🟡 **Connecting** forever | The WebSocket can't reach the provider. Check the URL (`wss://`, correct port) and that your network allows outbound TLS to that port. |
| 🔴 **Failed** with "401 Unauthorized" | Auth ID or password wrong. Some providers require the *Auth ID* to differ from the SIP user — check the provider's docs. |
| Inbound calls never arrive | You're registered but the provider isn't routing the DID to your registration. Verify the DID's SIP routing on the provider side. |
| You can't hear the remote party | TURN is needed (strict NAT). Add one in **Settings → Account**. |
| The remote party can't hear you | Microphone permission denied or wrong mic selected. Check **Settings → Audio**. |
| Mic permission keeps prompting | The site must be served over HTTPS. The Lovable preview already is; on a custom domain make sure your TLS cert is valid. |
| Speaker selector is grayed out | You're on Firefox or Safari — those browsers don't expose `setSinkId`. The system-default output is used instead. |

For deeper diagnosis, open **Settings → Debug** to see a live log of
SIP and WebSocket events. The log never includes your password.

---

## 6. Security notes

- All registration and signalling traffic uses `wss://` (TLS).
- Media (RTP/DTLS-SRTP) is encrypted end-to-end by WebRTC.
- The SIP password is only stored in your browser's `localStorage` and
  is never sent to our servers, never written to logs, and never included
  in the Debug log.
- All call logs and SIP account rows are protected by row-level security
  scoped to your user — no other user (including admins on other
  tenants) can read them.

---

## Tech stack

React 18 · Vite · TypeScript · Tailwind · shadcn/ui · JsSIP · WebRTC ·
Lovable Cloud (managed Postgres + Auth).
