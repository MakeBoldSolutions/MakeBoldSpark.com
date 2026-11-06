import { type Recipe } from '../api/recipeClient';

type RecipeListPageProps = {
  recipes: Recipe[];
  page: number;
  pageSize: number;
  totalCount: number;
  loading: boolean;
  onEdit: (recipe: Recipe) => void;
  onDelete: (recipe: Recipe) => void;
  onPrevious: () => void;
  onNext: () => void;
};

export function RecipeListPage({
  recipes,
  page,
  pageSize,
  totalCount,
  loading,
  onEdit,
  onDelete,
  onPrevious,
  onNext,
}: RecipeListPageProps) {
  const hasPrevious = page > 1;
  const hasNext = page * pageSize < totalCount;

  if (loading) return <p className="empty">Loading recipes...</p>;
  if (recipes.length === 0) return <p className="empty">No recipes in this domain.</p>;

  return (
    <section aria-label="Recipe inventory">
      <div className="list-toolbar">
        <span className="eyebrow">
          Page {page} of {Math.max(1, Math.ceil(totalCount / pageSize))}
        </span>
        <div className="actions">
          <button type="button" className="ghost" onClick={onPrevious} disabled={!hasPrevious}>
            Previous
          </button>
          <button type="button" className="ghost" onClick={onNext} disabled={!hasNext}>
            Next
          </button>
        </div>
      </div>
      <ul className="recipe-list">
        {recipes.map((recipe) => (
          <li key={recipe.id}>
            <button type="button" onClick={() => onEdit(recipe)}>
              {recipe.name}
            </button>
            <span className="meta">{recipe.recipeCategoryName}</span>
            <span className={`badge ${recipe.isApproved ? 'published' : 'draft'}`}>
              {recipe.isApproved ? 'Published' : 'Draft'}
            </span>
            <button type="button" className="danger" onClick={() => onDelete(recipe)}>
              Delete
            </button>
          </li>
        ))}
      </ul>
    </section>
  );
}
