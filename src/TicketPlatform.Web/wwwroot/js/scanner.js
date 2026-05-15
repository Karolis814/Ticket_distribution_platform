let stream = null;
let animId = null;
let paused = false;
let dotNet = null;   // DotNetObjectReference<ScannerBase>
let videoEl = null;
const canvas = document.createElement("canvas");
const ctx = canvas.getContext("2d", {willReadFrequently: true});

export async function startScanner(videoElementId, dotNetRef) {
    dotNet = dotNetRef;
    videoEl = document.getElementById(videoElementId);

    if (!videoEl) {
        console.error("[scanner] video element not found:", videoElementId);
        return;
    }

    try {
        stream = await navigator.mediaDevices.getUserMedia({
            video: {facingMode: "environment"}   // rear camera on mobile
        });
        videoEl.srcObject = stream;
        await videoEl.play();
        paused = false;
        tick();
    } catch (err) {
        console.error("[scanner] getUserMedia failed:", err);
    }
}

export function stopScanner() {
    paused = true;
    if (animId) {
        cancelAnimationFrame(animId);
        animId = null;
    }
    if (stream) {
        stream.getTracks().forEach(t => t.stop());
        stream = null;
    }
    if (videoEl) {
        videoEl.srcObject = null;
        videoEl = null;
    }
}

export function pauseScanner() {
    paused = true;
}

export function resumeScanner() {
    paused = false;
    tick();
}

function tick() {
    if (paused || !videoEl || videoEl.readyState < HTMLMediaElement.HAVE_ENOUGH_DATA) {
        animId = requestAnimationFrame(tick);
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
        paused = true;   // stop decoding until .NET tells us to resume
        dotNet.invokeMethodAsync("OnQrDecoded", code.data);
        return;          // don't schedule next frame yet
    }

    animId = requestAnimationFrame(tick);
}
