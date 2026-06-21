import { createHash } from "node:crypto";
import { cp, mkdir, readdir, readFile, rm, writeFile } from "node:fs/promises";
import path from "node:path";
import { spawn } from "node:child_process";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const contentRoot = path.join(root, "src", "content", "articles");
const outputRoot = path.join(root, "test", "output");
const fixtureRoot = path.join(root, "test", "fixtures");

function runBuild() {
  return new Promise((resolve) => {
    const child = spawn(process.execPath, ["scripts/build-and-swap.mjs", "--output-root", "test/output"], { cwd: root, stdio: "inherit" });
    child.on("exit", (code) => resolve(code));
  });
}

async function digest(target) {
  const hash = createHash("sha256");
  async function visit(current) {
    for (const entry of (await readdir(current, { withFileTypes: true })).sort((a, b) => a.name.localeCompare(b.name))) {
      const full = path.join(current, entry.name);
      hash.update(path.relative(outputRoot, full));
      if (entry.isDirectory()) await visit(full);
      else hash.update(await readFile(full));
    }
  }
  await visit(target);
  return hash.digest("hex");
}

async function withFixtures(fixtures, action) {
  const added = [];
  try {
    for (const fixture of fixtures) {
      const destination = path.join(contentRoot, "__test__", path.basename(path.dirname(fixture)), path.basename(fixture));
      await mkdir(path.dirname(destination), { recursive: true });
      await cp(fixture, destination);
      added.push(path.join(contentRoot, "__test__"));
    }
    return await action();
  } finally {
    await rm(path.join(contentRoot, "__test__"), { recursive: true, force: true });
  }
}

await rm(outputRoot, { recursive: true, force: true });
if (await runBuild() !== 0) throw new Error("Normal build must exit 0.");

const targets = [path.join(outputRoot, "insights"), path.join(outputRoot, "systems"), path.join(outputRoot, "assets", "makebold")];
const before = await Promise.all(targets.map(digest));
const unknown = await withFixtures([path.join(fixtureRoot, "unknown-system-reference.md")], runBuild);
if (unknown === 0) throw new Error("Unknown-system fixture must exit non-zero.");
const after = await Promise.all(targets.map(digest));
if (before.join(":") !== after.join(":")) throw new Error("Failed build changed generated output.");

const duplicate = await withFixtures([
  path.join(fixtureRoot, "duplicate-output", "first", "collision.md"),
  path.join(fixtureRoot, "duplicate-output", "second", "collision.md")
], runBuild);
if (duplicate === 0) throw new Error("Duplicate-output fixtures must exit non-zero.");
