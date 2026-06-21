import { useState } from "react";
import { api } from "../lib/api";
import { formatCurrency, toDateTimeLocalValue, toIsoFromLocalValue } from "../lib/format";
import type { CompleteStockConsignmentSaleRequest, StockConsignment } from "../types";

interface StockConsignmentSaleFormModalProps {
  token: string;
  record: StockConsignment;
  onClose: () => void;
  onSaved: () => void;
}

export function StockConsignmentSaleFormModal({ token, record, onClose, onSaved }: StockConsignmentSaleFormModalProps) {
  const [form, setForm] = useState<CompleteStockConsignmentSaleRequest>(() => ({
    salePrice: record.salePrice ?? 0,
    saleDate: record.saleDate ?? new Date().toISOString(),
    commissionAmount: record.commissionAmount,
    commissionRate: record.commissionRate,
    description: record.description ?? ""
  }));
  const [salePriceInput, setSalePriceInput] = useState(() => (record.salePrice ? formatCurrency(record.salePrice) : ""));
  const [commissionAmountInput, setCommissionAmountInput] = useState(() =>
    record.commissionAmount ? formatCurrency(record.commissionAmount) : ""
  );
  const [commissionRateInput, setCommissionRateInput] = useState(() => record.commissionRate?.toString() ?? "");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const isUpdate = record.status === 3;
  const vehicleName = [record.segment, record.brand, record.model].filter(Boolean).join(" / ");

  function handleCurrencyInput(value: string, setter: (value: string) => void) {
    const digits = value.replace(/\D/g, "");
    setter(digits ? formatCurrency(Number(digits)) : "");
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const salePrice = Number.parseInt(salePriceInput.replace(/\D/g, ""), 10);
    const commissionAmount = Number.parseInt(commissionAmountInput.replace(/\D/g, ""), 10);
    const commissionRate = Number.parseFloat(commissionRateInput.replace(",", "."));

    if (!salePrice) {
      setError("Satis tutari girmeniz gerekiyor.");
      return;
    }

    setSaving(true);
    setError("");

    try {
      const payload = {
        ...form,
        salePrice,
        commissionAmount: commissionAmount || null,
        commissionRate: Number.isFinite(commissionRate) ? commissionRate : null
      };

      if (isUpdate) {
        await api.updateStockConsignmentSale(token, record.id, payload);
      } else {
        await api.completeStockConsignmentSale(token, record.id, payload);
      }

      onSaved();
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : "Konsinye satisi kaydedilemedi.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="modal-backdrop">
      <form className="panel modal-panel data-form" onSubmit={handleSubmit}>
        <div className="modal-header">
          <div className="panel-heading">
            <h2>{isUpdate ? "Konsinye satis duzenle" : "Konsinye satis yap"}</h2>
          </div>
          <button type="button" className="modal-close" onClick={onClose} aria-label="Kapat">X</button>
        </div>

        <div className="panel-heading">
          <h2>{record.plate}</h2>
          <p>{vehicleName}</p>
        </div>

        <div className="form-grid">
          <label>
            <span>Satis tutari</span>
            <input inputMode="numeric" value={salePriceInput} onChange={(event) => handleCurrencyInput(event.target.value, setSalePriceInput)} required />
          </label>
          <label>
            <span>Satis tarihi</span>
            <input type="datetime-local" value={toDateTimeLocalValue(form.saleDate)} onChange={(event) => setForm((current) => ({ ...current, saleDate: toIsoFromLocalValue(event.target.value) }))} required />
          </label>
          <label>
            <span>Komisyon tutari</span>
            <input inputMode="numeric" value={commissionAmountInput} onChange={(event) => handleCurrencyInput(event.target.value, setCommissionAmountInput)} />
          </label>
          <label>
            <span>Komisyon orani</span>
            <input value={commissionRateInput} onChange={(event) => setCommissionRateInput(event.target.value)} placeholder="Orn. 5" />
          </label>
        </div>

        <label>
          <span>Aciklama</span>
          <textarea rows={3} value={form.description} onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))} />
        </label>

        {error ? <div className="alert error">{error}</div> : null}

        <div className="inline-actions">
          <button type="submit" className="primary-button" disabled={saving}>
            {saving ? "Kaydediliyor..." : isUpdate ? "Satisi guncelle" : "Satisi tamamla"}
          </button>
          <button type="button" className="ghost-button dark" onClick={onClose}>Iptal</button>
        </div>
      </form>
    </div>
  );
}
