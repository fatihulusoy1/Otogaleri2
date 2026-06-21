import { useCallback, useEffect, useRef, useState } from "react";
import { resolveFileUrl } from "../lib/api";
import type { VehiclePhoto } from "../types";

interface PhotoLightboxProps {
  photos: VehiclePhoto[];
  initialIndex?: number;
  title?: string;
  // Verilirse altta "Ana ekran resmi yap" butonu gosterilir; secilen foto kapak (ilk) olur.
  onSetCover?: (photoId: string) => void | Promise<void>;
  onClose: () => void;
}

const SWIPE_THRESHOLD = 50;

export function PhotoLightbox({ photos, initialIndex = 0, title, onSetCover, onClose }: PhotoLightboxProps) {
  // Foto'yu id ile takip ediyoruz; kapak degisince dizi yeniden siralanir ama goruntulenen ayni kalir.
  const [currentId, setCurrentId] = useState(() => photos[initialIndex]?.id ?? photos[0]?.id);
  const [savingCover, setSavingCover] = useState(false);
  const touchStartX = useRef<number | null>(null);

  const total = photos.length;
  const index = Math.max(0, photos.findIndex((photo) => photo.id === currentId));

  const showPrev = useCallback(() => {
    setCurrentId(photos[(index - 1 + total) % total]?.id);
  }, [photos, index, total]);

  const showNext = useCallback(() => {
    setCurrentId(photos[(index + 1) % total]?.id);
  }, [photos, index, total]);

  useEffect(() => {
    function handleKey(event: KeyboardEvent) {
      if (event.key === "Escape") {
        onClose();
      } else if (event.key === "ArrowLeft") {
        showPrev();
      } else if (event.key === "ArrowRight") {
        showNext();
      }
    }

    document.addEventListener("keydown", handleKey);
    return () => document.removeEventListener("keydown", handleKey);
  }, [onClose, showPrev, showNext]);

  if (total === 0) {
    return null;
  }

  const current = photos[index] ?? photos[0];
  const isCover = index === 0;

  function handleTouchStart(event: React.TouchEvent) {
    touchStartX.current = event.touches[0]?.clientX ?? null;
  }

  function handleTouchEnd(event: React.TouchEvent) {
    if (touchStartX.current === null) {
      return;
    }

    const deltaX = (event.changedTouches[0]?.clientX ?? 0) - touchStartX.current;
    touchStartX.current = null;

    if (total <= 1 || Math.abs(deltaX) < SWIPE_THRESHOLD) {
      return;
    }

    if (deltaX > 0) {
      showPrev();
    } else {
      showNext();
    }
  }

  async function handleSetCover() {
    if (!onSetCover || isCover) {
      return;
    }

    setSavingCover(true);
    try {
      await onSetCover(current.id);
    } finally {
      setSavingCover(false);
    }
  }

  return (
    <div className="lightbox-backdrop" onClick={onClose}>
      <button type="button" className="lightbox-close" onClick={onClose} aria-label="Kapat">
        ×
      </button>

      {title ? <div className="lightbox-title">{title}</div> : null}

      <div
        className="lightbox-stage"
        onClick={(event) => event.stopPropagation()}
        onTouchStart={handleTouchStart}
        onTouchEnd={handleTouchEnd}
      >
        {total > 1 ? (
          <button type="button" className="lightbox-nav prev" onClick={showPrev} aria-label="Onceki">
            ‹
          </button>
        ) : null}

        <img className="lightbox-image" src={resolveFileUrl(current.fileUrl)} alt={current.fileName} />

        {total > 1 ? (
          <button type="button" className="lightbox-nav next" onClick={showNext} aria-label="Sonraki">
            ›
          </button>
        ) : null}
      </div>

      <div className="lightbox-footer" onClick={(event) => event.stopPropagation()}>
        {total > 1 ? (
          <span className="lightbox-counter">
            {index + 1} / {total}
          </span>
        ) : null}

        {onSetCover ? (
          <button
            type="button"
            className="lightbox-cover-button"
            onClick={handleSetCover}
            disabled={isCover || savingCover}
          >
            {isCover ? "✓ Ana ekran resmi" : savingCover ? "Ayarlanıyor..." : "Ana ekran resmi yap"}
          </button>
        ) : null}
      </div>
    </div>
  );
}
