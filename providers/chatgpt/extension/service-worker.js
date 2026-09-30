const HOST = "com.personal_context_home.probe";
const pending = new Map();
let port;
let nextSequence = 0;

function connect() {
  if (port) return port;
  port = chrome.runtime.connectNative(HOST);
  port.onMessage.addListener((reply) => {
    const item = pending.get(reply.sequence);
    if (!item) return;
    pending.delete(reply.sequence);
    item.resolve(reply);
  });
  port.onDisconnect.addListener(() => {
    const reason = chrome.runtime.lastError?.message || "host_disconnected";
    port = undefined;
    for (const [sequence, item] of pending) {
      pending.delete(sequence);
      item.resolve({ ok: false, sequence, code: reason });
    }
  });
  return port;
}

chrome.action.onClicked.addListener(async (tab) => {
  if (!tab.id || !tab.url || new URL(tab.url).origin !== "https://chatgpt.com") {
    if (tab.id) chrome.action.setBadgeText({ tabId: tab.id, text: "URL" });
    return;
  }
  try {
    await chrome.scripting.executeScript({ target: { tabId: tab.id }, files: ["content.js"] });
    chrome.action.setBadgeText({ tabId: tab.id, text: "" });
  } catch {
    chrome.action.setBadgeText({ tabId: tab.id, text: "ERR" });
  }
});

chrome.runtime.onMessage.addListener((message, sender, respond) => {
  if (message?.kind !== "observation" || !sender.tab?.id ||
      !sender.url?.startsWith("https://chatgpt.com/")) return;
  if (!Array.isArray(message.messages) || message.messages.length > 500) {
    respond({ ok: false, code: "invalid_messages" });
    return;
  }
  const outbound = {
    kind: "observation",
    sequence: ++nextSequence,
    tabId: sender.tab.id,
    conversationId: message.conversationId,
    projectId: message.projectId,
    messages: message.messages.map(({ id, role, ordinal }) => ({ id, role, ordinal }))
  };
  if (new TextEncoder().encode(JSON.stringify(outbound)).length > 450_000) {
    respond({ ok: false, code: "batch_too_large" });
    return;
  }
  const sequence = outbound.sequence;
  new Promise((resolve) => {
    pending.set(sequence, { resolve });
    try { connect().postMessage(outbound); }
    catch (error) {
      pending.delete(sequence);
      resolve({ ok: false, sequence, code: String(error.message) });
    }
  }).then((reply) => {
    chrome.action.setBadgeText({ tabId: sender.tab.id, text: reply.ok ? "OK" : "ERR" });
    respond(reply);
  });
  return true;
});
