import { Crepe } from "@milkdown/crepe";
import { commandsCtx } from "@milkdown/kit/core";
import {
    emphasisSchema,
    inlineCodeSchema,
    isMarkSelectedCommand,
    linkSchema,
    strongSchema,
    toggleEmphasisCommand,
    toggleInlineCodeCommand,
    toggleLinkCommand,
    toggleStrongCommand
} from "@milkdown/kit/preset/commonmark";
import {
    strikethroughSchema,
    toggleStrikethroughCommand
} from "@milkdown/kit/preset/gfm";

const addMarkItem = (group, key, label, icon, schema, command) => {
    group.addItem(key, {
        icon,
        label,
        active: ctx => ctx.get(commandsCtx).call(isMarkSelectedCommand.key, schema.type(ctx)),
        onRun: ctx => ctx.get(commandsCtx).call(command.key)
    });
};

export function buildAllowedToolbar(builder) {
    builder.clear();
    const formatting = builder.addGroup("formatting", "Formatierung");
    addMarkItem(formatting, "bold", "Fett", "B", strongSchema, toggleStrongCommand);
    addMarkItem(formatting, "italic", "Kursiv", "I", emphasisSchema, toggleEmphasisCommand);
    addMarkItem(formatting, "strikethrough", "Durchgestrichen", "S", strikethroughSchema, toggleStrikethroughCommand);
    addMarkItem(formatting, "code", "Inline-Code", "{}", inlineCodeSchema, toggleInlineCodeCommand);

    const links = builder.addGroup("links", "Links");
    addMarkItem(links, "link", "Link", "↗", linkSchema, toggleLinkCommand);
}

export { Crepe };
