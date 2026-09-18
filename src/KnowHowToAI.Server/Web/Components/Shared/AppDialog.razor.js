// Schmale Isolationsgrenze für den nativen HTML-Dialog: showModal(), Fokusfalle
// per Tab/Shift+Tab, Escape über das native Cancel/Close-Ereignis und
// Fokusrückgabe an den Auslöser. Bewusst keine allgemeine Dialogzustandsmaschine.

const dialogStates = new WeakMap();

export function initialize(dialog, dotNetReference) {
  dialogStates.set(dialog, { dotNetReference, opener: null });
  dialog.addEventListener("keydown", handleKeydown);
  dialog.addEventListener("close", handleClose);
}

export function show(dialog) {
  const state = dialogStates.get(dialog);
  if (!state || dialog.open) {
    return;
  }

  state.opener = document.activeElement instanceof HTMLElement ? document.activeElement : null;
  dialog.showModal();
  focusFirstInDialog(dialog);
}

export function close(dialog) {
  if (dialog.open) {
    dialog.close();
  }
}

export function dispose(dialog) {
  if (!dialogStates.has(dialog)) {
    return;
  }

  dialog.removeEventListener("keydown", handleKeydown);
  dialog.removeEventListener("close", handleClose);
  dialogStates.delete(dialog);
}

function handleKeydown(event) {
  if (event.key !== "Tab") {
    return;
  }

  const dialog = event.currentTarget;
  const focusableElements = getFocusableElements(dialog);
  if (focusableElements.length === 0) {
    event.preventDefault();
    return;
  }

  const firstElement = focusableElements[0];
  const lastElement = focusableElements[focusableElements.length - 1];
  const focusLeavesDialog = !dialog.contains(document.activeElement);

  if (event.shiftKey && (document.activeElement === firstElement || focusLeavesDialog)) {
    event.preventDefault();
    lastElement.focus();
  } else if (!event.shiftKey && (document.activeElement === lastElement || focusLeavesDialog)) {
    event.preventDefault();
    firstElement.focus();
  }
}

function handleClose(event) {
  const dialog = event.currentTarget;
  const state = dialogStates.get(dialog);
  if (!state) {
    return;
  }

  if (state.opener instanceof HTMLElement) {
    state.opener.focus();
  }

  state.dotNetReference.invokeMethodAsync("NotifyDialogClosedAsync");
}

function getFocusableElements(dialog) {
  return Array.from(
    dialog.querySelectorAll(
      'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
    ),
  ).filter((element) => element.offsetParent !== null);
}

function focusFirstInDialog(dialog) {
  const focusableElements = getFocusableElements(dialog);
  if (focusableElements.length === 0) {
    dialog.focus();
    return;
  }

  focusableElements[0].focus();
}
