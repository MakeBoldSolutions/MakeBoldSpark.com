import { useEffect, useState, type ReactNode } from 'react';
import type { BaseEntity } from '../api/types';
import { adminCrud, ApiError } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { RichTextField } from './overrides/RichTextField';
import { MaskedCredentialField } from './overrides/MaskedCredentialField';
import { MarkdownField } from './overrides/MarkdownField';

export type FieldType = 'text' | 'textarea' | 'number' | 'boolean' | 'password' | 'richtext' | 'markdown' | 'select' | 'menuParent' | 'maskedCredential';

export interface FieldConfig<T> {
  key: keyof T;
  label: string;
  type: FieldType;
  required?: boolean;
  /** Excluded entirely from the create/edit form (e.g. Author.password rendered separately, never round-tripped as plain text — FR-006). */
  hiddenInForm?: boolean;
  /** Makes a field span the entire responsive form grid. */
  layout?: 'full';
  options?: { label: string; value: string | number }[];
  /** Custom list-column rendering, e.g. Category.content instead of a non-existent .name. */
  renderColumn?: (record: T) => ReactNode;
}

export interface EntityConfig<T extends BaseEntity> {
  resource: string;
  label: string;
  columns: FieldConfig<T>[];
  fields: FieldConfig<T>[];
  defaultValues: Omit<T, keyof BaseEntity>;
  allowEdit?: boolean;
  allowDelete?: boolean;
  /** False for Subscribers/Newsletters/Mail Configuration, whose GET also requires Admin — see client.ts's adminCrud. Defaults to true (anonymous read, admin write). */
  publicRead?: boolean;
}

export interface EntityListRenderProps<T extends BaseEntity> {
  records: T[];
  onEdit: (record: T) => void;
  onDelete: (record: T) => void;
}

interface EntityCrudPageProps<T extends BaseEntity> {
  config: EntityConfig<T>;
  /** Filters the list call, e.g. { blogId } to scope Posts to a selected Blog (FR-011). Re-fetches when its values change. */
  listQuery?: Record<string, string | number>;
  /** Replaces the default flat table with a custom list view (e.g. MenuTree's hierarchy) while still reusing this component's create/edit/delete form logic. */
  renderList?: (props: EntityListRenderProps<T>) => ReactNode;
}

const DEPENDENT_DELETE_MESSAGE = 'This record cannot be deleted because other records still depend on it.';
const INVALID_REFERENCE_MESSAGE = 'This could not be saved because it references a record that does not exist (check the selections above).';

