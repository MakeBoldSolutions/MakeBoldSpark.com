import { spawn } from "node:child_process";
import { access, cp, mkdir, mkdtemp, rename, rm, stat } from "node:fs/promises";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

const projectRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const apiWwwroot = path.resolve(projectRoot, "..", "MakeBoldSpark.Api", "wwwroot");
const outputArgument = process.argv.indexOf("--output-root");
const outputRoot = outputArgument >= 0
  ? path.resolve(projectRoot, process.argv[outputArgument + 1])
  : apiWwwroot;
const stageRoot = await mkdtemp(path.join(os.tmpdir(), "makeboldspark-eleventy-"));
const backupRoot = await mkdtemp(path.join(os.tmpdir(), "makeboldspark-backup-"));
const targets = ["insights", "systems", path.join("assets", "makebold", "catalog.json")];

function run(command, args) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, { cwd: projectRoot, stdio: "inherit" });
    child.on("error", reject);
    child.on("exit", (code) => code === 0 ? resolve() : reject(new Error(`Eleventy exited with ${code}`)));
  });
}

async function exists(target) {
  try { await access(target); return true; } catch { return false; }
}

async function move(source, destination) {
  await mkdir(path.dirname(destination), { recursive: true });
  await rename(source, destination);
}

async function restore(movedTargets) {
  for (const relative of targets) {
    const target = path.join(outputRoot, relative);
    if (await exists(target)) await rm(target, { recursive: true, force: true });
  }
  for (const relative of movedTargets) {
    const backup = path.join(backupRoot, relative);
    if (await exists(backup)) await move(backup, path.join(outputRoot, relative));
  }
}

try {
  await run(process.execPath, [path.join(projectRoot, "node_modules", "@11ty", "eleventy", "cmd.cjs"), "--output", stageRoot]);
  for (const relative of targets) {
    const staged = path.join(stageRoot, relative);
    if (!(await exists(staged))) throw new Error(`Eleventy did not produce required output: ${relative}`);
  }

  const movedTargets = [];
  try {
    for (const relative of targets) {
      const target = path.join(outputRoot, relative);
      if (await exists(target)) {
        await move(target, path.join(backupRoot, relative));
        movedTargets.push(relative);
      }
    }
    for (const relative of targets) {
      await move(path.join(stageRoot, relative), path.join(outputRoot, relative));
    }
  } catch (error) {
    await restore(movedTargets);
    throw error;
  }
} finally {
  await rm(stageRoot, { recursive: true, force: true });
  await rm(backupRoot, { recursive: true, force: true });
}
