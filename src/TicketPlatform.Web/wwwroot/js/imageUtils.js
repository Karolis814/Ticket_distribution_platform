window.cropImageTo = (imageBytes, contentType, targetWidth, targetHeight) => {
    return new Promise((resolve) => {
        const blob = new Blob([new Uint8Array(imageBytes)], { type: contentType });
        const url = URL.createObjectURL(blob);
        const img = new Image();
        img.onload = () => {
            URL.revokeObjectURL(url);
            const canvas = document.createElement('canvas');
            canvas.width = targetWidth;
            canvas.height = targetHeight;
            const ctx = canvas.getContext('2d');

            const targetRatio = targetWidth / targetHeight;
            const sourceRatio = img.width / img.height;

            let sx, sy, sw, sh;
            if (sourceRatio > targetRatio) {
                sh = img.height;
                sw = img.height * targetRatio;
                sy = 0;
                sx = (img.width - sw) / 2;
            } else {
                sw = img.width;
                sh = img.width / targetRatio;
                sx = 0;
                sy = (img.height - sh) / 2;
            }

            ctx.drawImage(img, sx, sy, sw, sh, 0, 0, targetWidth, targetHeight);
            canvas.toBlob((croppedBlob) => {
                croppedBlob.arrayBuffer().then(buf => {
                    resolve(new Uint8Array(buf));
                });
            }, 'image/jpeg', 0.92);
        };
        img.src = url;
    });
};
