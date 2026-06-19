// ExperimentLab — shared front-end helpers (used by every page)

const esc = (s) => String(s ?? "").replace(/[&<>"']/g, m =>
  ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[m]));
const pct = (x) => (x * 100).toFixed(2) + "%";
const signed = (x) => (x >= 0 ? "+" : "") + (x * 100).toFixed(2) + "%";
const VERDICT = { SHIP: "Ship it", HOLD: "Hold", KEEP_RUNNING: "Keep running", NO_DIFFERENCE: "No winner" };

// Thin fetch wrapper — JSON in, raw Response out (callers decide how to read it).
async function api(method, url, body) {
  const opts = { method };
  if (body !== undefined) {
    opts.headers = { "Content-Type": "application/json" };
    opts.body = JSON.stringify(body);
  }
  return fetch(url, opts);
}

function badge(status) {
  const s = String(status || "").toLowerCase();
  return `<span class="badge ${s}">${esc(status)}</span>`;
}

// Lightweight toast (replaces alert()).
let _toastWrap;
function toast(msg, type = "info") {
  if (!_toastWrap) {
    _toastWrap = document.createElement("div");
    _toastWrap.className = "toast-wrap";
    document.body.appendChild(_toastWrap);
  }
  const t = document.createElement("div");
  t.className = "toast " + type;
  t.textContent = msg;
  _toastWrap.appendChild(t);
  requestAnimationFrame(() => t.classList.add("show"));
  setTimeout(() => { t.classList.remove("show"); setTimeout(() => t.remove(), 250); }, 3500);
}
