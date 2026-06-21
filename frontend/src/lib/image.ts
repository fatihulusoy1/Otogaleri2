// Yuklemeden once tarayicida fotografi kucultur (bant genisligini azaltir).
// Sunucu ayrica yeniden boyutlandirir; bu adim sadece upload boyutunu dusurmek icindir.
const MAX_EDGE = 1920;
const QUALITY = 0.85;

export async function downscaleImage(file: File, maxEdge = MAX_EDGE, quality = QUALITY): Promise<File> {
  if (!file.type.startsWith("image/")) {
    return file;
  }

  try {
    const bitmap = await createImageBitmap(file);
    const scale = Math.min(1, maxEdge / Math.max(bitmap.width, bitmap.height));

    // Zaten kucukse ve formati uygunsa dokunma.
    if (scale >= 1 && (file.type === "image/jpeg" || file.type === "image/webp")) {
      bitmap.close?.();
      return file;
    }

    const targetWidth = Math.round(bitmap.width * scale);
    const targetHeight = Math.round(bitmap.height * scale);

    const canvas = document.createElement("canvas");
    canvas.width = targetWidth;
    canvas.height = targetHeight;

    const context = canvas.getContext("2d");
    if (!context) {
      bitmap.close?.();
      return file;
    }

    context.drawImage(bitmap, 0, 0, targetWidth, targetHeight);
    bitmap.close?.();

    const blob = await new Promise<Blob | null>((resolve) =>
      canvas.toBlob(resolve, "image/jpeg", quality)
    );

    if (!blob) {
      return file;
    }

    const newName = file.name.replace(/\.[^.]+$/, "") + ".jpg";
    return new File([blob], newName, { type: "image/jpeg", lastModified: Date.now() });
  } catch {
    // HEIC gibi tarayicinin cozemedigi formatlarda orijinali gonder; sunucu islesin.
    return file;
  }
}
