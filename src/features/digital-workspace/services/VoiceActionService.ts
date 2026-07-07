/**
 * Voice channel handoff contract. Backed by the existing VoIP/CTI module.
 * Stubbed here so the digital workspace can call `startOutbound()` without
 * touching the current telephony implementation.
 */
export interface VoiceCallRequest {
  customerId: string;
  phoneNumber: string;
  roomId?: string;
  caseId?: string;
}

export interface VoiceActionService {
  startOutbound(req: VoiceCallRequest): Promise<{ callId: string }>;
  escalateToVoice(req: VoiceCallRequest): Promise<{ callId: string }>;
  isAvailable(): boolean;
}

export class StubVoiceActionService implements VoiceActionService {
  async startOutbound(req: VoiceCallRequest) {
    console.info("[VoiceActionService] startOutbound (stub)", req);
    return { callId: `stub-${Date.now()}` };
  }
  async escalateToVoice(req: VoiceCallRequest) {
    console.info("[VoiceActionService] escalateToVoice (stub)", req);
    return { callId: `stub-${Date.now()}` };
  }
  isAvailable() {
    return true;
  }
}

export const voiceActionService: VoiceActionService = new StubVoiceActionService();
