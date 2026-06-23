import type { Menu } from '../../api/types';
import { EntityCrudPage, type EntityListRenderProps } from '../EntityCrudPage';
import { menuConfig } from '../entityConfigs';
import { buildMenuTree, type MenuNode } from './buildMenuTree';

function MenuNodeRow({ node, depth, onEdit }: {
  node: MenuNode;
  depth: number;
  onEdit: (record: Menu) => void;
}) {
  return (
    <>
      <li
        className="cms-editable-row"
        style={{ marginLeft: depth * 20 }}
        tabIndex={0}
        onClick={() => onEdit(node)}
        onKeyDown={(event) => {
          if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault();
            onEdit(node);
          }
        }}
      >
        <span>{node.title}</span>
      </li>
      {node.children.map((child) => (
        <MenuNodeRow key={child.id} node={child} depth={depth + 1} onEdit={onEdit} />
      ))}
    </>
  );
}

function MenuHierarchy({ records, onEdit }: EntityListRenderProps<Menu>) {
  const tree = buildMenuTree(records);
  return (
    <ul className="cms-menu-tree">
      {tree.map((node) => (
        <MenuNodeRow key={node.id} node={node} depth={0} onEdit={onEdit} />
      ))}
    </ul>
  );
}

/** Menu items render as a parent/child tree, not a flat list (FR-003) — create/edit/delete still go through EntityCrudPage's generic form. */
export function MenuTree({ domainId }: { domainId: number }) {
  return <EntityCrudPage config={menuConfig} listQuery={{ domainId }} renderList={MenuHierarchy} />;
}
