export type QueueChannel = "voice" | "chat" | "whatsapp" | "omnichannel";
export type QueueStatus = "active" | "inactive" | "draft";
export type QueueRoutingMode = "skills_based" | "static" | "hybrid";
export type QueueRoutingStrategy =
  | "best_skill_match"
  | "longest_idle"
  | "fewest_calls"
  | "round_robin"
  | "priority_penalty"
  | "ringall"
  | "leastrecent"
  | "fewestcalls"
  | "random"
  | "rrmemory"
  | "linear"
  | "wrandom";
export type QueueTieBreaker =
  | "longest_idle"
  | "fewest_calls"
  | "highest_priority"
  | "round_robin"
  | "random";
export type QueueSkillMatchLogic = "all" | "any";
export type RequirementType = "mandatory" | "preferred";

export interface QueueSkillRequirement {
  id: string;
  skillId: string;
  minimumLevel: number; // 1..5
  requirementType: RequirementType;
  weight: number; // 0..100
}

export interface QueueMember {
  id: string;
  agentId: string;
  priority: number; // 1..10
  penalty: number; // 0..100
  enabled: boolean;
  maxConcurrentInteractions?: number;
  wrapUpTimeOverride?: number;
}

export interface QueueFallbackAgent {
  id: string;
  agentId: string;
  priority: number;
  triggerAfterSeconds: number;
  enabled: boolean;
}

export type RelaxationAction =
  | "reduce_minimum_level"
  | "ignore_preferred"
  | "allow_fallback"
  | "route_to_queue";

export interface SkillRelaxationRule {
  id: string;
  waitSeconds: number;
  action: RelaxationAction;
  scope: "all" | "mandatory" | "preferred";
  reduction: number;
  destinationQueueId?: string;
}

export interface QueueSettings {
  capacity: {
    maxQueueLength: number;
    maxWaitTimeSeconds: number;
    maxConcurrentInteractions: number;
    maxConcurrentPerAgent: number;
  };
  ringing: {
    agentRingTimeoutSeconds: number;
    retryDelaySeconds: number;
    wrapUpTimeSeconds: number;
    wrapUpEnabled: boolean;
    autoAnswer: boolean;
    allowReject: boolean;
    reOfferRejected: boolean;
  };
  cx: {
    musicOnHold: string;
    welcomeAnnouncement: string;
    periodicAnnouncement: string;
    announcementIntervalSeconds: number;
    positionAnnouncement: boolean;
    estimatedWaitAnnouncement: boolean;
    callbackOffer: boolean;
    voicemailOffer: boolean;
  };
  sla: {
    targetTimeSeconds: number;
    targetPercentage: number;
    warningThreshold: number;
    criticalThreshold: number;
  };
}

export type OverflowTrigger =
  | "no_qualified_agents"
  | "no_available_agents"
  | "queue_full"
  | "max_wait_reached"
  | "outside_hours"
  | "routing_failure";

export type OverflowAction =
  | "route_to_queue"
  | "route_to_ivr"
  | "route_to_voicemail"
  | "offer_callback"
  | "route_external"
  | "announce_hangup"
  | "route_to_supervisor";

export interface QueueOverflowRule {
  id: string;
  trigger: OverflowTrigger;
  action: OverflowAction;
  destinationId?: string;
  delaySeconds: number;
  priority: number;
  enabled: boolean;
}

export type WorkingHoursMode = "always" | "existing" | "custom";

export interface CustomScheduleDay {
  enabled: boolean;
  start: string; // "09:00"
  end: string; // "18:00"
}

export interface QueueWorkingHours {
  mode: WorkingHoursMode;
  existingScheduleId?: string;
  timezone: string;
  schedule: Record<string, CustomScheduleDay>; // sun..sat
  outsideAction: OverflowAction;
  outsideDestinationId?: string;
}

export interface Queue {
  id: string;
  name: string;
  code: string;
  description?: string;
  channel: QueueChannel;
  departmentId?: string;
  status: QueueStatus;
  tags: string[];
  routingMode: QueueRoutingMode;
  routingStrategy: QueueRoutingStrategy;
  tieBreaker: QueueTieBreaker;
  skillMatchLogic: QueueSkillMatchLogic;
  requiredSkills: QueueSkillRequirement[];
  members: QueueMember[];
  fallbackAgents: QueueFallbackAgent[];
  skillRelaxationEnabled: boolean;
  skillRelaxationRules: SkillRelaxationRule[];
  settings: QueueSettings;
  overflowRules: QueueOverflowRule[];
  workingHours: QueueWorkingHours;
  createdAt: string;
  updatedAt: string;
}

export interface MockSkill {
  id: string;
  name: string;
}

export interface MockAgentForQueue {
  id: string;
  name: string;
  extension: string;
  groupId: string;
  groupName: string;
  status: "available" | "busy" | "paused" | "offline";
  skills: { skillId: string; level: number }[];
}
