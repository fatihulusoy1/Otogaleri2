interface ConfirmDialogProps {
  title: string;
  description: string;
  error?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  onConfirm: () => void;
  onCancel: () => void;
  busy?: boolean;
}

export function ConfirmDialog({
  title,
  description,
  error,
  confirmLabel = "Sil",
  cancelLabel = "Vazgec",
  onConfirm,
  onCancel,
  busy = false
}: ConfirmDialogProps) {
  return (
    <div className="modal-backdrop">
      <div className="panel modal-panel confirm-panel">
        <div className="modal-header">
          <div className="panel-heading">
            <h2>{title}</h2>
          </div>
          <button type="button" className="modal-close" onClick={onCancel} aria-label="Kapat">
            X
          </button>
        </div>

        <div className="confirm-body">
          <p className="confirm-copy">{description}</p>

          {error ? <div className="alert error">{error}</div> : null}

          <div className="inline-actions">
            <button type="button" className="ghost-button dark" onClick={onCancel} disabled={busy}>
              {cancelLabel}
            </button>
            <button type="button" className="ghost-button danger" onClick={onConfirm} disabled={busy}>
              {busy ? "Isleniyor..." : confirmLabel}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
