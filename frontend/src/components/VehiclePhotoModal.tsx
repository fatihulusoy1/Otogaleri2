import { useRef, useState } from "react";
import { PhotoLightbox } from "./PhotoLightbox";
import { api, resolveFileUrl } from "../lib/api";
import { downscaleImage } from "../lib/image";
import type { VehiclePhoto } from "../types";

interface VehiclePhotoModalProps {
  token: string;
  vehicleId: string;
  vehicleTitle: string;
  initialPhotos: VehiclePhoto[];
  onClose: () => void;
  onChanged: (photos: VehiclePhoto[]) => void;
}

export function VehiclePhotoModal({
  token,
  vehicleId,
  vehicleTitle,
  initialPhotos,
  onClose,
  onChanged
}: VehiclePhotoModalProps) {
  const [photos, setPhotos] = useState<VehiclePhoto[]>(initialPhotos);
  const [uploading, setUploading] = useState(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [error, setError] = useState("");
  const [lightboxIndex, setLightboxIndex] = useState<number | null>(null);
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  async function handleFilesSelected(event: React.ChangeEvent<HTMLInputElement>) {
    const selected = event.target.files ? Array.from(event.target.files) : [];
    // Ayni dosyalar tekrar secilebilsin diye input'u sifirla.
    event.target.value = "";

    if (selected.length === 0) {
      return;
    }

    setUploading(true);
    setError("");

    try {
      const prepared = await Promise.all(selected.map((file) => downscaleImage(file)));
      const created = await api.uploadVehiclePhotos(token, vehicleId, prepared);
      const next = [...photos, ...created];
      setPhotos(next);
      onChanged(next);
    } catch (uploadError) {
      setError(uploadError instanceof Error ? uploadError.message : "Fotograflar yuklenemedi.");
    } finally {
      setUploading(false);
    }
  }

  async function handleSetCover(photoId: string) {
    setError("");
    try {
      const next = await api.setVehiclePhotoCover(token, photoId);
      setPhotos(next);
      onChanged(next);
    } catch (coverError) {
      setError(coverError instanceof Error ? coverError.message : "Ana ekran resmi ayarlanamadı.");
    }
  }

  async function handleDelete(photoId: string) {
    setDeletingId(photoId);
    setError("");

    try {
      await api.deleteVehiclePhoto(token, photoId);
      const next = photos.filter((photo) => photo.id !== photoId);
      setPhotos(next);
      onChanged(next);
    } catch (deleteError) {
      setError(deleteError instanceof Error ? deleteError.message : "Fotograf silinemedi.");
    } finally {
      setDeletingId(null);
    }
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <article className="panel modal-panel photo-modal" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <div className="panel-heading">
            <h2>Araç fotoğrafları</h2>
            <p>{vehicleTitle}</p>
          </div>
          <div className="modal-header-actions">
            <button
              type="button"
              className="primary-button"
              onClick={() => fileInputRef.current?.click()}
              disabled={uploading}
            >
              {uploading ? "Yükleniyor..." : "Fotoğraf ekle"}
            </button>
            <button type="button" className="modal-close" onClick={onClose} aria-label="Kapat">
              X
            </button>
          </div>
          <input
            ref={fileInputRef}
            type="file"
            accept="image/*"
            multiple
            hidden
            onChange={handleFilesSelected}
          />
        </div>

        <div className="photo-modal-body">
          {error ? <div className="alert error">{error}</div> : null}

          {photos.length === 0 ? (
            <div className="empty-panel">Bu araca ait fotoğraf bulunmuyor. Eklemek için "Fotoğraf ekle"ye basın.</div>
          ) : (
            <div className="photo-grid">
              {photos.map((photo, photoIndex) => (
                <div key={photo.id} className="photo-grid-item">
                  <button
                    type="button"
                    className="photo-grid-thumb"
                    onClick={() => setLightboxIndex(photoIndex)}
                    aria-label="Fotoğrafı büyüt"
                  >
                    <img src={resolveFileUrl(photo.fileUrl)} alt={photo.fileName} loading="lazy" />
                  </button>
                  <button
                    type="button"
                    className="photo-grid-delete"
                    onClick={() => handleDelete(photo.id)}
                    disabled={deletingId === photo.id}
                    aria-label="Fotoğrafı sil"
                  >
                    {deletingId === photo.id ? "…" : "×"}
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="inline-actions photo-modal-footer">
          <button type="button" className="ghost-button dark" onClick={onClose}>
            Kapat
          </button>
        </div>
      </article>

      {lightboxIndex !== null ? (
        <PhotoLightbox
          photos={photos}
          initialIndex={lightboxIndex}
          title={vehicleTitle}
          onSetCover={handleSetCover}
          onClose={() => setLightboxIndex(null)}
        />
      ) : null}
    </div>
  );
}
