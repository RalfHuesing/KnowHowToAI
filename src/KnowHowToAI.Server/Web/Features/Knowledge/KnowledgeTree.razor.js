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
    let pointerStart = null;
    let pointerDragActive = false;

    const clearDragState = () => {
        sourceNode?.classList.remove("is-dragging");
        clearDropIndicator(dropTarget);
        sourceNode = null;
        dropTarget = null;
        pointerStart = null;
        pointerDragActive = false;
    };

    const completeDrop = async (target, clientY) => {
        if (!sourceNode || !target) return;

        const sourceNodeId = sourceNode.dataset.nodeid;
        const targetNodeId = target.dataset.nodeid;
        const position = resolveDropPosition(target, clientY);
        clearDragState();
        await dotNetReference.invokeMethodAsync("HandleTreeDropAsync", sourceNodeId, targetNodeId, position);
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
        await completeDrop(target, event.clientY);
    };

    const handlePointerDown = event => {
        if (event.button !== 0 || event.target.closest("button")) return;

        const node = resolveTreeNode(treeElement, event.target);
        if (!node) return;

        sourceNode = node;
        pointerStart = { x: event.clientX, y: event.clientY };
    };

    const handlePointerMove = event => {
        if (!sourceNode || !pointerStart) return;

        const deltaX = event.clientX - pointerStart.x;
        const deltaY = event.clientY - pointerStart.y;
        if (!pointerDragActive && Math.hypot(deltaX, deltaY) < 5) return;

        pointerDragActive = true;
        sourceNode.classList.add("is-dragging");
        const target = resolveTreeNode(treeElement, document.elementFromPoint(event.clientX, event.clientY));
        if (!target) return;

        if (dropTarget !== target) clearDropIndicator(dropTarget);
        dropTarget = target;
        setDropIndicator(target, resolveDropPosition(target, event.clientY));
        event.preventDefault();
    };

    const handlePointerUp = async event => {
        if (!pointerDragActive || !dropTarget) {
            clearDragState();
            return;
        }

        event.preventDefault();
        await completeDrop(dropTarget, event.clientY);
    };

    treeElement.addEventListener("dragstart", handleDragStart);
    treeElement.addEventListener("dragover", handleDragOver);
    treeElement.addEventListener("dragleave", handleDragLeave);
    treeElement.addEventListener("drop", handleDrop);
    treeElement.addEventListener("dragend", clearDragState);
    treeElement.addEventListener("pointerdown", handlePointerDown);
    window.addEventListener("pointermove", handlePointerMove);
    window.addEventListener("pointerup", handlePointerUp);
    window.addEventListener("pointercancel", clearDragState);
    treeElement.__treeDragAndDrop = () => {
        clearDragState();
        treeElement.removeEventListener("dragstart", handleDragStart);
        treeElement.removeEventListener("dragover", handleDragOver);
        treeElement.removeEventListener("dragleave", handleDragLeave);
        treeElement.removeEventListener("drop", handleDrop);
        treeElement.removeEventListener("dragend", clearDragState);
        treeElement.removeEventListener("pointerdown", handlePointerDown);
        window.removeEventListener("pointermove", handlePointerMove);
        window.removeEventListener("pointerup", handlePointerUp);
        window.removeEventListener("pointercancel", clearDragState);
        delete treeElement.__treeDragAndDrop;
    };
}

export function disposeTreeDragAndDrop(treeElement) {
    treeElement?.__treeDragAndDrop?.();
}
