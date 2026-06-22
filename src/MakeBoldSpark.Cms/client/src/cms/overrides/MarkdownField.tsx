import { lazy, Suspense, useState } from 'react';

const MarkdownPreview = lazy(() => import('./MarkdownPreview'));

interface MarkdownFieldProps {
  id: string;
  label: string;
  value: string;
  required?: boolean;
  onChange: (value: string) => void;
}

/** A source-first Markdown editor with sanitized inline HTML and constrained YouTube embeds. */
export function MarkdownField({ id, label, value, required, onChange }: MarkdownFieldProps) {
  const [previewing, setPreviewing] = useState(false);

  return (
    <div className="cms-field cms-field--full cms-markdown-field">
      <div className="cms-markdown-heading">
        <label htmlFor={id}>{label}</label>
        <button type="button" onClick={() => setPreviewing((current) => !current)} aria-pressed={previewing}>
          {previewing ? 'Edit Markdown' : 'Preview Markdown'}
        </button>
      </div>
      {previewing ? (
        <div className="cms-markdown-preview" aria-label={`${label} preview`}>
          <Suspense fallback={<p>Loading preview…</p>}>
            <MarkdownPreview content={value || '_Nothing to preview yet._'} />
          </Suspense>
        </div>
      ) : (
        <textarea
          id={id}
          required={required}
          value={value}
          onChange={(event) => onChange(event.target.value)}
          spellCheck
        />
      )}
    </div>
  );
}
