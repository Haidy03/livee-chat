import type { Room, RoomStatus } from "../models";

export interface AgentPermissions {
  isSupervisor: boolean;
  canDeleteMessage: boolean;
  canExport: boolean;
  canViewPII: boolean;
}

export const DEFAULT_PERMISSIONS: AgentPermissions = {
  isSupervisor: false,
  canDeleteMessage: false,
  canExport: true,
  canViewPII: true,
};

export const canAcceptRoom = (c: Room) => c.status === "offered" || c.status === "new";
export const canTransferRoom = (c: Room) =>
  ["assigned", "active", "waiting_customer", "pending", "escalated"].includes(c.status);
export const canResolveRoom = (c: Room) =>
  ["active", "waiting_customer", "pending", "escalated", "assigned"].includes(c.status);
export const canReopenRoom = (c: Room) => c.status === "resolved";
export const canViewCustomerPII = (p = DEFAULT_PERMISSIONS) => p.canViewPII;
export const canExportRoom = (p = DEFAULT_PERMISSIONS) => p.canExport;
export const canDeleteMessage = (p = DEFAULT_PERMISSIONS) => p.canDeleteMessage;
export const canStartVoiceCall = (c: Room, customerHasPhone: boolean) =>
  customerHasPhone && c.status !== "closed";
export const canInviteSupervisor = () => true;

export const isFinalStatus = (s: RoomStatus) => s === "closed" || s === "spam";
