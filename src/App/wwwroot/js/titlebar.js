let dotnet = null;
let dragging = false;
let startScreenX = 0;
let startScreenY = 0;
let winX = 0;
let winY = 0;
let captureZone = null;
let capturePointer = 0;

export function init(dotnetRef) {
    dotnet = dotnetRef;
    document.addEventListener("pointerdown", onPointerDown, true);
    document.addEventListener("pointermove", onPointerMove, true);
    document.addEventListener("pointerup", onPointerUp, true);
}

function dragZone(target) {
    if (!target || !target.closest) {
        return null;
    }

    if (target.closest(".titlebar-tabs, .titlebar-tools, .win-buttons")) {
        return null;
    }

    return target.closest(".titlebar");
}

async function onPointerDown(e) {
    const zone = dragZone(e.target);
    if (e.button !== 0 || !zone) {
        return;
    }

    startScreenX = e.screenX;
    startScreenY = e.screenY;
    const pos = await dotnet.invokeMethodAsync("GetWindowPosition");
    winX = pos[0];
    winY = pos[1];
    dragging = true;
    captureZone = zone;
    capturePointer = e.pointerId;
    try {
        zone.setPointerCapture(e.pointerId);
    } catch (err) {
    }
}

function onPointerMove(e) {
    if (!dragging) {
        return;
    }

    const dpr = window.devicePixelRatio || 1;
    const x = Math.round(winX + (e.screenX - startScreenX) * dpr);
    const y = Math.round(winY + (e.screenY - startScreenY) * dpr);
    dotnet.invokeMethodAsync("MoveWindow", x, y);
}

function onPointerUp() {
    dragging = false;
    if (captureZone) {
        try {
            captureZone.releasePointerCapture(capturePointer);
        } catch (err) {
        }

        captureZone = null;
    }
}
