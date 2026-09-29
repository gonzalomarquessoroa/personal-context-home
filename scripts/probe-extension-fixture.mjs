import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import path from "node:path";
import vm from "node:vm";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const source = readFileSync(path.join(root, "providers", "chatgpt", "extension", "content.js"), "utf8");
const conversation = "1845e62f-0ca5-4f39-b114-ec862d795029";
const project = "25a978a6-c58a-4b63-9b86-ae93331677e9";
const user = "23349274-516a-447e-9d60-4396966d160f";
const assistant = "bd129339-a388-4c7b-9e66-6ca0548366cb";
const continuationUser = "65c82821-a1b9-4e23-a38c-15130bde721e";
const continuationAssistant = "76c82701-3bb9-43e8-a8d7-b9c66b6a64a0";
const editedUser = "167264a3-c53b-456b-8863-bbfbead249e1";
const regeneratedAssistant = "634ec162-a137-4522-974d-f1b3a46d5b4c";

let nodes = [];
let projectHref = null;
let observer;
let nextTimer = 0;
const timers = new Map();
const sent = [];
const panel = { style: {}, textContent: "", contains: () => false, remove() {} };
const document = {
  documentElement: { append() {} },
  createElement: () => panel,
  querySelectorAll(selector) {
    if (selector === "[data-chatgpt-search-message-ids]") return nodes;
    if (selector === 'a[href*="/g/"][href$="/project"]' && projectHref)
      return [{ getAttribute: () => projectHref }];
    return [];
  }
};
const location = { pathname: `/g/${project}/c/${conversation}` };
class MutationObserver {
  constructor(callback) { observer = callback; }
  observe() {}
  disconnect() {}
}
const context = {
  document, location, MutationObserver, window: {},
  setTimeout(callback) { const id = ++nextTimer; timers.set(id, callback); return id; },
  clearTimeout(id) { timers.delete(id); },
  setInterval() { return ++nextTimer; },
  clearInterval() {},
  chrome: { runtime: { async sendMessage(batch) {
    sent.push(batch);
    return { ok: true, visible: batch.messages.length, added: batch.messages.length,
      noLongerVisible: 0 };
  } } }
};

function message(id, role, body = "Texto ficticio") {
  return {
    innerText: body,
    getAttribute: () => `${id} ${id}`,
    closest: () => ({ getAttribute: () => role }),
    querySelector: () => null
  };
}
async function flush() {
  const callbacks = [...timers.values()];
  timers.clear();
  callbacks.forEach(callback => callback());
  await new Promise(resolve => setImmediate(resolve));
}
async function observe(newNodes) {
  nodes = newNodes;
  observer?.([{ target: document.documentElement }]);
  await flush();
  return sent.at(-1);
}

nodes = [message(user, "user"), message(assistant, "assistant")];
vm.runInNewContext(source, context);
await flush();
assert.equal(sent.length, 1);
assert.equal(sent[0].projectId, project);
assert.deepEqual(Array.from(sent[0].messages, item => item.id), [user, assistant]);

const continuation = await observe([
  message(user, "user"), message(assistant, "assistant"),
  message(continuationUser, "user"), message(continuationAssistant, "assistant")
]);
assert.deepEqual(Array.from(continuation.messages, item => item.id),
  [user, assistant, continuationUser, continuationAssistant]);

const edit = await observe([
  message(editedUser, "user"), message(assistant, "assistant"),
  message(continuationUser, "user"), message(continuationAssistant, "assistant")
]);
assert.equal(edit.messages[0].id, editedUser);
assert.equal(edit.messages.some(item => item.id === user), false);

const regeneration = await observe([
  message(editedUser, "user"), message(regeneratedAssistant, "assistant"),
  message(continuationUser, "user"), message(continuationAssistant, "assistant")
]);
assert.equal(regeneration.messages[1].id, regeneratedAssistant);
assert.equal(regeneration.messages.some(item => item.id === assistant), false);

const incomplete = await observe([
  message(editedUser, "user", "x".repeat(9000)),
  message("invalid", "assistant")
]);
assert.equal(incomplete.skipped, 1);
assert.equal(incomplete.truncated, 1);
assert.equal(incomplete.messages[0].body.length, 8192);
location.pathname = `/c/${conversation}`;
projectHref = `/g/${project}/project`;
const breadcrumb = await observe([message(editedUser, "user", "Otro texto ficticio")]);
assert.equal(breadcrumb.projectId, project);
assert.equal(sent.length, 6);
process.stdout.write("Extension DOM fixture: project route/breadcrumb, continuation, edit, regenerate, partial capture OK\n");
