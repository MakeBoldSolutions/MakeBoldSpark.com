import type { Menu } from '../../api/types';

export interface MenuNode extends Menu {
  children: MenuNode[];
}

export function buildMenuTree(flat: Menu[], rootParentId: number | null = null): MenuNode[] {
  return flat
    .filter((m) => m.parentId === rootParentId)
    .sort((a, b) => a.displayOrder - b.displayOrder || a.title.localeCompare(b.title))
    .map((m) => ({
      ...m,
      children: buildMenuTree(flat, m.id),
    }));
}
