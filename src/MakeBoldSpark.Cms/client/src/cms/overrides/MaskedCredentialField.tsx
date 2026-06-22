import { useState } from 'react';

interface MaskedCredentialFieldProps {
  id: string;
  label: string;
  value: string;
  required?: boolean;
  onChange: (value: string) => void;
}

/** Masked-by-default credential input (FR-005/FR-006) — reveals plaintext only via an explicit click, never on load. */
export function MaskedCredentialField({ id, label, value, required, onChange }: MaskedCredentialFieldProps) {
  const [revealed, setRevealed] = useState(false);

  return (
    <div className="cms-field cms-field-masked-credential">
      <label htmlFor={id}>{label}</label>
      <input
        id={id}
        type={revealed ? 'text' : 'password'}
        autoComplete="new-password"
        required={required}
        value={value}
        onChange={(e) => onChange(e.target.value)}
      />
      <button type="button" onClick={() => setRevealed((prev) => !prev)}>
        {revealed ? 'Hide' : 'Reveal'}
      </button>
    </div>
  );
}
