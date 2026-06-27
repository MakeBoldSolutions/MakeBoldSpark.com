import { useCallback, useEffect, useState } from 'react';
import { ApiError, type Category, recipeApi } from '../api/recipeClient';
import { useAuth } from '../auth/AuthContext';
import { CategoryEditor } from './CategoryEditor';
import { DomainSelector } from './DomainSelector';
import { useActiveDomain } from './useActiveDomain';

const blankCategory: Partial<Category> = { name: '', description: '', displayOrder: 0, isActive: true };

export function CategoryMaintenancePage() {
  const { token } = useAuth();
  const { activeDomainId, selectDomain } = useActiveDomain();
  const [items, setItems] = useState<Category[]>([]);
  const [edit, setEdit] = useState<Partial<Category> | null>(null);
  const [message, setMessage] = useState('');
  const [loading, setLoading] = useState(false);

  const load = useCallback(
    async (domainId = activeDomainId) => {
      if (!domainId) {
        setItems([]);
        return;
      }

      setLoading(true);
      setMessage('');
      try {
        setItems(await recipeApi.categories(token, domainId));
      } catch {
        setMessage('Unable to load categories.');
      } finally {
        setLoading(false);
      }
    },
    [activeDomainId, token],
  );

  useEffect(() => {
    void load();
  }, [load]);

  function validateCategory(category: Partial<Category>) {
    if (!category.name?.trim()) return 'Category name is required.';
    return '';
  }

  async function saveCategory() {
    if (!edit || !activeDomainId) return;
    const validation = validateCategory(edit);
    if (validation) {
      setMessage(validation);
      return;
    }

    try {
      await recipeApi.saveCategory(token, activeDomainId, edit);
      setEdit(null);
      await load(activeDomainId);
      setMessage('Category saved.');
    } catch (error) {
      if (error instanceof ApiError && error.status === 412) setMessage('Category changed elsewhere. Reload before saving.');
      else setMessage('Unable to save category.');
    }
  }

  async function deleteCategory(category: Category) {
    if (!activeDomainId || !window.confirm(`Delete ${category.name}?`)) return;
    try {
      await recipeApi.deleteCategory(token, activeDomainId, category);
      await load(activeDomainId);
      setMessage('Category deleted.');
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) setMessage('Category is in use. Reassign recipes before deleting it.');
      else if (error instanceof ApiError && error.status === 412) setMessage('Category changed elsewhere. Reload before deleting.');
      else setMessage('Unable to delete category.');
    }
  }

  return (
    <>
      <header className="page-header">
        <h1>Recipe Categories</h1>
        <button type="button" onClick={() => setEdit({ ...blankCategory })} disabled={!activeDomainId}>
          New category
        </button>
      </header>
      <DomainSelector token={token} value={activeDomainId} onChange={selectDomain} onError={setMessage} />
      {message && <p role="alert">{message}</p>}
      {activeDomainId > 0 && (
        <section aria-label="Category inventory">
          {loading && <p>Loading categories...</p>}
          {!loading && items.length === 0 && <p>No categories in this domain.</p>}
          {!loading && items.length > 0 && (
            <ul className="recipe-list">
              {items.map((category) => (
                <li key={category.id}>
                  <button type="button" onClick={() => setEdit(category)}>
                    {category.name}
                  </button>
                  <span>{category.isActive ? 'Active' : 'Inactive'}</span>
                  <button type="button" onClick={() => void deleteCategory(category)}>
                    Delete
                  </button>
                </li>
              ))}
            </ul>
          )}
        </section>
      )}
      {edit && (
        <CategoryEditor
          category={edit}
          onChange={setEdit}
          onSave={() => void saveCategory()}
          onCancel={() => setEdit(null)}
        />
      )}
    </>
  );
}
