import { useCallback, useState } from 'react';

const ActiveDomainKey = 'mbs.recipe.domain';

export function useActiveDomain() {
  const [activeDomainId, setActiveDomainId] = useState(() => Number(sessionStorage.getItem(ActiveDomainKey) ?? 0));

  const selectDomain = useCallback((domainId: number) => {
    setActiveDomainId(domainId);
    if (domainId > 0) sessionStorage.setItem(ActiveDomainKey, String(domainId));
    else sessionStorage.removeItem(ActiveDomainKey);
  }, []);

  return { activeDomainId, selectDomain };
}
