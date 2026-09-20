export function initTreeKeyboard(treeElement) {
    if (!treeElement || treeElement.__treeKeyboard) return;

    const navKeys = new Set(["ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight", "Home", "End", " "]);
    const handleKeyDown = event => {
        if (navKeys.has(event.key)) event.preventDefault();
    };

    treeElement.addEventListener("keydown", handleKeyDown);
    treeElement.__treeKeyboard = () => {
        treeElement.removeEventListener("keydown", handleKeyDown);
        delete treeElement.__treeKeyboard;
    };
}

export function disposeTreeKeyboard(treeElement) {
    treeElement?.__treeKeyboard?.();
}

const dropClasses = ["is-drop-before", "is-drop-parent", "is-drop-after"];
let activeDragBinding = null;

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
    node?.classList.remove(...dropClasses);
}

function setDropIndicator(node, position) {
    clearDropIndicator(node);
    node.classList.add(`is-drop-${position.toLowerCase()}`);
}

function createDragBinding(treeElement, dotNetReference) {
    let currentDotNetReference = dotNetReference;
    const state = {
        phase: "idle",
        pointerId: null,
        sourceNode: null,
        targetNode: null,
        pointerStart: null,
        dropInFlight: null,
        suppressClick: false
    };

    const resetVisualState = () => {
        state.sourceNode?.classList.remove("is-dragging");
        clearDropIndicator(state.targetNode);
        state.phase = "idle";
        state.pointerId = null;
        state.sourceNode = null;
        state.targetNode = null;
        state.pointerStart = null;
    };

    const cancelPointerCapture = () => {
        if (state.sourceNode && state.pointerId !== null && state.sourceNode.hasPointerCapture(state.pointerId)) {
            state.sourceNode.releasePointerCapture(state.pointerId);
        }
    };

    const finishDrop = async (targetNode, clientY) => {
        const sourceNode = state.sourceNode;
        const sourceNodeId = sourceNode?.dataset.nodeid;
        const targetNodeId = targetNode?.dataset.nodeid;
        if (!sourceNodeId || !targetNodeId || sourceNodeId === targetNodeId) {
            cancelPointerCapture();
            resetVisualState();
            return;
        }

        const position = resolveDropPosition(targetNode, clientY);
        cancelPointerCapture();
        resetVisualState();
        state.suppressClick = true;
        state.dropInFlight = currentDotNetReference.invokeMethodAsync(
            "HandleTreeDropAsync",
            sourceNodeId,
            targetNodeId,
            position);

        try {
            await state.dropInFlight;
        }
        catch {
            // A disposed circuit may reject the in-flight interop call after the
            // DOM has already been cleaned up. The next render owns the state.
        }
        finally {
            state.dropInFlight = null;
            window.setTimeout(() => { state.suppressClick = false; }, 0);
        }
    };

    const handlePointerDown = event => {
        if (!event.isPrimary || event.button !== 0 || state.dropInFlight || event.target.closest("button")) return;

        const sourceNode = resolveTreeNode(treeElement, event.target);
        if (!sourceNode) return;

        state.phase = "pressed";
        state.pointerId = event.pointerId;
        state.sourceNode = sourceNode;
        state.pointerStart = { x: event.clientX, y: event.clientY };
        sourceNode.setPointerCapture(event.pointerId);
    };

    const handlePointerMove = event => {
        if (event.pointerId !== state.pointerId || !state.sourceNode || !state.pointerStart) return;

        const deltaX = event.clientX - state.pointerStart.x;
        const deltaY = event.clientY - state.pointerStart.y;
        if (state.phase === "pressed" && Math.hypot(deltaX, deltaY) < 5) return;

        state.phase = "dragging";
        state.sourceNode.classList.add("is-dragging");
        const targetNode = resolveTreeNode(treeElement, document.elementFromPoint(event.clientX, event.clientY));
        if (!targetNode || targetNode === state.sourceNode) {
            clearDropIndicator(state.targetNode);
            state.targetNode = null;
            event.preventDefault();
            return;
        }

        if (state.targetNode !== targetNode) clearDropIndicator(state.targetNode);
        state.targetNode = targetNode;
        setDropIndicator(targetNode, resolveDropPosition(targetNode, event.clientY));
        event.preventDefault();
    };

    const handlePointerUp = async event => {
        if (event.pointerId !== state.pointerId) return;
        if (state.phase !== "dragging" || !state.targetNode) {
            cancelPointerCapture();
            resetVisualState();
            return;
        }

        event.preventDefault();
        await finishDrop(state.targetNode, event.clientY);
    };

    const handlePointerCancel = event => {
        if (event.pointerId === state.pointerId) {
            cancelPointerCapture();
            resetVisualState();
        }
    };

    const handleClick = event => {
        if (!state.suppressClick) return;
        event.preventDefault();
        event.stopPropagation();
        state.suppressClick = false;
    };

    treeElement.addEventListener("pointerdown", handlePointerDown);
    treeElement.addEventListener("pointermove", handlePointerMove);
    treeElement.addEventListener("pointerup", handlePointerUp);
    treeElement.addEventListener("pointercancel", handlePointerCancel);
    treeElement.addEventListener("click", handleClick, true);

    const dispose = () => {
        cancelPointerCapture();
        resetVisualState();
        treeElement.removeEventListener("pointerdown", handlePointerDown);
        treeElement.removeEventListener("pointermove", handlePointerMove);
        treeElement.removeEventListener("pointerup", handlePointerUp);
        treeElement.removeEventListener("pointercancel", handlePointerCancel);
        treeElement.removeEventListener("click", handleClick, true);
        delete treeElement.__treeDragAndDrop;
        if (activeDragBinding?.treeElement === treeElement) activeDragBinding = null;
    };

    return {
        treeElement,
        dispose,
        update(reference) {
            currentDotNetReference = reference;
        }
    };
}

export function initTreeDragAndDrop(treeElement, dotNetReference) {
    if (!treeElement) return;

    if (activeDragBinding?.treeElement !== treeElement) {
        activeDragBinding?.dispose();
        activeDragBinding = createDragBinding(treeElement, dotNetReference);
        treeElement.__treeDragAndDrop = activeDragBinding.dispose;
    }
    else {
        activeDragBinding.update(dotNetReference);
    }
}

export function disposeTreeDragAndDrop(treeElement) {
    treeElement?.__treeDragAndDrop?.();
}
