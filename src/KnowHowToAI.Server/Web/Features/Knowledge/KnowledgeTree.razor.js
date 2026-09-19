export function initTreeKeyboard(treeElement) {
    if (!treeElement) return;
    const navKeys = new Set(["ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight", "Home", "End", " "]);
    treeElement.addEventListener("keydown", (e) => {
        if (navKeys.has(e.key)) {
            e.preventDefault();
        }
    });
}
