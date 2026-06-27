import { useEffect, useState } from 'react';
import { type Domain, recipeApi } from '../api/recipeClient';

type DomainSelectorProps = {
  token: string;
  value: number;
  onChange: (domainId: number) => void;
  onError: (message: string) => void;
};

export function DomainSelector({ token, value, onChange, onError }: DomainSelectorProps) {
  const [domains, setDomains] = useState<Domain[]>([]);

  useEffect(() => {
    recipeApi.domains(token).then(setDomains).catch(() => onError('Unable to load domains.'));
  }, [token, onError]);

  return (
    <label className="field">
      <span>Domain</span>
      <select value={value} onChange={(event) => onChange(Number(event.target.value))}>
        <option value={0}>Select a domain</option>
        {domains.map((domain) => (
          <option key={domain.id} value={domain.id}>
            {domain.name}
          </option>
        ))}
      </select>
    </label>
  );
}
