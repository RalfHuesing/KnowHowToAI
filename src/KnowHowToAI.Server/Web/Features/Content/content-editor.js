import { Crepe } from "@milkdown/crepe";
import { commandsCtx } from "@milkdown/kit/core";
import { toggleLinkCommand } from "@milkdown/kit/component/link-tooltip";
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
    link: { schema: linkSchema, command: toggleLinkCommand }
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
            ctx.get(commandsCtx).call(formatting.command.key);
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

export { Crepe };
