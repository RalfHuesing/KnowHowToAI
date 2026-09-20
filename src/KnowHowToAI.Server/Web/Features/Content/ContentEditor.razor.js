const editorInstances = new WeakMap();

export async function mount(element, markdown, dotNetReference, isReadOnly) {
    const { Crepe, buildAllowedToolbar } = await import("/generated/content-editor/content-editor.js");
    const editor = new Crepe({
        root: element,
        defaultValue: markdown ?? "",
        features: {
            [Crepe.Feature.Toolbar]: true,
            [Crepe.Feature.TopBar]: false,
            [Crepe.Feature.ImageBlock]: false,
            [Crepe.Feature.Latex]: false,
            [Crepe.Feature.AI]: false,
            [Crepe.Feature.BlockEdit]: false
        },
        featureConfigs: {
            [Crepe.Feature.Toolbar]: {
                buildToolbar: buildAllowedToolbar
            }
        }
    });

    editor.setReadonly(isReadOnly === true);
    editor.on(listener => {
        listener.markdownUpdated(() => dotNetReference.invokeMethodAsync("NotifyChangedAsync").catch(() => {}));
        listener.focus(() => dotNetReference.invokeMethodAsync("NotifyFocusAsync").catch(() => {}));
    });
    await editor.create();
    editorInstances.set(element, editor);
}

export function readMarkdown(element) {
    return editorInstances.get(element)?.getMarkdown() ?? "";
}

export function focus(element) {
    element?.querySelector(".ProseMirror")?.focus();
}

export async function dispose(element) {
    const editor = editorInstances.get(element);
    if (!editor) {
        return;
    }

    editorInstances.delete(element);
    await editor.destroy();
}
