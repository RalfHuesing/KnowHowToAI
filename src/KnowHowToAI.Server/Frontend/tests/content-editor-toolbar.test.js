import { describe, expect, it, vi } from "vitest";

vi.mock("@milkdown/crepe", () => ({ Crepe: class Crepe {} }));
vi.mock("@milkdown/kit/core", () => ({ commandsCtx: "commandsCtx" }));
vi.mock("@milkdown/kit/component/link-tooltip", () => ({ toggleLinkCommand: { key: "toggle:link" } }));
vi.mock("@milkdown/kit/preset/commonmark", () => ({
    emphasisSchema: { type: () => "schema:italic" },
    inlineCodeSchema: { type: () => "schema:code" },
    isMarkSelectedCommand: { key: "selected" },
    linkSchema: { type: () => "schema:link" },
    strongSchema: { type: () => "schema:bold" },
    toggleEmphasisCommand: { key: "toggle:italic" },
    toggleInlineCodeCommand: { key: "toggle:code" },
    toggleStrongCommand: { key: "toggle:bold" }
}));
vi.mock("@milkdown/kit/preset/gfm", () => ({
    strikethroughSchema: { type: () => "schema:strikethrough" },
    toggleStrikethroughCommand: { key: "toggle:strikethrough" }
}));

const { bindFormattingToolbar, updateFormattingState } = await import("../../Web/Features/Content/content-editor.js");

const formatNames = ["bold", "italic", "strikethrough", "code", "link"];

function createToolbar() {
    const buttons = formatNames.map(name => {
        const classes = new Set();
        return {
            dataset: { format: name },
            attributes: {},
            classes,
            setAttribute(name, value) { this.attributes[name] = value; },
            classList: { toggle(name, enabled) { enabled ? classes.add(name) : classes.delete(name); } }
        };
    });
    const listeners = new Map();
    return {
        buttons,
        listeners,
        addEventListener: vi.fn((name, listener) => listeners.set(name, listener)),
        removeEventListener: vi.fn(name => listeners.delete(name)),
        querySelectorAll: () => buttons,
        contains: button => buttons.includes(button)
    };
}

describe("ContentEditor formatting toolbar", () => {
    it("applies each existing Milkdown command to the retained selection", () => {
        const toolbar = createToolbar();
        const calls = [];
        const context = {
            get: key => key === "commandsCtx" ? { call: (...args) => { calls.push(args); return true; } } : null
        };
        const editor = { editor: { action: callback => callback(context) } };
        bindFormattingToolbar(editor, toolbar);

        for (const button of toolbar.buttons) {
            toolbar.listeners.get("click")({ target: { closest: () => button } });
        }

        expect(calls.filter(([key]) => key.startsWith("toggle:"))).toEqual([
            ["toggle:bold"], ["toggle:italic"], ["toggle:strikethrough"], ["toggle:code"], ["toggle:link"]
        ]);
    });

    it("reflects all five active selection marks and removes toolbar handlers on dispose", () => {
        const toolbar = createToolbar();
        const context = {
            get: () => ({ call: () => true })
        };
        const cleanup = bindFormattingToolbar({ editor: { action: callback => callback(context) } }, toolbar);

        updateFormattingState(toolbar, context);

        expect(toolbar.buttons.map(button => button.attributes["aria-pressed"])).toEqual(["true", "true", "true", "true", "true"]);
        expect(toolbar.buttons.every(button => button.classes.has("is-active"))).toBe(true);
        cleanup();
        expect(toolbar.listeners.size).toBe(0);
    });
});
