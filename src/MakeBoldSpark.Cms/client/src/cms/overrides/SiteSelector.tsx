import { useEffect, useState } from 'react';
import type { WebSite } from '../../api/types';
import { apiFetch } from '../../api/client';
import { MenuTree } from './MenuTree';

/** A Site must be selected before its Menus are shown (FR-011) — Menus aren't scoped without it. */
export function SiteSelector() {
  const [sites, setSites] = useState<WebSite[]>([]);
  const [domainId, setDomainId] = useState<number | null>(null);

  useEffect(() => {
    apiFetch<WebSite[]>('/api/public/makeboldspark/domains').then(setSites);
  }, []);

  return (
    <div className="cms-site-selector">
      <label htmlFor="site-select">Site</label>
      <select
        id="site-select"
        value={domainId ?? ''}
        onChange={(e) => setDomainId(e.target.value ? Number(e.target.value) : null)}
      >
        <option value="">Select a site…</option>
        {sites.map((site) => (
          <option key={site.id} value={site.id}>
            {site.name}
          </option>
        ))}
      </select>

      {domainId != null && <MenuTree domainId={domainId} />}
    </div>
  );
}
