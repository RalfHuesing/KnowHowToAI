import { build } from "esbuild";
import { rm } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const frontendDirectory = dirname(fileURLToPath(import.meta.url));
const serverDirectory = resolve(frontendDirectory, "..");
const outputDirectory = resolve(serverDirectory, "wwwroot/generated/content-editor");

await rm(outputDirectory, { recursive: true, force: true });

await build({
  absWorkingDir: frontendDirectory,
  entryPoints: [resolve(serverDirectory, "Web/Features/Content/content-editor.js")],
  bundle: true,
  format: "esm",
  platform: "browser",
  target: "es2022",
  nodePaths: [resolve(frontendDirectory, "node_modules")],
  outfile: resolve(outputDirectory, "content-editor.js"),
  legalComments: "eof",
  sourcemap: false,
  minify: true,
});
