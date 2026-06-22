import type { Menu } from '../../api/types';
import { EntityCrudPage, type EntityListRenderProps } from '../EntityCrudPage';
import { menuConfig } from '../entityConfigs';
import { buildMenuTree, type MenuNode } from './buildMenuTree';

function MenuNodeRow({ node, depth, onEdit, onDelete }: {
  node: MenuNode;
  depth: number;
  onEdit: (record: Menu) => void;
  onDelete: (record: Menu) => void;
}) {
  return (
    <>
      <li style={{ marginLeft: depth * 20 }}>
        <span>{node.title}</span>
        <button type="button" onClick={() => onEdit(node)}>
          Edit
        </button>
        <button type="button" onClick={() => onDelete(node)}>
          Delete
        </button>
      </li>
      {node.children.map((child) => (
        <MenuNodeRow key={child.id} node={child} depth={depth + 1} onEdit={onEdit} onDelete={onDelete} />
      ))}
    </>
  );
}

function MenuHierarchy({ records, onEdit, onDelete }: EntityListRenderProps<Menu>) {
  const tree = buildMenuTree(records);
  return (
    <ul className="cms-menu-tree">
      {tree.map((node) => (
        <MenuNodeRow key={node.id} node={node} depth={0} onEdit={onEdit} onDelete={onDelete} />
      ))}
    </ul>
  );
}

/** Menu items render as a parent/child tree, not a flat list (FR-003) — create/edit/delete still go through EntityCrudPage's generic form. */
export function MenuTree({ domainId }: { domainId: number }) {
  return <EntityCrudPage config={menuConfig} listQuery={{ domainId }} renderList={MenuHierarchy} />;
}
