import { create } from "zustand";

type DepartmentTreeState = {
  expandedIds: string[];
  selected: {
    id: string | null;
    name: string | null;
  };
  toggleExpanded: (id: string) => void;
  toggleSelected: ({ id, name }: { id: string; name: string }) => void;
};

export const useDepartmentTreeStore = create<DepartmentTreeState>()((set) => ({
  // Начальное состояние: ни один отдел не раскрыт.
  expandedIds: [],
  selected: {
    id: null,
    name: null,
  },

  // Действие: раскрыть отдел или свернуть его.
  toggleExpanded: (id) =>
    set((state) => ({
      expandedIds: state.expandedIds.includes(id)
        ? state.expandedIds.filter((item) => item !== id)
        : [...state.expandedIds, id],
    })),

  toggleSelected: ({ id, name }) => set(() => ({ selected: { id, name } })),
}));
