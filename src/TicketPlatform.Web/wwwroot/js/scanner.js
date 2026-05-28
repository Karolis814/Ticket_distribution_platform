let stream = null;
let animId = null;
let paused = false;
let dotNet = null;
let videoEl = null;
const canvas = document.createElement("canvas");
const ctx = canvas.getContext("2d", { willReadFrequently: true });

export async function startScanner(videoElementId, dotNetRef) {
    dotNet = dotNetRef;
    videoEl = document.getElementById(videoElementId);

    if (!videoEl) {
        console.error("[scanner] video element not found:", videoElementId);
        return;
    }

    try {
        stream = await navigator.mediaDevices.getUserMedia({
            video: { facingMode: "environment" }
        });
        videoEl.srcObject = stream;
        await videoEl.play();
        paused = false;
        scheduleTick();
    } catch (err) {
        console.error("[scanner] getUserMedia failed:", err);
    }
}

export function stopScanner() {
    paused = true;
    cancelTick();
    if (stream) {
        stream.getTracks().forEach(t => t.stop());
        stream = null;
    }
    if (videoEl) {
        videoEl.srcObject = null;
        videoEl = null;
    }
    dotNet = null;
}

export function pauseScanner() {
    paused = true;
    cancelTick();
}

export function resumeScanner() {
    if (!paused) return;
    paused = false;
    scheduleTick();
}

function cancelTick() {
    if (animId !== null) {
        cancelAnimationFrame(animId);
        animId = null;
    }
}

function scheduleTick() {
    if (!paused && videoEl) {
        animId = requestAnimationFrame(tick);
    }
}

function tick() {
    animId = null;

    if (paused || !videoEl) return;

    if (videoEl.readyState < HTMLMediaElement.HAVE_ENOUGH_DATA) {
        scheduleTick();
        return;
    }

    canvas.width = videoEl.videoWidth;
    canvas.height = videoEl.videoHeight;
    ctx.drawImage(videoEl, 0, 0, canvas.width, canvas.height);

    const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
    const code = jsQR(imageData.data, imageData.width, imageData.height, {
        inversionAttempts: "dontInvert"
    });

    if (code) {
        paused = true;
        dotNet.invokeMethodAsync("OnQrDecoded", code.data)
            .catch(err => {
                console.error("[scanner] OnQrDecoded failed:", err);
                paused = false;
                scheduleTick();
            });
        return;
    }

    scheduleTick();
}
