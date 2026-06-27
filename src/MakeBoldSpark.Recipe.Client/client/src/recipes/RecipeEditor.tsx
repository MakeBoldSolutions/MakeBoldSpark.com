import { type Category, type Recipe } from '../api/recipeClient';

type RecipeEditorProps = {
  recipe: Partial<Recipe>;
  categories: Category[];
  onChange: (recipe: Partial<Recipe>) => void;
  onSave: () => void;
  onCancel: () => void;
};

export function RecipeEditor({ recipe, categories, onChange, onSave, onCancel }: RecipeEditorProps) {
  return (
    <section className="editor" aria-label="Recipe editor">
      <h2>{recipe.id ? 'Edit recipe' : 'New recipe'}</h2>
      <label className="field">
        <span>Name</span>
        <input value={recipe.name ?? ''} onChange={(event) => onChange({ ...recipe, name: event.target.value })} />
      </label>
      <label className="field">
        <span>Author</span>
        <input
          value={recipe.authorName ?? ''}
          onChange={(event) => onChange({ ...recipe, authorName: event.target.value })}
        />
      </label>
      <label className="field">
        <span>Category</span>
        <select
          value={recipe.recipeCategoryId ?? 0}
          onChange={(event) => onChange({ ...recipe, recipeCategoryId: Number(event.target.value) })}
        >
          <option value={0}>Select category</option>
          {categories.map((category) => (
            <option key={category.id} value={category.id}>
              {category.name}
            </option>
          ))}
        </select>
      </label>
      <label className="field">
        <span>Description</span>
        <textarea
          value={recipe.description ?? ''}
          onChange={(event) => onChange({ ...recipe, description: event.target.value })}
        />
      </label>
      <label className="field">
        <span>Ingredients</span>
        <textarea
          value={recipe.ingredients ?? ''}
          onChange={(event) => onChange({ ...recipe, ingredients: event.target.value })}
        />
      </label>
      <label className="field">
        <span>Instructions</span>
        <textarea
          value={recipe.instructions ?? ''}
          onChange={(event) => onChange({ ...recipe, instructions: event.target.value })}
        />
      </label>
      <label className="field inline">
        <input
          type="checkbox"
          checked={recipe.isApproved ?? false}
          onChange={(event) => onChange({ ...recipe, isApproved: event.target.checked })}
        />
        <span>Published</span>
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
