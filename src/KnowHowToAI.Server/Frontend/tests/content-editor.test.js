import { beforeEach, describe, expect, it, vi } from "vitest";
import { dispose, focus, mount, readMarkdown } from "../../Web/Features/Content/ContentEditor.razor.js";
import { lastEditor, lifecycle } from "./fake-generated-editor.js";

beforeEach(() => {
    lifecycle.length = 0;
});

describe("ContentEditor JS interop", () => {
    it("mounts, reads and focuses the current instance, then disposes", async () => {
        const focusTarget = { focus: vi.fn() };
        const element = { querySelector: vi.fn(() => focusTarget) };
        const reference = { invokeMethodAsync: vi.fn().mockResolvedValue(undefined) };

        await mount(element, "Initial", reference, false);

        expect(lifecycle.map(([name]) => name)).toEqual(["construct", "readonly", "create"]);
        expect(lastEditor.options.defaultValue).toBe("Initial");
        expect(lastEditor.options.features).toMatchObject({
            toolbar: true,
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
        expect(lifecycle.map(([name]) => name)).toEqual(["construct", "readonly", "create", "read", "destroy"]);
        expect(readMarkdown(element)).toBe("");
    });

    it("forwards markdown and focus callbacks and makes dispose idempotent", async () => {
        const element = {};
        const reference = { invokeMethodAsync: vi.fn().mockResolvedValue(undefined) };

        await mount(element, "Callback", reference, false);
        lastEditor.markdownUpdated();
        lastEditor.focused();
        expect(reference.invokeMethodAsync).toHaveBeenNthCalledWith(1, "NotifyChangedAsync");
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
});
