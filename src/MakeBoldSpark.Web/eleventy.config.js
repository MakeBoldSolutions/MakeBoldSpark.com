import systems from "./src/_data/systems.json" with { type: "json" };

export default function (eleventyConfig) {
  eleventyConfig.addFilter("displayDate", (value) => new Intl.DateTimeFormat("en-US", { year: "numeric", month: "short", day: "numeric", timeZone: "UTC" }).format(new Date(value)));
  eleventyConfig.addFilter("systemById", (systems, id) => systems.find((system) => system.id === id));
  eleventyConfig.addFilter("json", (value) => JSON.stringify(value, null, 2));
  eleventyConfig.addCollection("articles", (collectionApi) => {
    const articles = collectionApi.getFilteredByGlob("./src/content/articles/**/*.md");
    const knownSystems = new Set(systems.map((system) => system.id));
    for (const article of articles) {
      if (!knownSystems.has(article.data.system)) {
        throw new Error(`Unknown system '${article.data.system}' in ${article.inputPath}`);
      }
    }
    return articles.sort((left, right) => new Date(right.data.published) - new Date(left.data.published));
  });
  return {
    dir: { input: "src", output: "_site", includes: "_includes", data: "_data" },
    templateFormats: ["md", "njk"],
    markdownTemplateEngine: "njk",
    htmlTemplateEngine: "njk"
  };
}
