import { spawn } from "node:child_process";
import { fileURLToPath } from "node:url";
import { dirname, resolve } from "node:path";

const scriptsDirectory = dirname(fileURLToPath(import.meta.url));
const webDirectory = resolve(scriptsDirectory, "..");
const repositoryRoot = resolve(webDirectory, "..", "..");
const lintExecutable = resolve(
  webDirectory,
  "node_modules",
  "markdownlint-cli2",
  "markdownlint-cli2-bin.mjs"
);
const lintConfiguration = resolve(repositoryRoot, ".markdownlint-cli2.yaml");

const lint = spawn(process.execPath, [lintExecutable, "--config", lintConfiguration], {
  cwd: repositoryRoot,
  stdio: "inherit"
});

lint.on("exit", (code) => process.exit(code ?? 1));
lint.on("error", (error) => {
  console.error(error);
  process.exit(1);
});
