import { readdir, readFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const baseline = path.join(root, "test", "baselines", "pre-migration");
const output = path.resolve(root, "..", "MakeBoldSpark.Api", "wwwroot");
const normalize = (html) => html.replace(/\s+/g, " ").trim();

async function files(directory) {
  const result = [];
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    const full = path.join(directory, entry.name);
    if (entry.isDirectory()) result.push(...await files(full));
    else if (entry.name.endsWith(".html")) result.push(full);
  }
  return result;
}

const failures = [];
for (const source of await files(baseline)) {
  const relative = path.relative(baseline, source);
  const generated = path.join(output, relative);
  try {
    const [before, after] = await Promise.all([readFile(source, "utf8"), readFile(generated, "utf8")]);
    if (normalize(before) !== normalize(after)) failures.push(relative);
  } catch {
    failures.push(relative);
  }
}
if (failures.length) throw new Error(`Migration parity mismatch: ${failures.join(", ")}`);
