import { useEffect, useRef, useState, type ClipboardEvent, type KeyboardEvent } from 'react';
import { type Category, type Recipe } from '../api/recipeClient';

type RecipeWizardProps = {
  initial: Partial<Recipe>;
  categories: Category[];
  defaultAuthor: string;
  onSave: (recipe: Partial<Recipe>) => void;
  onCancel: () => void;
};

const stepNames = ['Basics', 'Ingredients', 'Steps', 'Review'] as const;
const lastInputStep = 2;

function splitLines(text?: string) {
  return (text ?? '').split(/\r?\n/).map((line) => line.trim()).filter(Boolean);
}

function nonEmpty(items: string[]) {
  return items.map((item) => item.replace(/\s*\r?\n\s*/g, ' ').trim()).filter(Boolean);
}

type LineListProps = {
  label: string;
  items: string[];
  onChange: (items: string[]) => void;
  placeholder: string;
  multiline?: boolean;
  numbered?: boolean;
  addLabel: string;
};

function LineList({ label, items, onChange, placeholder, multiline, numbered, addLabel }: LineListProps) {
  const listRef = useRef<HTMLUListElement>(null);
  const pendingFocus = useRef<number | null>(null);

  useEffect(() => {
    if (pendingFocus.current === null) return;
    const fields = listRef.current?.querySelectorAll<HTMLElement>('input, textarea');
    fields?.[pendingFocus.current]?.focus();
    pendingFocus.current = null;
  });

  function update(index: number, value: string) {
    onChange(items.map((item, i) => (i === index ? value : item)));
  }

  function insertAfter(index: number) {
    const next = [...items];
    next.splice(index + 1, 0, '');
    pendingFocus.current = index + 1;
    onChange(next);
  }

  function remove(index: number) {
    const next = items.filter((_, i) => i !== index);
    onChange(next.length ? next : ['']);
  }

  function move(index: number, delta: number) {
    const target = index + delta;
    if (target < 0 || target >= items.length) return;
    const next = [...items];
    [next[index], next[target]] = [next[target], next[index]];
    onChange(next);
  }

  function onKeyDown(index: number, event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      insertAfter(index);
    }
  }

  function onPaste(index: number, event: ClipboardEvent) {
    const lines = splitLines(event.clipboardData.getData('text'));
    if (lines.length < 2) return;
    event.preventDefault();
    const next = [...items];
    next.splice(index, 1, ...lines);
    onChange(next);
  }

  return (
    <div>
      <ul className="line-list" ref={listRef}>
        {items.map((item, index) => {
          const itemLabel = `${label} ${index + 1}`;
          return (
            <li key={index}>
              <span className="idx" aria-hidden="true">
                {numbered ? index + 1 : '•'}
              </span>
              {multiline ? (
                <textarea
                  aria-label={itemLabel}
                  value={item}
                  placeholder={placeholder}
                  onChange={(event) => update(index, event.target.value)}
                  onKeyDown={(event) => onKeyDown(index, event)}
                  onPaste={(event) => onPaste(index, event)}
                />
              ) : (
                <input
                  aria-label={itemLabel}
                  value={item}
                  placeholder={placeholder}
                  onChange={(event) => update(index, event.target.value)}
                  onKeyDown={(event) => onKeyDown(index, event)}
                  onPaste={(event) => onPaste(index, event)}
                />
              )}
              <div className="line-tools">
                {numbered && (
                  <>
                    <button type="button" className="ghost icon" aria-label={`Move ${itemLabel} up`} disabled={index === 0} onClick={() => move(index, -1)}>
                      ↑
                    </button>
                    <button type="button" className="ghost icon" aria-label={`Move ${itemLabel} down`} disabled={index === items.length - 1} onClick={() => move(index, 1)}>
                      ↓
                    </button>
                  </>
                )}
                <button type="button" className="danger icon" aria-label={`Remove ${itemLabel}`} onClick={() => remove(index)}>
                  ✕
                </button>
              </div>
            </li>
          );
        })}
      </ul>
      <button type="button" className="ghost add-row" onClick={() => insertAfter(items.length - 1)}>
        + {addLabel}
      </button>
    </div>
  );
}

