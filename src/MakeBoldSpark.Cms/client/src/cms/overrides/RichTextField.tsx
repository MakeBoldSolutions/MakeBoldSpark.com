import { useRef } from 'react';

interface RichTextFieldProps {
  id: string;
  label: string;
  value: string;
  onChange: (html: string) => void;
}

const COMMANDS: Array<{ command: string; label: string }> = [
  { command: 'bold', label: 'B' },
  { command: 'italic', label: 'I' },
  { command: 'insertUnorderedList', label: '• List' },
  { command: 'createLink', label: 'Link' },
];

/** Format-while-you-type body editor (FR-010) — a contentEditable surface with a small
 * formatting toolbar, syncing its HTML back to the parent form's state on every input. */
export function RichTextField({ id, label, value, onChange }: RichTextFieldProps) {
  const editorRef = useRef<HTMLDivElement>(null);

  function exec(command: string) {
    if (command === 'createLink') {
      const url = window.prompt('Link URL');
      if (!url) return;
      document.execCommand(command, false, url);
    } else {
      document.execCommand(command);
    }
    if (editorRef.current) onChange(editorRef.current.innerHTML);
  }

  return (
    <div className="cms-field cms-field-richtext">
      <label htmlFor={id}>{label}</label>
      <div className="cms-richtext-toolbar" role="toolbar" aria-label={`${label} formatting`}>
        {COMMANDS.map((c) => (
          <button key={c.command} type="button" onClick={() => exec(c.command)}>
            {c.label}
          </button>
        ))}
      </div>
      <div
        id={id}
        ref={editorRef}
        className="cms-richtext-editor"
        contentEditable
        suppressContentEditableWarning
        dangerouslySetInnerHTML={{ __html: value }}
        onInput={() => {
          if (editorRef.current) onChange(editorRef.current.innerHTML);
        }}
      />
    </div>
  );
}
