(() => {
  if (window.__pchProbeStop) {
    window.__pchProbeStop();
    delete window.__pchProbeStop;
    return;
  }

  const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
  const panel = document.createElement("div");
  panel.style.cssText = "position:fixed;right:12px;bottom:12px;z-index:2147483647;padding:8px 10px;background:#123;color:white;border-radius:6px;font:12px sans-serif;max-width:360px";
  panel.textContent = "PCH prueba: esperando chat de prueba";
  document.documentElement.append(panel);
  let lastSignature = "";
  let lastPath = location.pathname;
  let timer;
  let busy = false;

  function observation() {
    const conversationId = location.pathname.match(/(?:^|\/)c\/([0-9a-f-]{36})(?:\/|$)/i)?.[1];
    if (!conversationId || !UUID.test(conversationId)) return null;
    const projectFromPath = location.pathname.match(/(?:^|\/)g\/([0-9a-f-]{36})/i)?.[1];
    const projectFromLink = [...document.querySelectorAll('a[href*="/g/"][href$="/project"]')]
      .map(a => a.getAttribute("href")?.match(/\/g\/([0-9a-f-]{36})\/project/i)?.[1])
      .find(Boolean);
    const projectId = projectFromPath || projectFromLink || null;
    const messages = [];
    let skipped = 0;
    let truncated = 0;
    const seen = new Set();
    for (const element of document.querySelectorAll("[data-chatgpt-search-message-ids]")) {
      const tokens = (element.getAttribute("data-chatgpt-search-message-ids") || "").split(/\s+/).filter(Boolean);
      const distinct = [...new Set(tokens)];
      const id = distinct.length === 1 && UUID.test(distinct[0]) ? distinct[0] : null;
      const role = element.closest('[data-message-author-role]')?.getAttribute("data-message-author-role") ||
        element.querySelector('[data-message-author-role]')?.getAttribute("data-message-author-role");
      if (!id || !["user", "assistant"].includes(role)) { skipped++; continue; }
      if (seen.has(id)) continue;
      seen.add(id);
      const fullBody = (element.innerText || element.textContent || "").trim();
      if (fullBody.length > 8192) truncated++;
      const body = fullBody.slice(0, 8192);
      messages.push({ id, role, ordinal: messages.length, body });
      if (messages.length >= 500) { skipped++; break; }
    }
    return {
      kind: "observation", conversationId, projectId,
      messages, skipped, truncated,
      coverage: "visible_dom_only"
    };
  }

  async function send() {
    timer = undefined;
    if (busy) { schedule(); return; }
    const batch = observation();
    if (!batch) { panel.textContent = "PCH prueba: sin ruta de chat"; return; }
    const signature = JSON.stringify(batch);
    if (signature === lastSignature) return;
    busy = true;
    try {
      const reply = await chrome.runtime.sendMessage(batch);
      if (!reply?.ok) throw new Error(reply?.code || "sin confirmación");
      lastSignature = signature;
      panel.textContent = `PCH prueba: ${reply.visible} visibles, +${reply.added}, ${reply.noLongerVisible} ya no visibles, ${batch.skipped} sin ID/rol, ${batch.truncated} truncados · solo DOM`;
    } catch (error) {
      panel.textContent = `PCH prueba: error del puente (${String(error.message).slice(0, 80)})`;
      setTimeout(schedule, 3000);
    } finally {
      busy = false;
    }
  }

  function schedule() {
    clearTimeout(timer);
    timer = setTimeout(send, 1000);
  }
  const observer = new MutationObserver((changes) => {
    if (changes.every(change => panel.contains(change.target))) return;
    schedule();
  });
  observer.observe(document.documentElement, { childList: true, subtree: true, characterData: true, attributes: true });
  const poll = setInterval(() => {
    if (location.pathname !== lastPath) { lastPath = location.pathname; schedule(); }
  }, 1000);
  window.__pchProbeStop = () => {
    observer.disconnect();
    clearInterval(poll);
    clearTimeout(timer);
    panel.remove();
  };
  schedule();
})();
