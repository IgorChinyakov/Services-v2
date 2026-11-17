import { create } from "zustand";

type SelectedDepartmentsState = {
  selectedIds: string[];
};

type SelectedDepartmentsActions = {
  toggleSelected: (id: string) => void;
  setSelectedIds: (ids: string[]) => void;
  clearSelecteion: () => void;
};

export const useSelectedDepartmentsStore = create<
  SelectedDepartmentsState & SelectedDepartmentsActions
>()((set) => ({
  selectedIds: [],
  toggleSelected: (id) =>
    set((state) => ({
      selectedIds: state.selectedIds.includes(id)
        ? state.selectedIds.filter((selectedId) => selectedId !== id)
        : [...state.selectedIds, id],
    })),
  setSelectedIds: (ids) =>
    set(() => ({
      selectedIds: ids,
    })),
  clearSelecteion: () =>
    set(() => ({
      selectedIds: [],
    })),
}));