export function EntityCrudPage<T extends BaseEntity>({ config, listQuery, renderList }: EntityCrudPageProps<T>) {
  const { session, markSessionExpired } = useAuth();
  const crud = adminCrud<T>(config.resource, { publicRead: config.publicRead });
  const token = session?.token ?? '';

  const [records, setRecords] = useState<T[]>([]);
  const [loading, setLoading] = useState(true);
  const [listError, setListError] = useState<string | null>(null);
  const [editing, setEditing] = useState<Partial<T> | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    let cancelled = false;
    // This state transition represents the start of an asynchronous request. It keeps the
    // previous list from being presented as current while a different resource is loading.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setLoading(true);
    crud
      .list(token, listQuery)
      .then((data) => {
        if (!cancelled) setRecords(data);
      })
      .catch((err) => {
        if (cancelled) return;
        if (err instanceof ApiError && err.status === 401) markSessionExpired();
        setListError('Could not load this list.');
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [config.resource, JSON.stringify(listQuery)]);

  function startCreate() {
    setFormError(null);
    // A selector-scoped editor (Posts/Menus) owns the foreign key; do not make authors
    // re-enter it or accidentally create a record under the wrong parent resource.
    setEditing({ ...config.defaultValues, ...listQuery } as Partial<T>);
  }

  function startEdit(record: T) {
    setFormError(null);
    setEditing(record);
  }

  function cancelEdit() {
    setEditing(null);
    setFormError(null);
  }

  function updateField(key: keyof T, value: unknown) {
    setEditing((prev) => (prev ? { ...prev, [key]: value } : prev));
  }

  async function handleSave() {
    if (!editing) return;
    setSaving(true);
    setFormError(null);
    try {
      if ('id' in editing && editing.id != null) {
        const updated = await crud.update(token, editing.id, editing as Partial<T>);
        setRecords((prev) => prev.map((r) => (r.id === updated.id ? updated : r)));
      } else {
        const created = await crud.create(token, editing as Omit<T, keyof BaseEntity>);
        setRecords((prev) => [...prev, created]);
      }
      setEditing(null);
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        // Session expired mid-edit: surface the re-auth prompt (AuthContext/CmsLayout) without
        // discarding the open form — `editing` state is left untouched (gate finding analyze-E2).
        markSessionExpired();
        return;
      }
      if (err instanceof ApiError) {
        setFormError(err.status === 415
          ? 'The CMS could not send this save request. Refresh the page and try again.'
          : `The server could not save this record (HTTP ${err.status}).`);
        return;
      }
      setFormError(INVALID_REFERENCE_MESSAGE);
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(record: T) {
    if (!window.confirm(`Delete this ${config.label.toLowerCase()}? This cannot be undone.`)) return;
    try {
      await crud.delete(token, record.id);
      setRecords((prev) => prev.filter((r) => r.id !== record.id));
      setEditing(null);
      setFormError(null);
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        markSessionExpired();
        return;
      }
      // Same undifferentiated 500 from the API, but on the delete path it means dependent
      // child records block the delete (FR-012) — a different explanation than an invalid
      // reference on save, even though the underlying HTTP status is identical.
      window.alert(DEPENDENT_DELETE_MESSAGE);
    }
  }

  if (loading) return <p>Loading {config.label.toLowerCase()}…</p>;

  if (editing) {
    const isExistingRecord = 'id' in editing && editing.id != null;

    return (
      <div className="cms-entity-form">
        <h2>{'id' in editing && editing.id != null ? `Edit ${config.label}` : `New ${config.label}`}</h2>
        {isExistingRecord && 'updatedDate' in editing && (
          <p className="cms-entity-meta">
            Last changed: {new Date((editing as T).updatedDate).toLocaleString()}
          </p>
        )}
        <form
          onSubmit={(e) => {
            e.preventDefault();
            handleSave();
          }}
        >
          {config.fields
            .filter((f) => !f.hiddenInForm)
            .map((field) => (
              <FormField
                key={String(field.key)}
                field={field}
                value={(editing as Record<string, unknown>)[field.key as string]}
                onChange={(value) => updateField(field.key, value)}
                menuParentOptions={field.type === 'menuParent'
                  ? records
                    .filter((record) => record.id !== editing.id)
                    .map((record) => ({
                      label: String((record as Record<string, unknown>).title ?? `Menu item ${record.id}`),
                      value: record.id,
                    }))
                  : undefined}
              />
            ))}
          {formError && <p role="alert" className="cms-form-error">{formError}</p>}
          <div className="cms-form-actions">
            <button type="submit" disabled={saving}>
              {saving ? 'Saving…' : 'Save'}
            </button>
            <button type="button" onClick={cancelEdit} disabled={saving}>
              Cancel
            </button>
          </div>
        </form>
        {isExistingRecord && config.allowDelete !== false && (
          <div className="cms-destructive-actions">
            <p>Deleting this record cannot be undone.</p>
            <button type="button" onClick={() => handleDelete(editing as T)}>
              Delete {config.label}
            </button>
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="cms-entity-list">
      <div className="cms-entity-list-header">
        <h2>{config.label}</h2>
        <button type="button" onClick={startCreate}>
          New {config.label}
        </button>
      </div>
      {listError && <p role="alert">{listError}</p>}
      {renderList ? (
        renderList({ records, onEdit: startEdit, onDelete: handleDelete })
      ) : (
        <table>
          <thead>
            <tr>
              {config.columns.map((col) => (
                <th key={String(col.key)}>{col.label}</th>
              ))}
              {config.allowEdit === false && <th>Actions</th>}
            </tr>
          </thead>
          <tbody>
            {records.map((record) => (
              <tr
                key={record.id}
                className={config.allowEdit !== false ? 'cms-editable-row' : undefined}
                tabIndex={config.allowEdit !== false ? 0 : undefined}
                onClick={config.allowEdit !== false ? () => startEdit(record) : undefined}
                onKeyDown={config.allowEdit !== false ? (event) => {
                  if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    startEdit(record);
                  }
                } : undefined}
              >
                {config.columns.map((col) => (
                  <td key={String(col.key)}>
                    {col.renderColumn ? col.renderColumn(record) : String((record as Record<string, unknown>)[col.key as string] ?? '')}
                  </td>
                ))}
                {config.allowEdit === false && (
                  <td>
                    {config.allowDelete !== false && (
                    <button type="button" onClick={() => handleDelete(record)}>
                      Delete
                    </button>
                    )}
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

function FormField({
  field,
  value,
  onChange,
  menuParentOptions,
}: {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  field: FieldConfig<any>;
  value: unknown;
  onChange: (value: unknown) => void;
  menuParentOptions?: { label: string; value: number }[];
}) {
  const id = `field-${String(field.key)}`;
  const fieldClassName = field.layout === 'full' ? 'cms-field cms-field--full' : 'cms-field';

  switch (field.type) {
    case 'markdown':
      return (
        <MarkdownField
          id={id}
          label={field.label}
          value={String(value ?? '')}
          required={field.required}
          onChange={onChange}
        />
      );
    case 'richtext':
      return (
        <RichTextField id={id} label={field.label} value={String(value ?? '')} onChange={onChange} />
      );
    case 'maskedCredential':
      return (
        <MaskedCredentialField
          id={id}
          label={field.label}
          value={String(value ?? '')}
          required={field.required}
          onChange={onChange}
        />
      );
    case 'boolean':
      return (
        <label htmlFor={id} className={`${fieldClassName} cms-field-boolean`}>
          <input
            id={id}
            type="checkbox"
            checked={Boolean(value)}
            onChange={(e) => onChange(e.target.checked)}
          />
          {field.label}
        </label>
      );
    case 'number':
      return (
        <label htmlFor={id} className={fieldClassName}>
          {field.label}
          <input
            id={id}
            type="number"
            required={field.required}
            value={value == null ? '' : Number(value)}
            onChange={(e) => onChange(e.target.valueAsNumber)}
          />
        </label>
      );
    case 'textarea':
      return (
        <label htmlFor={id} className={fieldClassName}>
          {field.label}
          <textarea
            id={id}
            required={field.required}
            value={String(value ?? '')}
            onChange={(e) => onChange(e.target.value)}
          />
        </label>
      );
    case 'password':
      return (
        <label htmlFor={id} className={fieldClassName}>
          {field.label}
          <input
            id={id}
            type="password"
            autoComplete="new-password"
            required={field.required}
            value={String(value ?? '')}
            onChange={(e) => onChange(e.target.value)}
          />
        </label>
      );
    case 'select':
      return (
        <label htmlFor={id} className={fieldClassName}>
          {field.label}
          <select id={id} value={String(value ?? '')} onChange={(e) => onChange(e.target.value)}>
            <option value="" disabled>
              Select…
            </option>
            {field.options?.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
        </label>
      );
    case 'menuParent':
      return (
        <label htmlFor={id} className={fieldClassName}>
          {field.label}
          <select
            id={id}
            value={value == null ? '' : String(value)}
            onChange={(e) => onChange(e.target.value === '' ? null : Number(e.target.value))}
          >
            <option value="">Top level</option>
            {menuParentOptions?.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
      );
    default:
      return (
        <label htmlFor={id} className={fieldClassName}>
          {field.label}
          <input
            id={id}
            type="text"
            required={field.required}
            value={String(value ?? '')}
            onChange={(e) => onChange(e.target.value)}
          />
        </label>
      );
  }
}
