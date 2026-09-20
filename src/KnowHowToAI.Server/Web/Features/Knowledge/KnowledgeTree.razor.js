export function initTreeKeyboard(treeElement) {
    if (!treeElement) return;
    const navKeys = new Set(["ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight", "Home", "End", " "]);
    treeElement.addEventListener("keydown", (e) => {
        if (navKeys.has(e.key)) {
            e.preventDefault();
        }
    });
}

const dropClasses = ["is-drop-before", "is-drop-parent", "is-drop-after"];

function resolveTreeNode(treeElement, target) {
    const node = target instanceof Element ? target.closest(".tree-node[data-nodeid]") : null;
    return node && treeElement.contains(node) ? node : null;
}

function resolveDropPosition(node, clientY) {
    const bounds = node.getBoundingClientRect();
    const relativeY = Math.min(Math.max(clientY - bounds.top, 0), bounds.height);

    if (relativeY < bounds.height * 0.25) return "Before";
    if (relativeY > bounds.height * 0.75) return "After";
    return "Parent";
}

function clearDropIndicator(node) {
    if (node) node.classList.remove(...dropClasses);
}

function setDropIndicator(node, position) {
    clearDropIndicator(node);
    node.classList.add(`is-drop-${position.toLowerCase()}`);
}

export function initTreeDragAndDrop(treeElement, dotNetReference) {
    if (!treeElement || treeElement.__treeDragAndDrop) return;

    let sourceNode = null;
    let dropTarget = null;

    const clearDragState = () => {
        sourceNode?.classList.remove("is-dragging");
        clearDropIndicator(dropTarget);
        sourceNode = null;
        dropTarget = null;
    };

    const handleDragStart = event => {
        sourceNode = resolveTreeNode(treeElement, event.target);
        if (!sourceNode) return;

        sourceNode.classList.add("is-dragging");
        event.dataTransfer?.setData("text/plain", sourceNode.dataset.nodeid);
        if (event.dataTransfer) event.dataTransfer.effectAllowed = "move";
    };

    const handleDragOver = event => {
        if (!sourceNode) return;

        const target = resolveTreeNode(treeElement, event.target);
        if (!target) return;

        event.preventDefault();
        if (dropTarget !== target) clearDropIndicator(dropTarget);
        dropTarget = target;
        setDropIndicator(target, resolveDropPosition(target, event.clientY));
    };

    const handleDragLeave = event => {
        const target = resolveTreeNode(treeElement, event.target);
        if (target && target === dropTarget && !target.contains(event.relatedTarget)) {
            clearDropIndicator(target);
            dropTarget = null;
        }
    };

    const handleDrop = async event => {
        const target = resolveTreeNode(treeElement, event.target);
        if (!sourceNode || !target) return;

        event.preventDefault();
        const sourceNodeId = sourceNode.dataset.nodeid;
        const targetNodeId = target.dataset.nodeid;
        const position = resolveDropPosition(target, event.clientY);
        clearDragState();
        await dotNetReference.invokeMethodAsync("HandleTreeDropAsync", sourceNodeId, targetNodeId, position);
    };

    treeElement.addEventListener("dragstart", handleDragStart);
    treeElement.addEventListener("dragover", handleDragOver);
    treeElement.addEventListener("dragleave", handleDragLeave);
    treeElement.addEventListener("drop", handleDrop);
    treeElement.addEventListener("dragend", clearDragState);
    treeElement.__treeDragAndDrop = () => {
        clearDragState();
        treeElement.removeEventListener("dragstart", handleDragStart);
        treeElement.removeEventListener("dragover", handleDragOver);
        treeElement.removeEventListener("dragleave", handleDragLeave);
        treeElement.removeEventListener("drop", handleDrop);
        treeElement.removeEventListener("dragend", clearDragState);
        delete treeElement.__treeDragAndDrop;
    };
}

export function disposeTreeDragAndDrop(treeElement) {
    treeElement?.__treeDragAndDrop?.();
}
