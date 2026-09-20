export const lifecycle = [];
export let lastEditor;

export class Crepe {
    static Feature = {
        Toolbar: "toolbar",
        TopBar: "top-bar",
        ImageBlock: "image-block",
        Latex: "latex",
        AI: "ai",
        BlockEdit: "block-edit"
    };

    constructor(options) {
        this.options = options;
        this.markdown = options.defaultValue;
        lastEditor = this;
        lifecycle.push(["construct", options]);
    }

    setReadonly(value) {
        this.readonly = value;
        lifecycle.push(["readonly", value]);
    }

    on(register) {
        register({
            markdownUpdated: callback => { this.markdownUpdated = callback; },
            focus: callback => { this.focused = callback; }
        });
    }

    async create() {
        lifecycle.push(["create"]);
    }

    getMarkdown() {
        lifecycle.push(["read"]);
        return this.markdown;
    }

    async destroy() {
        lifecycle.push(["destroy"]);
    }
}

export function buildAllowedToolbar() {}
