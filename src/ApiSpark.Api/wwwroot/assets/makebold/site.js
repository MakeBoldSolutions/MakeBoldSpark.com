const catalogPromise = fetch("/assets/makebold/catalog.json").then((response) => {
  if (!response.ok) throw new Error("Unable to load Make Bold Spark catalog.");
  return response.json();
});

function bySystem(catalog, systemId) {
  return catalog.articles.filter((article) => article.system === systemId);
}

function systemName(catalog, systemId) {
  return catalog.systems.find((system) => system.id === systemId)?.name || systemId;
}

function articleCard(article, catalog) {
  const tags = article.tags.map((tag) => `<span class="tag">${tag}</span>`).join("");
  return `
    <article class="card article-card">
      <div class="meta-row">
        <span class="tag">${systemName(catalog, article.system)}</span>
        <span class="tag">${article.category}</span>
      </div>
      <h3><a href="${article.url}">${article.title}</a></h3>
      <p>${article.summary}</p>
      <div class="article-date">${new Date(article.published).toLocaleDateString("en-US", {
        year: "numeric",
        month: "short",
        day: "numeric",
      })}</div>
      <div class="meta-row">${tags}</div>
    </article>`;
}

function systemCard(system, catalog) {
  const related = bySystem(catalog, system.id);
  const relatedLink = related.length
    ? `<a href="/insights/${system.id}/">Related Insights</a>`
    : `<a href="/insights/${system.id}/">Insights</a>`;

  return `
    <article class="card system-card">
      <div class="card-top">
        <div>
          <h3 class="name">${system.name}</h3>
          <div class="tagline">${system.category}</div>
        </div>
      </div>
      <div class="meta-row">
        <span class="tag">${system.status}</span>
        <span class="tag">${system.role}</span>
      </div>
      <p>${system.summary}</p>
      <p class="system-purpose"><strong>Purpose:</strong> ${system.purpose}</p>
      <div class="links">
        <a href="/systems/${system.id}/">Explore ${system.name}</a>
        <a href="${system.githubUrl}" target="_blank" rel="noopener">GitHub</a>
        <a href="${system.docsUrl}">Docs</a>
        ${relatedLink}
      </div>
    </article>`;
}

function renderSystems(targetId, options = {}) {
  catalogPromise.then((catalog) => {
    const target = document.getElementById(targetId);
    if (!target) return;
    const systems = options.limit ? catalog.systems.slice(0, options.limit) : catalog.systems;
    target.innerHTML = systems.map((system) => systemCard(system, catalog)).join("");
  });
}

function renderInsightsIndex(targetId) {
  catalogPromise.then((catalog) => {
    const target = document.getElementById(targetId);
    if (!target) return;
    const featured = catalog.articles.filter((article) => article.featured);
    const byTopic = catalog.topics
      .map((topic) => {
        const articles = catalog.articles.filter(
          (article) => article.category === topic || article.tags.includes(topic),
        );
        if (!articles.length) return "";
        return `
          <section class="category">
            <div class="category-header">
              <h3>${topic}</h3>
              <p>${articles.length} article${articles.length === 1 ? "" : "s"}</p>
            </div>
            <div class="grid">${articles.map((article) => articleCard(article, catalog)).join("")}</div>
          </section>`;
      })
      .join("");

    target.innerHTML = `
      <section class="category">
        <div class="category-header">
          <h3>Featured Articles</h3>
          <p>Selected writing for new visitors.</p>
        </div>
        <div class="grid">${featured.map((article) => articleCard(article, catalog)).join("")}</div>
      </section>
      <section class="category">
        <div class="category-header">
          <h3>By System</h3>
          <p>Follow the articles connected to each Make Bold Spark system.</p>
        </div>
        <div class="grid">
          ${catalog.systems
            .map(
              (system) => `
                <article class="card">
                  <h3><a href="/insights/${system.id}/">${system.name}</a></h3>
                  <p>${system.summary}</p>
                  <div class="links"><a href="/insights/${system.id}/">Open ${system.name} Insights</a></div>
                </article>`,
            )
            .join("")}
        </div>
      </section>
      ${byTopic}
      <section class="category">
        <div class="category-header">
          <h3>Recent Articles</h3>
          <p>Newest published insights, without making chronology the whole experience.</p>
        </div>
        <div class="grid">
          ${[...catalog.articles]
            .sort((a, b) => new Date(b.published) - new Date(a.published))
            .map((article) => articleCard(article, catalog))
            .join("")}
        </div>
      </section>`;
  });
}

function renderSystemInsights(targetId, systemId) {
  catalogPromise.then((catalog) => {
    const target = document.getElementById(targetId);
    if (!target) return;
    const articles = bySystem(catalog, systemId);
    target.innerHTML = articles.length
      ? articles.map((article) => articleCard(article, catalog)).join("")
      : `<article class="card"><h3>Insights are being prepared</h3><p>Migrated Markdown articles for ${systemName(catalog, systemId)} will appear here as they are published.</p></article>`;
  });
}

function renderSystemPage(targetId, systemId) {
  catalogPromise.then((catalog) => {
    const system = catalog.systems.find((item) => item.id === systemId);
    const target = document.getElementById(targetId);
    if (!system || !target) return;
    const articles = bySystem(catalog, systemId);
    target.innerHTML = `
      <section class="section split">
        <div>
          <p class="section-label">What It Is</p>
          <h2 class="section-title">${system.name}</h2>
          <p class="section-desc">${system.whatItIs}</p>
        </div>
        <div class="brand-panel">
          <p>${system.whyItExists}</p>
          <div class="meta-row">
            <span class="tag">${system.status}</span>
            <span class="tag">${system.role}</span>
            <span class="tag">${system.category}</span>
          </div>
        </div>
      </section>
      <div class="band">
        <section class="band-inner">
          <p class="section-label">Capabilities</p>
          <h2 class="section-title">What ${system.name} supports</h2>
          <div class="grid">${system.capabilities
            .map((capability) => `<div class="pill">${capability}</div>`)
            .join("")}</div>
        </section>
      </div>
      <section class="section split">
        <div>
          <p class="section-label">Design Philosophy</p>
          <h2 class="section-title">Built for practical use</h2>
        </div>
        <div class="brand-panel"><p>${system.philosophy}</p></div>
      </section>
      <section class="section">
        <p class="section-label">Related Articles</p>
        <h2 class="section-title">Insights for ${system.name}</h2>
        <div class="grid">${
          articles.length
            ? articles.map((article) => articleCard(article, catalog)).join("")
            : `<article class="card"><h3>Articles coming soon</h3><p>Markdown articles related to ${system.name} will appear here after migration.</p></article>`
        }</div>
      </section>
      <div class="band">
        <section class="band-inner split">
          <div>
            <p class="section-label">Next Steps</p>
            <h2 class="section-title">Keep exploring</h2>
          </div>
          <div class="links link-stack">
            <a href="${system.githubUrl}" target="_blank" rel="noopener">Open ${system.name} on GitHub</a>
            <a href="/insights/${system.id}/">Read ${system.name} insights</a>
            <a href="/systems/">Back to all systems</a>
          </div>
        </section>
      </div>`;
  });
}
