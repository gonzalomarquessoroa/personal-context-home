import assert from "node:assert/strict";
import { spawn } from "node:child_process";
import { fileURLToPath } from "node:url";
import path from "node:path";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const executable = path.join(root, ".tools", "native-host", "publish", "PersonalContext.NativeHost.exe");
const child = spawn(executable, [], { stdio: ["pipe", "pipe", "pipe"] });
child.on("error", error => { throw error; });
child.stderr.on("data", chunk => process.stderr.write(chunk));
let buffer = Buffer.alloc(0);
const pending = [];
child.stdout.on("data", chunk => {
  buffer = Buffer.concat([buffer, chunk]);
  while (buffer.length >= 4 && buffer.length >= 4 + buffer.readUInt32LE(0)) {
    const length = buffer.readUInt32LE(0);
    const reply = JSON.parse(buffer.subarray(4, 4 + length).toString("utf8"));
    buffer = buffer.subarray(4 + length);
    pending.shift()?.(reply);
  }
});

function send(message) {
  const payload = Buffer.from(JSON.stringify(message), "utf8");
  const prefix = Buffer.alloc(4);
  prefix.writeUInt32LE(payload.length);
  return new Promise(resolve => {
    pending.push(resolve);
    child.stdin.write(Buffer.concat([prefix, payload]));
  });
}

const conversationId = "1845e62f-0ca5-4f39-b114-ec862d795029";
const userId = "23349274-516a-447e-9d60-4396966d160f";
const assistantId = "bd129339-a388-4c7b-9e66-6ca0548366cb";
const editedId = "167264a3-c53b-456b-8863-bbfbead249e1";
const regeneratedId = "634ec162-a137-4522-974d-f1b3a46d5b4c";
const observation = (sequence, ids, projectId = null, tabId = 1) => ({
  kind: "observation", sequence, tabId, conversationId, projectId,
  messages: ids.map((id, ordinal) => ({
    id, role: ordinal % 2 ? "assistant" : "user", ordinal
  }))
});

try {
  assert.equal((await send({ kind: "ping" })).kind, "pong");
  const first = await send(observation(1, [userId, assistantId]));
  assert.deepEqual([first.added, first.noLongerVisible], [2, 0]);
  const retry = await send(observation(2, [userId, assistantId]));
  assert.deepEqual([retry.added, retry.noLongerVisible], [0, 0]);
  const edit = await send(observation(3, [editedId, assistantId]));
  assert.deepEqual([edit.added, edit.noLongerVisible], [1, 1]);
  const regenerate = await send(observation(4, [editedId, regeneratedId],
    "g-p-25a978a6c58a4b639b86ae93331677e9"));
  assert.deepEqual([regenerate.added, regenerate.noLongerVisible], [1, 1]);
  assert.equal(regenerate.projectObserved, true);
  const secondTab = await send(observation(5, [userId, assistantId], null, 2));
  assert.deepEqual([secondTab.added, secondTab.noLongerVisible], [2, 0]);
  const unknown = observation(6, [editedId, regeneratedId]);
  unknown.messages[0].role = "unknown";
  unknown.messages[1].role = "unknown";
  const unknownReply = await send(unknown);
  assert.equal(unknownReply.ok, true);
  assert.deepEqual([unknownReply.added, unknownReply.noLongerVisible], [0, 0]);
  process.stdout.write("Native Messaging smoke: ping, retry, edit, regenerate, project, separate tabs OK\n");
} finally {
  child.stdin.end();
}
