import { cp, mkdir, rm } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const webroot = path.resolve(root, "..", "MakeBoldSpark.Api", "wwwroot");
const baseline = path.join(root, "test", "baselines", "pre-migration");

await rm(baseline, { recursive: true, force: true });
await mkdir(baseline, { recursive: true });
for (const directory of ["insights", "systems"]) {
  await cp(path.join(webroot, directory), path.join(baseline, directory), { recursive: true });
}
