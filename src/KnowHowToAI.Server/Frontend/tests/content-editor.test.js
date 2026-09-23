import { beforeEach, describe, expect, it, vi } from "vitest";
import { dispose, focus, mount, normalizePastedPlainText, readMarkdown, sanitizePastedHtml } from "../../Web/Features/Content/ContentEditor.razor.js";
import { lastEditor, lifecycle } from "./fake-generated-editor.js";

beforeEach(() => {
    lifecycle.length = 0;
});

describe("ContentEditor JS interop", () => {
    it("mounts, reads and focuses the current instance, then disposes", async () => {
        const focusTarget = { focus: vi.fn() };
        const toolbar = { addEventListener: vi.fn(), removeEventListener: vi.fn(), querySelectorAll: vi.fn(() => []) };
        const element = {
            querySelector: vi.fn(() => focusTarget),
            closest: vi.fn(() => ({ querySelector: () => toolbar }))
        };
        const reference = { invokeMethodAsync: vi.fn().mockResolvedValue(undefined) };

        await mount(element, "Initial", reference, false);

        expect(lifecycle.map(([name]) => name)).toEqual(["construct", "readonly", "bind-toolbar", "create"]);
        expect(lastEditor.options.defaultValue).toBe("Initial");
        expect(lastEditor.options.features).toMatchObject({
            toolbar: false,
            "top-bar": false,
            "image-block": false,
            latex: false,
            ai: false,
            "block-edit": false
        });
        expect(readMarkdown(element)).toBe("Initial");
        focus(element);
        expect(element.querySelector).toHaveBeenCalledWith(".ProseMirror");
        expect(focusTarget.focus).toHaveBeenCalledOnce();

        await dispose(element);
        expect(lifecycle.map(([name]) => name)).toEqual(["construct", "readonly", "bind-toolbar", "create", "read", "unbind-toolbar", "destroy"]);
        expect(readMarkdown(element)).toBe("");
    });

    it("forwards markdown and focus callbacks and makes dispose idempotent", async () => {
        const element = {};
        const reference = { invokeMethodAsync: vi.fn().mockResolvedValue(undefined) };

        await mount(element, "Callback", reference, false);
        lastEditor.markdownUpdated({}, "Callback");
        lastEditor.focused();
        expect(reference.invokeMethodAsync).toHaveBeenNthCalledWith(1, "NotifyChangedAsync", "Callback");
        expect(reference.invokeMethodAsync).toHaveBeenNthCalledWith(2, "NotifyFocusAsync");

        await dispose(element);
        await dispose(element);
        expect(lifecycle.filter(([name]) => name === "destroy")).toHaveLength(1);
    });

    it("returns an empty value and does not throw for a missing instance", async () => {
        const element = {};

        expect(readMarkdown(element)).toBe("");
        await expect(dispose(element)).resolves.toBeUndefined();
    });

    it("reduces browser and office HTML without preserving executable or external content", () => {
        const result = sanitizePastedHtml(`
            <h2>Überschrift</h2><p><strong>Text</strong> <a href="javascript:alert(1)">Link</a></p>
            <img src="https://example.test/remote.png" alt="Bild"><script>fetch('https://example.test')</script>
        `);

        expect(result.reduced).toBe(true);
        expect(result.html).toContain("<p>Überschrift</p>");
        expect(result.html).toContain("<strong>Text</strong>");
        expect(result.html).toContain("Link");
        expect(result.html).not.toContain("javascript:");
        expect(result.html).not.toContain("<img");
        expect(result.html).not.toContain("fetch(");
    });

    it("keeps plain text plain and normalizes clipboard line endings", () => {
        expect(normalizePastedPlainText("erste\r\nzweite\rritte")).toBe("erste\nzweite\nritte");
    });
});
