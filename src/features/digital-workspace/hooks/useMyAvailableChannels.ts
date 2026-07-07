import { useEffect, useState } from "react";
import { api } from "@/lib/apiClient";
import { useAuth } from "@/hooks/useAuth";
import type { ChannelGroup } from "../models";

const USER_TO_GROUP: Record<string, ChannelGroup> = {
  voice: "phone",
  chat: "chat",
  email: "email",
  social: "social",
};

interface MyProfile {
  availableChannels?: string[] | null;
}

let cache: ChannelGroup[] | null = null;
let inflight: Promise<ChannelGroup[]> | null = null;

async function loadMyChannels(): Promise<ChannelGroup[]> {
  if (cache) return cache;
  if (!inflight) {
    inflight = (async () => {
      const me = await api.get<MyProfile>("/profiles/me");
      const groups: ChannelGroup[] = [];
      for (const c of me?.availableChannels ?? []) {
        const g = USER_TO_GROUP[c];
        if (g && !groups.includes(g)) groups.push(g);
      }
      cache = groups;
      return groups;
    })();
  }
  return inflight;
}

export function useMyAvailableChannels(): ChannelGroup[] | undefined {
  const { user } = useAuth();
  const [channels, setChannels] = useState<ChannelGroup[] | undefined>(
    cache ?? undefined,
  );

  useEffect(() => {
    if (!user) {
      setChannels(undefined);
      return;
    }
    let cancelled = false;
    loadMyChannels().then((groups) => {
      if (!cancelled) setChannels(groups);
    });
    return () => {
      cancelled = true;
    };
  }, [user]);

  return channels;
}
