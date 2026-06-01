let ref = null;

export function register(dotnetRef) {
    ref = dotnetRef;
    document.addEventListener("keydown", onKey);
}

function onKey(e) {
    if ((e.ctrlKey || e.metaKey) && (e.key === "p" || e.key === "P")) {
        e.preventDefault();
        if (ref) {
            ref.invokeMethodAsync("Open");
        }
    }
}

export function unregister() {
    document.removeEventListener("keydown", onKey);
    ref = null;
}
