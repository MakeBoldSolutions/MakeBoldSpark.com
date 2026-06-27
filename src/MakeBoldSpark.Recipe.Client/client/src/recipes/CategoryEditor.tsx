import { type Category } from '../api/recipeClient';

type CategoryEditorProps = {
  category: Partial<Category>;
  onChange: (category: Partial<Category>) => void;
  onSave: () => void;
  onCancel: () => void;
};

export function CategoryEditor({ category, onChange, onSave, onCancel }: CategoryEditorProps) {
  return (
    <section className="editor" aria-label="Category editor">
      <h2>{category.id ? 'Edit category' : 'New category'}</h2>
      <label className="field">
        <span>Category name</span>
        <input value={category.name ?? ''} onChange={(event) => onChange({ ...category, name: event.target.value })} />
      </label>
      <label className="field">
        <span>Category description</span>
        <textarea
          value={category.description ?? ''}
          onChange={(event) => onChange({ ...category, description: event.target.value })}
        />
      </label>
      <label className="field">
        <span>Display order</span>
        <input
          type="number"
          value={category.displayOrder ?? 0}
          onChange={(event) => onChange({ ...category, displayOrder: Number(event.target.value) })}
        />
      </label>
      <label className="field inline">
        <input
          type="checkbox"
          checked={category.isActive ?? true}
          onChange={(event) => onChange({ ...category, isActive: event.target.checked })}
        />
        <span>Active</span>
      </label>
      <div className="actions">
        <button type="button" onClick={onSave}>
          Save
        </button>
        <button type="button" onClick={onCancel}>
          Cancel
        </button>
      </div>
    </section>
  );
}
