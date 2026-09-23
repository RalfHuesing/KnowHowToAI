import { Crepe } from "@milkdown/crepe";
import { commandsCtx, editorViewCtx } from "@milkdown/kit/core";
import {
    emphasisSchema,
    inlineCodeSchema,
    isMarkSelectedCommand,
    linkSchema,
    strongSchema,
    toggleEmphasisCommand,
    toggleInlineCodeCommand,
    toggleStrongCommand
} from "@milkdown/kit/preset/commonmark";
import {
    strikethroughSchema,
    toggleStrikethroughCommand
} from "@milkdown/kit/preset/gfm";

const formattingCommands = {
    bold: { schema: strongSchema, command: toggleStrongCommand },
    italic: { schema: emphasisSchema, command: toggleEmphasisCommand },
    strikethrough: { schema: strikethroughSchema, command: toggleStrikethroughCommand },
    code: { schema: inlineCodeSchema, command: toggleInlineCodeCommand },
    link: { schema: linkSchema }
};

export function updateFormattingState(toolbar, ctx) {
    const commands = ctx.get(commandsCtx);
    for (const button of toolbar.querySelectorAll("[data-format]")) {
        const formatting = formattingCommands[button.dataset.format];
        if (!formatting) continue;

        const active = commands.call(isMarkSelectedCommand.key, formatting.schema.type(ctx));
        button.setAttribute("aria-pressed", String(active));
        button.classList.toggle("is-active", active);
    }
}

export function bindFormattingToolbar(editor, toolbar) {
    const preserveSelection = event => event.preventDefault();
    const applyFormatting = event => {
        const button = event.target.closest?.("button[data-format]");
        if (!button || !toolbar.contains(button)) return;

        const formatting = formattingCommands[button.dataset.format];
        if (!formatting) return;

        editor.editor.action(ctx => {
            if (button.dataset.format === "link") {
                applyLinkFormatting(editor, ctx);
            } else {
                ctx.get(commandsCtx).call(formatting.command.key);
            }
            updateFormattingState(toolbar, ctx);
        });
    };

    toolbar.addEventListener("mousedown", preserveSelection);
    toolbar.addEventListener("click", applyFormatting);
    return () => {
        toolbar.removeEventListener("mousedown", preserveSelection);
        toolbar.removeEventListener("click", applyFormatting);
    };
}

function applyLinkFormatting(editor, ctx) {
    const view = ctx.get(editorViewCtx);
    const { state } = view;
    const { from, to, empty } = state.selection;
    if (empty) return;

    const markType = linkSchema.type(ctx);
    const selectedLink = state.doc.rangeHasMark(from, to, markType)
        ? state.doc.resolve(from).marks().find(mark => mark.type === markType)
        : null;
    const href = globalThis.prompt("Link-Adresse", selectedLink?.attrs.href ?? "");
    if (href === null) return;

    const normalizedHref = href.trim();
    const transaction = normalizedHref
        ? state.tr.addMark(from, to, markType.create({ href: normalizedHref }))
        : state.tr.removeMark(from, to, markType);
    view.dispatch(transaction);
}

export { Crepe };
