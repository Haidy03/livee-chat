import { useEffect, useState } from "react";
import { getSipAdapter, setSipAdapter } from "./SipService";

export { getSipAdapter };
import { DemoSipAdapter } from "./demoAdapter";
import type {
  ActiveSipCall,
  CallEvent,
  RegistrationEvent,
  RegistrationStatus,
  SipAdapter,
  SipDebugEvent,
} from "./types";

export interface SipUiState {
  registration: RegistrationStatus;
  registrationReason?: string;
  call: ActiveSipCall | null;
  debugLog: SipDebugEvent[];
}

const DEBUG_LOG_MAX = 200;

let demoAdapter: DemoSipAdapter | null = null;
let unsubscribeRegistration: (() => void) | null = null;
let unsubscribeCall: (() => void) | null = null;
let unsubscribeDebug: (() => void) | null = null;
let wiredAdapter: SipAdapter | null = null;

export function enableDemoMode(enabled: boolean) {
  if (enabled) {
    if (!demoAdapter) demoAdapter = new DemoSipAdapter();
    setSipAdapter(demoAdapter);
    wired = false;
    ensureWired();
  } else {
    // Do not recreate the real adapter on every AccountTab mount. Only switch
    // away when demo mode was actually active; otherwise listeners remain on
    // the live adapter that will emit REGISTER / 401 / 200 wire events.
    if (demoAdapter && getSipAdapter() === demoAdapter) {
      setSipAdapter(undefined as unknown as SipAdapter);
      wired = false;
      ensureWired();
    }
  }
}

export function getDemoAdapter(): DemoSipAdapter {
  if (!demoAdapter) demoAdapter = new DemoSipAdapter();
  return demoAdapter;
}

// Module-global state so the debug log + registration survive component
// remounts (e.g., navigating between settings tabs). Without this, each
// `useSipState` instance kept its own buffer and lost events fired before
// mount, which is why the SIP wire-trace panel sometimes appeared empty
// after clicking "Test register".
const globalState: SipUiState = {
  registration: "unregistered",
  call: null,
  debugLog: [],
};
const subscribers = new Set<(s: SipUiState) => void>();
let wired = false;

function setGlobal(patch: Partial<SipUiState>) {
  Object.assign(globalState, patch);
  const snapshot: SipUiState = { ...globalState, debugLog: globalState.debugLog };
  subscribers.forEach((fn) => fn(snapshot));
}

function ensureWired() {
  const adapter = getSipAdapter();
  if (wired && wiredAdapter === adapter) return;

  unsubscribeRegistration?.();
  unsubscribeCall?.();
  unsubscribeDebug?.();
  wiredAdapter = adapter;
  wired = true;
  unsubscribeRegistration = adapter.on("registration", (p) => {
    const e = p as RegistrationEvent;
    setGlobal({ registration: e.status, registrationReason: e.reason });
  });
  unsubscribeCall = adapter.on("call", (p) => {
    const e = p as CallEvent;
    setGlobal({ call: e.call });
  });
  unsubscribeDebug = adapter.on("debug", (p) => {
    const e = p as SipDebugEvent;
    globalState.debugLog = [...globalState.debugLog.slice(-DEBUG_LOG_MAX + 1), e];
    setGlobal({});
  });
}

export function useSipState(): SipUiState {
  ensureWired();
  const [state, setState] = useState<SipUiState>(globalState);
  useEffect(() => {
    subscribers.add(setState);
    setState({ ...globalState });
    return () => { subscribers.delete(setState); };
  }, []);
  return state;
}

/** Synchronous read of the latest SIP UI state (no React subscription). */
export function getSipSnapshot(): SipUiState {
  ensureWired();
  return globalState;
}
