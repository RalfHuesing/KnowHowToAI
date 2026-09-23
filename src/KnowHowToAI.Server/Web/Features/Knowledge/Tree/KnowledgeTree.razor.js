const dropClasses = ["is-drop-before", "is-drop-parent", "is-drop-after"];
let activeDragBinding = null;
const suppressedClickDeadlineKey = "__knowledgeTreeSuppressedClickDeadline";

function resolveTreeNode(treeElement, target) {
    const node = target instanceof Element ? target.closest(".tree-node[data-nodeid]") : null;
    return node && treeElement.contains(node) ? node : null;
}

function resolveDropPosition(node, clientY) {
    const bounds = node.querySelector(".tree-node-title")?.getBoundingClientRect() ?? node.getBoundingClientRect();
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

function createDragBinding(treeElement, dotNetReference, canMove) {
    let currentDotNetReference = dotNetReference;
    let canMoveNodes = canMove;
    const state = {
        phase: "idle",
        pointerId: null,
        sourceNode: null,
        targetNode: null,
        pointerStart: null,
        dropInFlight: null,
        suppressClick: false,
        suppressClickTimer: null
    };

    const suppressClickForCurrentGesture = () => {
        state.suppressClick = true;
        window[suppressedClickDeadlineKey] = Date.now() + 1000;
        window.clearTimeout(state.suppressClickTimer);
        state.suppressClickTimer = window.setTimeout(() => {
            state.suppressClick = false;
            state.suppressClickTimer = null;
        }, 1000);
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
        suppressClickForCurrentGesture();
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
        }
    };

    const handlePointerDown = event => {
        if (!canMoveNodes || !event.isPrimary || event.button !== 0 || state.dropInFlight
            || event.target.closest("button:not(.tree-node-select)")) return;

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

        if (state.phase === "pressed") state.suppressClick = true;
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
            const wasDragging = state.phase === "dragging";
            cancelPointerCapture();
            resetVisualState();
            if (wasDragging) suppressClickForCurrentGesture();
            return;
        }

        event.preventDefault();
        await finishDrop(state.targetNode, event.clientY);
    };

    const handlePointerCancel = event => {
        if (event.pointerId === state.pointerId) {
            const wasDragging = state.phase === "dragging";
            cancelPointerCapture();
            resetVisualState();
            if (wasDragging) suppressClickForCurrentGesture();
        }
    };

    const handleClick = event => {
        const selectionButton = event.target instanceof Element
            ? event.target.closest(".tree-node-select[data-nodeid]")
            : null;
        if (!selectionButton || !treeElement.contains(selectionButton)) return;

        if (state.suppressClick || Date.now() <= (window[suppressedClickDeadlineKey] ?? 0)) {
            event.preventDefault();
            event.stopImmediatePropagation();
            state.suppressClick = false;
            window[suppressedClickDeadlineKey] = 0;
            window.clearTimeout(state.suppressClickTimer);
            state.suppressClickTimer = null;
            return;
        }

        currentDotNetReference.invokeMethodAsync("HandleTreeSelectionAsync", selectionButton.dataset.nodeid).catch(() => {});
    };

    treeElement.addEventListener("pointerdown", handlePointerDown);
    treeElement.addEventListener("pointermove", handlePointerMove);
    treeElement.addEventListener("pointerup", handlePointerUp);
    treeElement.addEventListener("pointercancel", handlePointerCancel);
    window.addEventListener("click", handleClick, true);

    const dispose = () => {
        window.clearTimeout(state.suppressClickTimer);
        cancelPointerCapture();
        resetVisualState();
        treeElement.removeEventListener("pointerdown", handlePointerDown);
        treeElement.removeEventListener("pointermove", handlePointerMove);
        treeElement.removeEventListener("pointerup", handlePointerUp);
        treeElement.removeEventListener("pointercancel", handlePointerCancel);
        window.removeEventListener("click", handleClick, true);
        delete treeElement.__treeDragAndDrop;
        if (activeDragBinding?.treeElement === treeElement) activeDragBinding = null;
    };

    return {
        treeElement,
        dispose,
        update(reference, canMove) {
            currentDotNetReference = reference;
            canMoveNodes = canMove;
        }
    };
}

export function initTreeDragAndDrop(treeElement, dotNetReference, canMove) {
    if (!treeElement) return;

    if (activeDragBinding?.treeElement !== treeElement) {
        activeDragBinding?.dispose();
        activeDragBinding = createDragBinding(treeElement, dotNetReference, canMove);
        treeElement.__treeDragAndDrop = activeDragBinding.dispose;
    }
    else {
        activeDragBinding.update(dotNetReference, canMove);
    }
}

export function disposeTreeDragAndDrop(treeElement) {
    treeElement?.__treeDragAndDrop?.();
}
