import { useRef, useState } from "react";
import { api, resolveFileUrl } from "../lib/api";
import { downscaleImage } from "../lib/image";
import type { Vehicle, VehiclePhoto } from "../types";

function PaperclipIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      width="13"
      height="13"
      fill="none"
      stroke="currentColor"
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d="M21.44 11.05l-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48" />
    </svg>
  );
}

interface VehiclePhotoThumbProps {
  vehicle: Vehicle;
  token: string;
  small?: boolean;
  onView: () => void;
  onManage: () => void;
  onUploaded: (photos: VehiclePhoto[]) => void;
  onError?: (message: string) => void;
}

export function VehiclePhotoThumb({
  vehicle,
  token,
  small,
  onView,
  onManage,
  onUploaded,
  onError
}: VehiclePhotoThumbProps) {
  const sizeClass = small ? "vehicle-thumb vehicle-thumb--sm" : "vehicle-thumb";
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const [uploading, setUploading] = useState(false);

  async function handleFilesSelected(event: React.ChangeEvent<HTMLInputElement>) {
    const selected = event.target.files ? Array.from(event.target.files) : [];
    event.target.value = "";

    if (selected.length === 0) {
      return;
    }

    setUploading(true);
    try {
      const prepared = await Promise.all(selected.map((file) => downscaleImage(file)));
      const created = await api.uploadVehiclePhotos(token, vehicle.id, prepared);
      onUploaded(created);
    } catch (uploadError) {
      onError?.(uploadError instanceof Error ? uploadError.message : "Fotoğraflar yüklenemedi.");
    } finally {
      setUploading(false);
    }
  }

  // Fotograf yoksa: tek dokunusta dogrudan kamera/galeri secimi (modal acilmaz).
  if (vehicle.photos.length === 0) {
    return (
      <>
        <button
          type="button"
          className={`${sizeClass} vehicle-thumb-empty`}
          onClick={() => fileInputRef.current?.click()}
          disabled={uploading}
          aria-label="Fotoğraf ekle"
        >
          <span aria-hidden="true">＋</span>
          <small>{uploading ? "Yükleniyor..." : "Fotoğraf ekle"}</small>
        </button>
        <input
          ref={fileInputRef}
          type="file"
          accept="image/*"
          multiple
          hidden
          onChange={handleFilesSelected}
        />
      </>
    );
  }

  return (
    <div className={sizeClass}>
      <button type="button" className="vehicle-thumb-image" onClick={onView} aria-label="Fotoğrafları görüntüle">
        <img src={resolveFileUrl(vehicle.photos[0].fileUrl)} alt={vehicle.plate} loading="lazy" />
      </button>
      {vehicle.photos.length > 1 ? <span className="vehicle-thumb-count">{vehicle.photos.length}</span> : null}
      <button type="button" className="vehicle-thumb-clip" onClick={onManage} aria-label="Fotoğrafları yönet">
        <PaperclipIcon />
      </button>
    </div>
  );
}
