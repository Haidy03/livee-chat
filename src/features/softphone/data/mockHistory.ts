import type { CallRecord } from "../store";

const now = Date.now();
export const mockHistory: CallRecord[] = [
  { id: "h1", contactId: "c1", number: "+14155550123", type: "in",     at: now - 1000 * 60 * 4,            durationSec: 312 },
  { id: "h2", contactId: "c3", number: "+14085550199", type: "out",    at: now - 1000 * 60 * 35,           durationSec: 84  },
  { id: "h3", contactId: "c5", number: "+12135550155", type: "missed", at: now - 1000 * 60 * 60 * 2,       durationSec: 0   },
  { id: "h4", contactId: "c7", number: "+971501234567",type: "in",     at: now - 1000 * 60 * 60 * 5,       durationSec: 540 },
  { id: "h5", contactId: "c11",number: "+13125550144", type: "out",    at: now - 1000 * 60 * 60 * 8,       durationSec: 22  },
  { id: "h6",                  number: "+19998887777", type: "missed", at: now - 1000 * 60 * 60 * 24,      durationSec: 0   },
  { id: "h7", contactId: "c14",number: "+966551112233",type: "in",     at: now - 1000 * 60 * 60 * 26,      durationSec: 198 },
  { id: "h8", contactId: "c2", number: "+966501234567",type: "out",    at: now - 1000 * 60 * 60 * 48,      durationSec: 760 },
  { id: "h9", contactId: "c20",number: "+447911123456",type: "out",    at: now - 1000 * 60 * 60 * 72,      durationSec: 145 },
  { id: "h10",contactId: "c17",number: "+919812345678",type: "missed", at: now - 1000 * 60 * 60 * 80,      durationSec: 0   },
];
