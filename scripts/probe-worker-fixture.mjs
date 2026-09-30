import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import path from "node:path";
import vm from "node:vm";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const source = readFileSync(path.join(root, "providers", "chatgpt", "extension", "service-worker.js"), "utf8");
let messageListener;
let nativeReply;
let outbound;
const port = {
  onMessage: { addListener(listener) { nativeReply = listener; } },
  onDisconnect: { addListener() {} },
  postMessage(message) { outbound = message; }
};
const chrome = {
  runtime: {
    onMessage: { addListener(listener) { messageListener = listener; } },
    connectNative() { return port; }
  },
  action: { onClicked: { addListener() {} }, setBadgeText() {} }
};
vm.runInNewContext(source, { chrome, TextEncoder, URL });

const id = "1845e62f-0ca5-4f39-b114-ec862d795029";
const sender = { tab: { id: 7 }, url: `https://chatgpt.com/c/${id}` };
const batch = {
  kind: "observation", conversationId: id, projectId: null,
  messages: [{ id, role: "unknown", ordinal: 0, body: "Texto ficticio privado" }],
  hiddenDom: 2, unexpected: "Texto ficticio privado"
};
let reply;
assert.equal(messageListener(batch, sender, value => { reply = value; }), true);
assert.deepEqual(Object.keys(outbound).sort(),
  ["conversationId", "kind", "messages", "projectId", "sequence", "tabId"].sort());
assert.deepEqual(Object.keys(outbound.messages[0]).sort(), ["id", "ordinal", "role"]);
assert.equal(JSON.stringify(outbound).includes("Texto ficticio privado"), false);
assert.equal(outbound.tabId, 7);
nativeReply({ ok: true, sequence: outbound.sequence, visible: 1 });
await new Promise(resolve => setImmediate(resolve));
assert.equal(reply.ok, true);

outbound = undefined;
assert.equal(messageListener(batch, { tab: { id: 7 }, url: `https://example.com/c/${id}` }, () => {}), undefined);
assert.equal(outbound, undefined);
process.stdout.write("Service worker fixture: origin, tab isolation, text stripping OK\n");
