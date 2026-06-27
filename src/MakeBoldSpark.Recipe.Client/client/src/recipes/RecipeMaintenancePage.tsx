import { useCallback, useEffect, useState } from 'react';
import { ApiError, type Category, type Page, type Recipe, recipeApi } from '../api/recipeClient';
import { useAuth } from '../auth/AuthContext';
import { DomainSelector } from './DomainSelector';
import { RecipeEditor } from './RecipeEditor';
import { RecipeListPage } from './RecipeListPage';
import { useActiveDomain } from './useActiveDomain';

const emptyRecipe: Partial<Recipe> = {
  name: '',
  description: '',
  authorName: '',
  ingredients: '',
  instructions: '',
  servings: 1,
  isApproved: false,
  recipeCategoryId: 0,
};

const emptyPage: Page<Recipe> = { items: [], page: 1, pageSize: 50, totalCount: 0 };

export function RecipeMaintenancePage() {
  const { token } = useAuth();
  const { activeDomainId, selectDomain } = useActiveDomain();
  const [page, setPage] = useState(1);
  const [inventory, setInventory] = useState<Page<Recipe>>(emptyPage);
  const [categories, setCategories] = useState<Category[]>([]);
  const [edit, setEdit] = useState<Partial<Recipe> | null>(null);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState('');

  const load = useCallback(
    async (domainId = activeDomainId, selectedPage = page) => {
      if (!domainId) {
        setInventory(emptyPage);
        setCategories([]);
        return;
      }

      setLoading(true);
      setMessage('');
      try {
        const [recipes, categoryList] = await Promise.all([
          recipeApi.recipes(token, domainId, selectedPage),
          recipeApi.categories(token, domainId),
        ]);
        setInventory(recipes);
        setCategories(categoryList);
      } catch (error) {
        setMessage(error instanceof ApiError && error.status === 401 ? 'Session expired. Sign in again.' : 'Unable to load recipes.');
      } finally {
        setLoading(false);
      }
    },
    [activeDomainId, page, token],
  );

  useEffect(() => {
    void load();
  }, [load]);

  function changeDomain(domainId: number) {
    selectDomain(domainId);
    setPage(1);
    setEdit(null);
  }

  function validateRecipe(recipe: Partial<Recipe>) {
    if (!recipe.name?.trim()) return 'Recipe name is required.';
    if (!recipe.authorName?.trim()) return 'Author is required.';
    if (!recipe.recipeCategoryId) return 'Category is required.';
    if (!recipe.ingredients?.trim()) return 'Ingredients are required.';
    if (!recipe.instructions?.trim()) return 'Instructions are required.';
    return '';
  }

  async function saveRecipe() {
    if (!edit || !activeDomainId) return;
    const validation = validateRecipe(edit);
    if (validation) {
      setMessage(validation);
      return;
    }

    try {
      await recipeApi.saveRecipe(token, activeDomainId, edit);
      setEdit(null);
      await load(activeDomainId, page);
      setMessage('Recipe saved.');
    } catch (error) {
      if (error instanceof ApiError && error.status === 412) setMessage('Recipe changed elsewhere. Reload before saving.');
      else setMessage('Unable to save recipe.');
    }
  }

  async function deleteRecipe(recipe: Recipe) {
    if (!activeDomainId || !window.confirm(`Delete ${recipe.name}?`)) return;
    try {
      await recipeApi.deleteRecipe(token, activeDomainId, recipe);
      await load(activeDomainId, page);
      setMessage('Recipe deleted.');
    } catch (error) {
      if (error instanceof ApiError && error.status === 412) setMessage('Recipe changed elsewhere. Reload before deleting.');
      else setMessage('Unable to delete recipe.');
    }
  }

  async function goToPage(nextPage: number) {
    setPage(nextPage);
    await load(activeDomainId, nextPage);
  }

  return (
    <>
      <header className="page-header">
        <h1>Recipe Maintenance</h1>
        <button type="button" onClick={() => setEdit({ ...emptyRecipe })} disabled={!activeDomainId}>
          New recipe
        </button>
      </header>
      <DomainSelector token={token} value={activeDomainId} onChange={changeDomain} onError={setMessage} />
      {message && <p role="alert">{message}</p>}
      {activeDomainId > 0 && (
        <RecipeListPage
          recipes={inventory.items}
          page={inventory.page}
          pageSize={inventory.pageSize}
          totalCount={inventory.totalCount}
          loading={loading}
          onEdit={(recipe) => setEdit(recipe)}
          onDelete={(recipe) => void deleteRecipe(recipe)}
          onPrevious={() => void goToPage(page - 1)}
          onNext={() => void goToPage(page + 1)}
        />
      )}
      {edit && (
        <RecipeEditor
          recipe={edit}
          categories={categories}
          onChange={setEdit}
          onSave={() => void saveRecipe()}
          onCancel={() => setEdit(null)}
        />
      )}
    </>
  );
}
