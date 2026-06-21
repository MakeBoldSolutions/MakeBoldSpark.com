import { spawn } from "node:child_process";
import { readFile, rm, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const contentFile = path.join(root, "src", "content", "articles", "__live-reload-check.md");
const outputFile = path.join(root, "_site", "insights", "devspark", "__live-reload-check", "index.html");
const started = Date.now();
const server = spawn(process.execPath, ["node_modules/@11ty/eleventy/cmd.cjs", "--serve", "--port=8082"], { cwd: root, stdio: "ignore" });

async function waitFor(predicate, timeoutMs) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    if (await predicate()) return;
    await new Promise((resolve) => setTimeout(resolve, 100));
  }
  throw new Error("Preview did not update within the five-second budget.");
}

try {
  await waitFor(async () => {
    try { return (await readFile(path.join(root, "_site", "insights", "index.html"), "utf8")).length > 0; } catch { return false; }
  }, 5000);
  await writeFile(contentFile, `---\ntitle: "Live reload check"\nsummary: "Temporary preview fixture."\nsystem: devspark\ncategory: testing\ntags: ["test"]\npublished: 2026-06-20\nfeatured: false\nlayout: layouts/article.njk\npermalink: "/insights/{{ system }}/{{ page.fileSlug }}/index.html"\n---\n\nPreview update marker.\n`);
  await waitFor(async () => {
    try { return (await readFile(outputFile, "utf8")).includes("Preview update marker."); } catch { return false; }
  }, 5000);
  console.log(`Live preview updated in ${Date.now() - started} ms.`);
} finally {
  await rm(contentFile, { force: true });
  await rm(path.join(root, "_site", "insights", "devspark", "__live-reload-check"), { recursive: true, force: true });
  server.kill();
}
