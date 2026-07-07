(function () {
  var script = document.currentScript;
  if (!script) return;
  var d = script.dataset;

  var origin = new URL(script.src).origin;
  var params = new URLSearchParams();
  params.set("mode", "embed");
  if (d.apiBase) params.set("apiBase", d.apiBase);
  if (d.projectId) params.set("projectId", d.projectId);
  if (d.chatbotId) params.set("chatbotId", d.chatbotId);
  if (d.agentChannel) params.set("agentChannel", d.agentChannel);
  if (d.channel) params.set("channel", d.channel);
  if (d.title) params.set("title", d.title);
  if (d.subtitle) params.set("subtitle", d.subtitle);
  if (d.accent) params.set("accent", d.accent);
  if (d.departments) params.set("departments", d.departments);
  if (d.lang) params.set("lang", d.lang);

  var accent = d.accent || "#2563eb";
  var open = false;

  var iframe = document.createElement("iframe");
  iframe.src = origin + "/widget?" + params.toString();
  iframe.title = "Chat widget";
  iframe.allow = "microphone; clipboard-write";
  iframe.style.cssText = [
    "position:fixed",
    "bottom:88px",
    "right:16px",
    "width:380px",
    "height:560px",
    "max-width:calc(100vw - 32px)",
    "max-height:calc(100vh - 120px)",
    "border:0",
    "border-radius:16px",
    "box-shadow:0 20px 50px rgba(0,0,0,0.25)",
    "z-index:2147483646",
    "display:none",
    "background:transparent",
  ].join(";");

  var button = document.createElement("button");
  button.setAttribute("aria-label", "Open chat");
  button.innerHTML =
    '<svg xmlns="http://www.w3.org/2000/svg" width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/></svg>';
  button.style.cssText = [
    "position:fixed",
    "bottom:16px",
    "right:16px",
    "width:56px",
    "height:56px",
    "border:0",
    "border-radius:9999px",
    "background:" + accent,
    "color:#fff",
    "cursor:pointer",
    "box-shadow:0 10px 25px rgba(0,0,0,0.25)",
    "z-index:2147483647",
    "display:flex",
    "align-items:center",
    "justify-content:center",
  ].join(";");

  button.addEventListener("click", function () {
    open = !open;
    iframe.style.display = open ? "block" : "none";
  });

  document.body.appendChild(iframe);
  document.body.appendChild(button);
})();
