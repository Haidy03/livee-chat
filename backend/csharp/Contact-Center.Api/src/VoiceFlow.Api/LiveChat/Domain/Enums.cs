namespace VoiceFlow.Api.LiveChat.Domain;

public enum ChannelType { WebWidget, MobileApp, WhatsApp, Messenger, Instagram, Voip }
public enum AgentStatus { Offline, Available, Busy, Away }

public enum MessageDirection { Inbound, Outbound }
public enum SenderType { Customer, Agent, System, Bot }
