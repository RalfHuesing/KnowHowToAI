import { resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { defineConfig } from "vitest/config";

const frontendDirectory = fileURLToPath(new URL(".", import.meta.url));

export default defineConfig({
    resolve: {
        alias: {
            "/generated/content-editor/content-editor.js": resolve(frontendDirectory, "tests/fake-generated-editor.js")
        }
    },
    test: {
        environment: "node",
        include: ["tests/**/*.test.js"]
    }
});
