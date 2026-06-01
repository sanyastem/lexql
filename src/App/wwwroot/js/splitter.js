export function init() {
    document.querySelectorAll("[data-resizer]").forEach(wire);
}

function wire(handle) {
    if (handle.dataset.wired) {
        return;
    }

    handle.dataset.wired = "1";

    const dir = handle.dataset.dir || "x";
    const min = parseInt(handle.dataset.min || "120", 10);
    const max = parseInt(handle.dataset.max || "100000", 10);
    const store = handle.dataset.store;
    const target = handle.previousElementSibling;
    if (!target) {
        return;
    }

    if (store) {
        const saved = parseInt(localStorage.getItem(store) || "", 10);
        if (!Number.isNaN(saved)) {
            applySize(target, dir, clamp(saved, min, max));
        }
    }

    handle.addEventListener("pointerdown", (e) => {
        e.preventDefault();
        handle.setPointerCapture(e.pointerId);
        handle.classList.add("dragging");
        document.body.style.cursor = dir === "x" ? "col-resize" : "row-resize";
        document.body.style.userSelect = "none";

        const startPos = dir === "x" ? e.clientX : e.clientY;
        const rect = target.getBoundingClientRect();
        const startSize = dir === "x" ? rect.width : rect.height;

        const onMove = (ev) => {
            const pos = dir === "x" ? ev.clientX : ev.clientY;
            const size = clamp(startSize + (pos - startPos), min, max);
            applySize(target, dir, size);
        };

        const onUp = (ev) => {
            handle.releasePointerCapture(ev.pointerId);
            handle.classList.remove("dragging");
            document.body.style.cursor = "";
            document.body.style.userSelect = "";
            handle.removeEventListener("pointermove", onMove);
            handle.removeEventListener("pointerup", onUp);

            if (store) {
                const rectNow = target.getBoundingClientRect();
                localStorage.setItem(store, String(Math.round(dir === "x" ? rectNow.width : rectNow.height)));
            }
        };

        handle.addEventListener("pointermove", onMove);
        handle.addEventListener("pointerup", onUp);
    });
}

function applySize(target, dir, size) {
    if (dir === "x") {
        target.style.flex = "0 0 auto";
        target.style.width = size + "px";
        target.style.minWidth = size + "px";
        target.style.maxWidth = size + "px";
    } else {
        target.style.flex = "0 0 auto";
        target.style.height = size + "px";
    }
}

function clamp(value, min, max) {
    return Math.max(min, Math.min(max, value));
}
