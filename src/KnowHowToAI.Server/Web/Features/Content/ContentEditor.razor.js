const editorInstances = new WeakMap();

const allowedTags = new Set([
    "a", "blockquote", "br", "code", "del", "div", "em", "i", "li", "ol", "p", "pre", "s", "strong", "table", "tbody", "td", "th", "thead", "tr", "ul"
]);
const removedBlockTags = new Set(["audio", "canvas", "embed", "iframe", "img", "input", "object", "picture", "script", "source", "style", "svg", "video"]);
const headingPattern = /^h[1-6]$/;

function isSafeLink(value) {
    const href = value.trim();
    return href.startsWith("https:")
        || href.startsWith("mailto:")
        || href.startsWith("#")
        || (href.startsWith("/") && !href.startsWith("//"))
        || (!href.includes(":") && !href.startsWith("//"));
}

function readAttribute(attributes, name) {
    const pattern = new RegExp(`\\b${name}\\s*=\\s*(?:"([^"]*)"|'([^']*)'|([^\\s>]+))`, "i");
    const match = attributes.match(pattern);
    return match?.[1] ?? match?.[2] ?? match?.[3] ?? null;
}

/**
 * Reduziert Browser-/Office-HTML auf die erlaubten Editorbausteine. Die
 * Funktion arbeitet bewusst nur auf dem Clipboard-String; dadurch können
 * eingebettete Bildquellen niemals als Ressource geladen werden.
 */
export function sanitizePastedHtml(html) {
    let reduced = false;
    let skipDepth = 0;
    const output = [];
    const tokenPattern = /<!--[\s\S]*?-->|<\/?[a-z][^>]*>/gi;
    let cursor = 0;

    const appendText = text => {
        if (skipDepth === 0) output.push(text);
    };

    for (const match of html.matchAll(tokenPattern)) {
        appendText(html.slice(cursor, match.index));
        cursor = match.index + match[0].length;
        const token = match[0];
        if (token.startsWith("<!--")) {
            reduced = true;
            continue;
        }

        const closing = /^<\//.test(token);
        const nameMatch = token.match(/^<\/?([a-z][a-z0-9-]*)/i);
        if (!nameMatch) continue;
        const name = nameMatch[1].toLowerCase();

        if (removedBlockTags.has(name)) {
            reduced = true;
            if (!closing && !token.endsWith("/>") && name !== "img" && name !== "input" && name !== "source" && name !== "embed") skipDepth++;
            else if (closing && skipDepth > 0) skipDepth--;
            continue;
        }
        if (skipDepth > 0) {
            if (!closing && !token.endsWith("/")) skipDepth++;
            else if (closing) skipDepth--;
            continue;
        }

        const normalizedName = headingPattern.test(name) ? "p" : name;
        if (headingPattern.test(name) || (!allowedTags.has(name) && !closing)) reduced = true;
        if (headingPattern.test(name)) reduced = true;
        if (closing) {
            if (allowedTags.has(normalizedName) || headingPattern.test(name)) output.push(`</${normalizedName}>`);
            continue;
        }
        if (!allowedTags.has(normalizedName)) continue;

        if (normalizedName === "a") {
            const href = readAttribute(token, "href");
            if (href && isSafeLink(href)) output.push(`<a href="${href.replaceAll("&", "&amp;").replaceAll('"', "&quot;")}">`);
            else {
                output.push("<a>");
                if (href) reduced = true;
            }
            continue;
        }
        output.push(`<${normalizedName}>`);
    }
    appendText(html.slice(cursor));
    return { html: output.join(""), reduced };
}

export function normalizePastedPlainText(text) {
    return text.replace(/\r\n?/g, "\n");
}

function setPasteStatus(element, reduced) {
    if (!reduced) return;
    const root = element.closest?.("[data-testid='content-editor']");
    const status = root?.querySelector?.("[data-testid='content-editor-paste-warning']");
    if (status) status.hidden = false;
    element.dispatchEvent?.(new CustomEvent("content-editor-paste-reduced", { bubbles: true }));
}

function installPasteHandler(element, isReadOnly) {
    if (isReadOnly || typeof element.addEventListener !== "function") return null;
    const handler = event => {
        const data = event.clipboardData;
        if (!data) return;
        const html = data.getData("text/html");
        const text = data.getData("text/plain");
        if (!html && !text) return;

        const sanitized = html ? sanitizePastedHtml(html) : { html: "", reduced: false };
        event.preventDefault();
        event.stopPropagation();
        if (html && sanitized.html) document.execCommand("insertHTML", false, sanitized.html);
        else if (text) document.execCommand("insertText", false, normalizePastedPlainText(text));
        setPasteStatus(element, sanitized.reduced);
        if (sanitized.reduced) {
            const instance = editorInstances.get(element);
            instance?.dotNetReference?.invokeMethodAsync("NotifyPasteReducedAsync").catch(() => {});
        }
    };
    element.addEventListener("paste", handler, true);
    element.dataset.pastePolicy = "active";
    return handler;
}

export async function mount(element, markdown, dotNetReference, isReadOnly) {
    const { Crepe, bindFormattingToolbar, updateFormattingState } = await import("/generated/content-editor/content-editor.js");
    const editor = new Crepe({
        root: element,
        defaultValue: markdown ?? "",
        features: {
            [Crepe.Feature.Toolbar]: false,
            [Crepe.Feature.TopBar]: false,
            [Crepe.Feature.ImageBlock]: false,
            [Crepe.Feature.Latex]: false,
            [Crepe.Feature.AI]: false,
            [Crepe.Feature.BlockEdit]: false
        },
    });

    editor.setReadonly(isReadOnly === true);
    editor.on(listener => {
        listener.markdownUpdated((ctx, markdown) => dotNetReference.invokeMethodAsync("NotifyChangedAsync", markdown ?? "").catch(() => {}));
        listener.focus(() => dotNetReference.invokeMethodAsync("NotifyFocusAsync").catch(() => {}));
        listener.selectionUpdated(ctx => {
            const toolbar = element.closest?.("[data-testid='content-editor']")
                ?.querySelector?.("[data-testid='content-editor-toolbar']");
            if (toolbar) updateFormattingState(toolbar, ctx);
        });
    });
    editor.dotNetReference = dotNetReference;
    editor.pasteHandler = installPasteHandler(element, isReadOnly === true);
    const toolbar = element.closest?.("[data-testid='content-editor']")
        ?.querySelector?.("[data-testid='content-editor-toolbar']");
    editor.toolbarHandler = !toolbar || isReadOnly === true ? null : bindFormattingToolbar(editor, toolbar);
    editorInstances.set(element, editor);
    await editor.create();
    if (toolbar) editor.editor.action(ctx => updateFormattingState(toolbar, ctx));
}

export function readMarkdown(element) {
    return editorInstances.get(element)?.getMarkdown() ?? "";
}

export function focus(element) {
    element?.querySelector(".ProseMirror")?.focus();
}

export function activate(element) {
    requestAnimationFrame(() => {
        const editor = element?.querySelector(".ProseMirror");
        if (!editor) return;
        editor.getBoundingClientRect();
        window.dispatchEvent(new Event("resize"));
        editor.focus();
    });
}

export async function dispose(element) {
    const editor = editorInstances.get(element);
    if (!editor) {
        return;
    }

    editorInstances.delete(element);
    if (editor.pasteHandler) {
        element.removeEventListener("paste", editor.pasteHandler, true);
        delete element.dataset.pastePolicy;
    }
    editor.toolbarHandler?.();
    await editor.destroy();
}
