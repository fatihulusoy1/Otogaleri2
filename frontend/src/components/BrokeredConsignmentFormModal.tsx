import { useMemo, useState } from "react";
import { SearchableSelect } from "./SearchableSelect";
import { api } from "../lib/api";
import { formatCurrency, toDateTimeLocalValue, toIsoFromLocalValue } from "../lib/format";
import type { BrokeredConsignment, CreateBrokeredConsignmentRequest, VehicleLookups } from "../types";

const createInitialForm = (): CreateBrokeredConsignmentRequest => ({
  ownerName: "",
  ownerPhone: "",
  customerName: "",
  customerPhone: "",
  plate: "",
  segmentId: "",
  brandId: "",
  modelId: "",
  year: new Date().getFullYear(),
  color: "",
  engineNumber: "",
  chassisNumber: "",
  consignmentDate: new Date().toISOString(),
  purchasePrice: 0,
  commissionAmount: null,
  commissionRate: null,
  description: ""
});

function buildFormFromRecord(record: BrokeredConsignment): CreateBrokeredConsignmentRequest {
  return {
    ownerName: record.ownerName,
    ownerPhone: record.ownerPhone,
    customerName: record.customerName,
    customerPhone: record.customerPhone,
    plate: record.plate,
    segmentId: record.segmentId ?? "",
    brandId: record.brandId ?? "",
    modelId: record.modelId ?? "",
    year: record.year,
    color: record.color,
    engineNumber: record.engineNumber,
    chassisNumber: record.chassisNumber,
    consignmentDate: record.consignmentDate,
    purchasePrice: record.purchasePrice,
    commissionAmount: record.commissionAmount,
    commissionRate: record.commissionRate,
    description: record.description ?? ""
  };
}

interface BrokeredConsignmentFormModalProps {
  token: string;
  lookups: VehicleLookups;
  editing?: BrokeredConsignment | null;
  onClose: () => void;
  onSaved: () => void;
}