export function RecipeWizard({ initial, categories, defaultAuthor, onSave, onCancel }: RecipeWizardProps) {
  const [step, setStep] = useState(0);
  const [furthest, setFurthest] = useState(0);
  const [error, setError] = useState('');
  const [basics, setBasics] = useState({
    name: initial.name ?? '',
    authorName: initial.authorName || defaultAuthor,
    recipeCategoryId: initial.recipeCategoryId ?? 0,
    servings: initial.servings || 4,
    description: initial.description ?? '',
  });
  const [ingredients, setIngredients] = useState<string[]>(() => {
    const lines = splitLines(initial.ingredients);
    return lines.length ? lines : [''];
  });
  const [method, setMethod] = useState<string[]>(() => {
    const lines = splitLines(initial.instructions);
    return lines.length ? lines : [''];
  });

  function validate(index: number) {
    if (index === 0) {
      if (!basics.name.trim()) return 'Recipe name is required.';
      if (!basics.authorName.trim()) return 'Author is required.';
      if (!basics.recipeCategoryId) return 'Category is required.';
    }
    if (index === 1 && nonEmpty(ingredients).length === 0) return 'Add at least one ingredient.';
    if (index === 2 && nonEmpty(method).length === 0) return 'Add at least one step.';
    return '';
  }

  function goTo(index: number) {
    setError('');
    setStep(index);
    setFurthest((current) => Math.max(current, index));
  }

  function next() {
    const problem = validate(step);
    if (problem) {
      setError(problem);
      return;
    }
    goTo(step + 1);
  }

  function finish(publish: boolean) {
    for (let index = 0; index <= lastInputStep; index += 1) {
      const problem = validate(index);
      if (problem) {
        setStep(index);
        setError(problem);
        return;
      }
    }
    onSave({
      ...initial,
      ...basics,
      name: basics.name.trim(),
      authorName: basics.authorName.trim(),
      description: basics.description.trim(),
      ingredients: nonEmpty(ingredients).join('\n'),
      instructions: nonEmpty(method).join('\n'),
      isApproved: publish,
    });
  }

  const categoryName = categories.find((category) => category.id === basics.recipeCategoryId)?.name ?? '';

  return (
    <section className="wizard" aria-label="New recipe wizard">
      <ol className="stepper" aria-label="Progress">
        {stepNames.map((name, index) => (
          <li key={name} className={index === step ? 'current' : index < step || index < furthest ? 'done' : ''} aria-current={index === step ? 'step' : undefined}>
            <button type="button" disabled={index > furthest} onClick={() => goTo(index)}>
              <span className="num">{index + 1}</span>
              <span className="label">{name}</span>
            </button>
          </li>
        ))}
      </ol>

      <div className="card wizard-card">
        {step === 0 && (
          <>
            <div>
              <p className="eyebrow">Step 1 of 4</p>
              <h2>The basics</h2>
            </div>
            <label className="field">
              <span>Recipe name</span>
              <input value={basics.name} onChange={(event) => setBasics({ ...basics, name: event.target.value })} autoFocus />
            </label>
            <div className="field-row thirds">
              <label className="field">
                <span>Category</span>
                <select value={basics.recipeCategoryId} onChange={(event) => setBasics({ ...basics, recipeCategoryId: Number(event.target.value) })}>
                  <option value={0}>Select category</option>
                  {categories.map((category) => (
                    <option key={category.id} value={category.id}>
                      {category.name}
                    </option>
                  ))}
                </select>
              </label>
              <label className="field">
                <span>Servings</span>
                <input type="number" min={1} value={basics.servings} onChange={(event) => setBasics({ ...basics, servings: Math.max(1, Number(event.target.value) || 1) })} />
              </label>
            </div>
            <label className="field">
              <span>Author</span>
              <input value={basics.authorName} onChange={(event) => setBasics({ ...basics, authorName: event.target.value })} />
            </label>
            <label className="field">
              <span>
                Short description <span className="hint">(optional)</span>
              </span>
              <textarea value={basics.description} onChange={(event) => setBasics({ ...basics, description: event.target.value })} />
            </label>
          </>
        )}

        {step === 1 && (
          <>
            <div>
              <p className="eyebrow">Step 2 of 4</p>
              <h2>Ingredients</h2>
              <p className="hint">One per line. Press Enter to add the next one, or paste a whole list.</p>
            </div>
            <LineList label="Ingredient" items={ingredients} onChange={setIngredients} placeholder="e.g. 2 cups flour" addLabel="Add ingredient" />
          </>
        )}

        {step === 2 && (
          <>
            <div>
              <p className="eyebrow">Step 3 of 4</p>
              <h2>Method</h2>
              <p className="hint">One step at a time. Enter adds a step; Shift+Enter breaks a line.</p>
            </div>
            <LineList label="Step" items={method} onChange={setMethod} placeholder="Describe this step" multiline numbered addLabel="Add step" />
          </>
        )}

        {step === 3 && (
          <div className="review">
            <div>
              <p className="eyebrow">Step 4 of 4</p>
              <h2>Review and publish</h2>
              <p className="hint">Save as a draft to keep working, or publish to make it live.</p>
            </div>
            <section>
              <header>
                <h3>{basics.name}</h3>
                <button type="button" className="ghost" onClick={() => goTo(0)}>
                  Edit basics
                </button>
              </header>
              <dl>
                <dt>Category</dt>
                <dd>{categoryName}</dd>
                <dt>Servings</dt>
                <dd>{basics.servings}</dd>
                <dt>Author</dt>
                <dd>{basics.authorName}</dd>
              </dl>
              {basics.description && <p>{basics.description}</p>}
            </section>
            <section>
              <header>
                <h3>Ingredients</h3>
                <button type="button" className="ghost" onClick={() => goTo(1)}>
                  Edit ingredients
                </button>
              </header>
              <ul>
                {nonEmpty(ingredients).map((item, index) => (
                  <li key={index}>{item}</li>
                ))}
              </ul>
            </section>
            <section>
              <header>
                <h3>Method</h3>
                <button type="button" className="ghost" onClick={() => goTo(2)}>
                  Edit steps
                </button>
              </header>
              <ol>
                {nonEmpty(method).map((item, index) => (
                  <li key={index}>{item}</li>
                ))}
              </ol>
            </section>
          </div>
        )}

        {error && <p role="alert">{error}</p>}
      </div>

      <div className="wizard-footer">
        <button type="button" className="ghost" onClick={onCancel}>
          Cancel
        </button>
        <div className="right">
          {step > 0 && (
            <button type="button" onClick={() => goTo(step - 1)}>
              Back
            </button>
          )}
          {step < stepNames.length - 1 ? (
            <button type="button" className="primary" onClick={next}>
              Next
            </button>
          ) : (
            <>
              <button type="button" onClick={() => finish(false)}>
                Save draft
              </button>
              <button type="button" className="primary" onClick={() => finish(true)}>
                Publish recipe
              </button>
            </>
          )}
        </div>
      </div>
    </section>
  );
}
