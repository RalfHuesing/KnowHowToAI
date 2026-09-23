// Schmale Isolationsgrenze für showModal(), close() und das native Close-Ereignis.

const closeHandlers = new WeakMap();

export function initialize(dialog, dotNetReference) {
  if (closeHandlers.has(dialog)) {
    return;
  }

  const handleClose = () => dotNetReference.invokeMethodAsync("NotifyDialogClosedAsync");
  closeHandlers.set(dialog, handleClose);
  dialog.addEventListener("close", handleClose);
}

export function show(dialog) {
  if (dialog.open) {
    return;
  }

  dialog.showModal();
}

export function close(dialog) {
  if (dialog.open) {
    dialog.close();
  }
}

export function dispose(dialog) {
  const handleClose = closeHandlers.get(dialog);
  if (!handleClose) {
    return;
  }

  dialog.removeEventListener("close", handleClose);
  closeHandlers.delete(dialog);
}
