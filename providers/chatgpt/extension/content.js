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
  let totalAdded = 0;
  let totalNoLongerVisible = 0;

  function structuralRole(element) {
    for (let node = element; node; node = node.parentElement) {
      const classes = typeof node.className === "string" ? node.className.split(/\s+/) : [];
      if (classes.includes("group/user-message")) return "user";
      if (classes.includes("group/assistant-message")) return "assistant";
      const testId = node.getAttribute?.("data-testid");
      if (testId === "user-message") return "user";
      if (testId === "assistant-message") return "assistant";
    }
    return null;
  }

  function isRendered(element) {
    if (element.closest('[hidden],[aria-hidden="true"],[inert]')) return false;
    if (element.getClientRects && element.getClientRects().length === 0) return false;
    for (let node = element; node && node.nodeType === 1; node = node.parentElement) {
      const style = window.getComputedStyle?.(node);
      if (style && (style.display === "none" || style.visibility === "hidden" ||
          style.contentVisibility === "hidden")) return false;
    }
    return true;
  }

  function observation() {
    const conversationId = location.pathname.match(/(?:^|\/)c\/([0-9a-f-]{36})(?:\/|$)/i)?.[1];
    if (!conversationId || !UUID.test(conversationId)) return null;
    const projectIdFromUrl = value => value?.match(/\/g\/(g-p-[0-9a-f]{32}|[0-9a-f-]{36})(?:-|\/)/i)?.[1];
    const projectFromPath = projectIdFromUrl(location.pathname);
    const projectFromLink = [...document.querySelectorAll('a[href*="/g/"][href$="/project"]')]
      .map(a => projectIdFromUrl(a.getAttribute("href")))
      .find(Boolean);
    const projectId = projectFromPath || projectFromLink || null;
    const messages = [];
    let skipped = 0;
    let missingId = 0;
    let missingRole = 0;
    let truncated = 0;
    let hiddenDom = 0;
    let inferredAssistant = 0;
    const seen = new Set();
    for (const element of document.querySelectorAll("[data-chatgpt-search-message-ids]")) {
      if (!isRendered(element)) { hiddenDom++; continue; }
      const tokens = (element.getAttribute("data-chatgpt-search-message-ids") || "").split(/\s+/).filter(Boolean);
      const distinct = [...new Set(tokens)];
      const id = distinct.length === 1 && UUID.test(distinct[0]) ? distinct[0] : null;
      if (!id) {
        skipped++;
        missingId++;
        continue;
      }
      if (seen.has(id)) continue;
      let role = element.closest('[data-message-author-role]')?.getAttribute("data-message-author-role") ||
        element.querySelector('[data-message-author-role]')?.getAttribute("data-message-author-role") ||
        structuralRole(element);
      if (!role && messages.at(-1)?.role === "user") {
        role = "assistant";
        inferredAssistant++;
      }
      if (!["user", "assistant"].includes(role)) {
        role = "unknown";
        missingRole++;
      }
      seen.add(id);
      const fullBody = (element.innerText || element.textContent || "").trim();
      if (fullBody.length > 8192) truncated++;
      const body = fullBody.slice(0, 8192);
      messages.push({ id, role, ordinal: messages.length, body });
      if (messages.length >= 500) { skipped++; break; }
    }
    return {
      kind: "observation", conversationId, projectId,
      messages, skipped, missingId, missingRole, inferredAssistant, truncated, hiddenDom,
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
      totalAdded += reply.added;
      totalNoLongerVisible += reply.noLongerVisible;
      panel.textContent = `PCH prueba: ${reply.visible} visibles · último +${reply.added}/-${reply.noLongerVisible} · acumulado +${totalAdded}/-${totalNoLongerVisible} · proyecto ${reply.projectObserved ? "sí" : "no"} · ${batch.hiddenDom} ocultos, ${batch.missingId} sin ID, ${batch.missingRole} rol desconocido, ${batch.inferredAssistant} rol inferido, ${batch.truncated} truncados`;
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
