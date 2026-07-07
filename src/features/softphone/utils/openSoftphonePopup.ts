export const SOFTPHONE_POPUP_NAME = "KhwarizmiDialer";
export const SOFTPHONE_OPEN_MODE_KEY = "softphone:open-mode";

export type SoftphoneOpenMode = "popup" | "page";

export function openSoftphonePopup(path = "/softphone"): Window | null {
  // No forced width/height — the popup resizes itself to fit content on load.
  const features =
    "popup=yes,resizable=no,scrollbars=no,toolbar=no,menubar=no,status=no,location=no";
  const win = window.open(path, SOFTPHONE_POPUP_NAME, features);
  try {
    win?.focus();
  } catch {
    /* ignore */
  }
  return win;
}

/**
 * Resize the current popup window so its outer size fits the rendered content
 * exactly (no blank area). Safe no-op outside a popup or when the browser
 * blocks resizeTo.
 */
export function fitPopupToContent(root?: HTMLElement | null): void {
  if (typeof window === "undefined") return;
  try {
    const el =
      root ??
      document.querySelector<HTMLElement>(".softphone-bg-texture") ??
      document.documentElement;
    const rect = el.getBoundingClientRect();
    const chromeW = Math.max(0, window.outerWidth - window.innerWidth);
    const chromeH = Math.max(0, window.outerHeight - window.innerHeight);
    const maxW = window.screen?.availWidth ?? 1920;
    const maxH = window.screen?.availHeight ?? 1080;
    const w = Math.min(Math.ceil(rect.width) + chromeW, maxW);
    const h = Math.min(Math.ceil(rect.height) + chromeH, maxH);
    if (w > 0 && h > 0) window.resizeTo(w, h);
  } catch {
    /* ignore — some browsers block resizeTo */
  }
}

export function getSoftphoneOpenMode(): SoftphoneOpenMode {
  if (typeof window === "undefined") return "page";
  const v = localStorage.getItem(SOFTPHONE_OPEN_MODE_KEY);
  return v === "popup" ? "popup" : "page";
}

export function setSoftphoneOpenMode(mode: SoftphoneOpenMode): void {
  localStorage.setItem(SOFTPHONE_OPEN_MODE_KEY, mode);
}

export function isSoftphonePopupWindow(): boolean {
  if (typeof window === "undefined") return false;
  try {
    return !!window.opener && window.name === SOFTPHONE_POPUP_NAME;
  } catch {
    return false;
  }
}

/**
 * Open the softphone using the user's preferred mode. Falls back to in-app
 * navigation if the popup is blocked.
 */
export function openSoftphone(
  navigate: (path: string) => void,
  path = "/softphone",
): void {
  if (getSoftphoneOpenMode() === "popup") {
    const win = openSoftphonePopup(path);
    if (win) return;
  }
  navigate(path);
}