export function BrokeredConsignmentFormModal({ token, lookups, editing, onClose, onSaved }: BrokeredConsignmentFormModalProps) {
  const [form, setForm] = useState<CreateBrokeredConsignmentRequest>(() =>
    editing ? buildFormFromRecord(editing) : createInitialForm()
  );
  const [purchasePriceInput, setPurchasePriceInput] = useState(() =>
    editing ? formatCurrency(editing.purchasePrice) : ""
  );
  const [commissionAmountInput, setCommissionAmountInput] = useState(() =>
    editing?.commissionAmount ? formatCurrency(editing.commissionAmount) : ""
  );
  const [commissionRateInput, setCommissionRateInput] = useState(() => editing?.commissionRate?.toString() ?? "");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const editingId = editing?.id ?? null;

  const filteredModels = useMemo(
    () =>
      lookups.models
        .filter((model) => (!form.brandId || model.brandId === form.brandId))
        .filter((model) => (!form.segmentId || !model.segmentId || model.segmentId === form.segmentId))
        .map((model) => ({ id: model.id, name: model.name })),
    [form.brandId, form.segmentId, lookups.models]
  );

  function handleCurrencyInput(value: string, setter: (value: string) => void) {
    const digits = value.replace(/\D/g, "");
    setter(digits ? formatCurrency(Number(digits)) : "");
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const purchasePrice = Number.parseInt(purchasePriceInput.replace(/\D/g, ""), 10);
    const commissionAmount = Number.parseInt(commissionAmountInput.replace(/\D/g, ""), 10);
    const commissionRate = Number.parseFloat(commissionRateInput.replace(",", "."));

    if (!purchasePrice) {
      setError("Alim tutari girmeniz gerekiyor.");
      return;
    }

    setSaving(true);
    setError("");

    try {
      const payload = {
        ...form,
        purchasePrice,
        commissionAmount: commissionAmount || null,
        commissionRate: Number.isFinite(commissionRate) ? commissionRate : null
      };

      if (editingId) {
        await api.updateBrokeredConsignment(token, editingId, payload);
      } else {
        await api.createBrokeredConsignment(token, payload);
      }

      onSaved();
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : "Konsinye kaydi kaydedilemedi.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="modal-backdrop">
      <form className="panel modal-panel data-form" onSubmit={handleSubmit}>
        <div className="modal-header">
          <div className="panel-heading">
            <h2>{editingId ? "Aracilik kaydi duzenle" : "Yeni aracilik kaydi"}</h2>
          </div>
          <button type="button" className="modal-close" onClick={onClose} aria-label="Kapat">
            X
          </button>
        </div>

        <div className="form-grid">
          <label>
            <span>Arac sahibi</span>
            <input value={form.ownerName} onChange={(event) => setForm((current) => ({ ...current, ownerName: event.target.value }))} required />
          </label>
          <label>
            <span>Sahip telefonu</span>
            <input value={form.ownerPhone ?? ""} onChange={(event) => setForm((current) => ({ ...current, ownerPhone: event.target.value }))} />
          </label>
          <label>
            <span>Alici adi</span>
            <input value={form.customerName ?? ""} onChange={(event) => setForm((current) => ({ ...current, customerName: event.target.value }))} />
          </label>
          <label>
            <span>Alici telefonu</span>
            <input value={form.customerPhone ?? ""} onChange={(event) => setForm((current) => ({ ...current, customerPhone: event.target.value }))} />
          </label>
          <label>
            <span>Plaka</span>
            <input value={form.plate} onChange={(event) => setForm((current) => ({ ...current, plate: event.target.value }))} required />
          </label>
          <SearchableSelect label="Segment" value={form.segmentId} options={lookups.segments} placeholder="Segment secin" onChange={(segmentId) => setForm((current) => ({ ...current, segmentId, modelId: "" }))} />
          <SearchableSelect label="Marka" value={form.brandId} options={lookups.brands} placeholder="Marka secin" onChange={(brandId) => setForm((current) => ({ ...current, brandId, modelId: "" }))} />
          <SearchableSelect label="Model" value={form.modelId} options={filteredModels} placeholder="Model secin" onChange={(modelId) => setForm((current) => ({ ...current, modelId }))} disabled={!form.brandId} />
          <label>
            <span>Yil</span>
            <input type="number" value={form.year} onChange={(event) => setForm((current) => ({ ...current, year: Number(event.target.value) }))} required />
          </label>
          <label>
            <span>Renk</span>
            <input value={form.color} onChange={(event) => setForm((current) => ({ ...current, color: event.target.value }))} required />
          </label>
          <label>
            <span>Motor no</span>
            <input value={form.engineNumber} onChange={(event) => setForm((current) => ({ ...current, engineNumber: event.target.value }))} required />
          </label>
          <label>
            <span>Sasi no</span>
            <input value={form.chassisNumber} onChange={(event) => setForm((current) => ({ ...current, chassisNumber: event.target.value }))} required />
          </label>
          <label>
            <span>Islem tarihi</span>
            <input type="datetime-local" value={toDateTimeLocalValue(form.consignmentDate)} onChange={(event) => setForm((current) => ({ ...current, consignmentDate: toIsoFromLocalValue(event.target.value) }))} required />
          </label>
          <label>
            <span>Alim tutari</span>
            <input inputMode="numeric" value={purchasePriceInput} onChange={(event) => handleCurrencyInput(event.target.value, setPurchasePriceInput)} required />
          </label>
          <label>
            <span>Komisyon tutari</span>
            <input inputMode="numeric" value={commissionAmountInput} onChange={(event) => handleCurrencyInput(event.target.value, setCommissionAmountInput)} />
          </label>
          <label>
            <span>Komisyon orani</span>
            <input value={commissionRateInput} onChange={(event) => setCommissionRateInput(event.target.value)} placeholder="Orn. 4.5" />
          </label>
        </div>

        <label>
          <span>Aciklama</span>
          <textarea rows={3} value={form.description} onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))} />
        </label>

        {error ? <div className="alert error">{error}</div> : null}

        <div className="inline-actions">
          <button type="submit" className="primary-button" disabled={saving}>
            {saving ? "Kaydediliyor..." : editingId ? "Degisiklikleri kaydet" : "Kaydi kaydet"}
          </button>
          <button type="button" className="ghost-button dark" onClick={onClose}>
            Iptal
          </button>
        </div>
      </form>
    </div>
  );
}
