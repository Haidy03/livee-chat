import { create } from "zustand";
import { persist } from "zustand/middleware";

interface State {
  tags: Record<string, string[]>;
  add: (rowId: string, tag: string) => void;
  remove: (rowId: string, tag: string) => void;
  get: (rowId: string) => string[];
}

export const useTagsStore = create<State>()(
  persist(
    (set, getState) => ({
      tags: {},
      add: (rowId, tag) => {
        const t = tag.trim().slice(0, 24);
        if (!t) return;
        const cur = getState().tags[rowId] ?? [];
        if (cur.includes(t)) return;
        set({ tags: { ...getState().tags, [rowId]: [...cur, t] } });
      },
      remove: (rowId, tag) => {
        const cur = getState().tags[rowId] ?? [];
        set({ tags: { ...getState().tags, [rowId]: cur.filter((x) => x !== tag) } });
      },
      get: (rowId) => getState().tags[rowId] ?? [],
    }),
    { name: "softphone:tags" },
  ),
);
