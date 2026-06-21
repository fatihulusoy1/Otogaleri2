import { useState } from "react";
import { SearchableSelect } from "./SearchableSelect";
import { api } from "../lib/api";
import { formatCurrency, toDateTimeLocalValue, toIsoFromLocalValue } from "../lib/format";
import type { CreatePurchaseRequest, PurchaseRecord, VehicleLookups } from "../types";

const createInitialForm = (): CreatePurchaseRequest => ({
  plate: "",
  segmentId: "",
  brandId: "",
  modelId: "",
  year: new Date().getFullYear(),
  color: "",
  engineNumber: "",
  chassisNumber: "",
  purchaseDate: new Date().toISOString(),
  purchasePrice: 0,
  paymentMethod: 1,
  notaryRegistryNumber: null,
  counterpartyName: null,
  dueDate: null,
  documentNumber: null,
  installmentCount: null,
  installmentIntervalMonths: null,
  tradePlate: null,
  tradeAmount: null,
  targetSalePrice: null,
  description: ""
});

function toDateOnlyInputValue(value: string | null): string {
  if (!value) {
    return "";
  }

  const date = new Date(value);
  const pad = (part: number) => part.toString().padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

function toIsoDateValue(value: string): string | null {
  if (!value) {
    return null;
  }

  const date = new Date(`${value}T12:00:00`);
  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}

function isPurchaseFinanceMethod(paymentMethod: number) {
  return paymentMethod === 5 || paymentMethod === 6 || paymentMethod === 7;
}

function isTradePaymentMethod(paymentMethod: number) {
  return paymentMethod === 4;
}

function buildFormFromPurchase(purchase: PurchaseRecord): CreatePurchaseRequest {
  return {
    plate: purchase.plate,
    segmentId: purchase.segmentId ?? "",
    brandId: purchase.brandId ?? "",
    modelId: purchase.modelId ?? "",
    year: purchase.year,
    color: purchase.color,
    engineNumber: purchase.engineNumber,
    chassisNumber: purchase.chassisNumber,
    purchaseDate: purchase.purchasedAt,
    purchasePrice: purchase.purchasePrice,
    paymentMethod: purchase.paymentMethod,
    notaryRegistryNumber: purchase.notaryRegistryNumber,
    counterpartyName: purchase.counterpartyName,
    dueDate: purchase.dueDate,
    documentNumber: purchase.documentNumber,
    installmentCount: purchase.installmentCount,
    installmentIntervalMonths: purchase.installmentIntervalMonths,
    tradePlate: purchase.tradePlate,
    tradeAmount: purchase.tradeAmount,
    targetSalePrice: purchase.targetSalePrice,
    description: purchase.description ?? ""
  };
}

interface PurchaseFormModalProps {
  token: string;
  lookups: VehicleLookups;
  editing?: PurchaseRecord | null;
  onClose: () => void;
  onSaved: () => void;
}

export function PurchaseFormModal({ token, lookups, editing, onClose, onSaved }: PurchaseFormModalProps) {
  const [form, setForm] = useState<CreatePurchaseRequest>(() =>
    editing ? buildFormFromPurchase(editing) : createInitialForm()
  );
  const [purchasePriceInput, setPurchasePriceInput] = useState(() =>
    editing ? formatCurrency(editing.purchasePrice) : ""
  );
  const [tradeAmountInput, setTradeAmountInput] = useState(() =>
    editing?.tradeAmount ? formatCurrency(editing.tradeAmount) : ""
  );
  const [targetSalePriceInput, setTargetSalePriceInput] = useState(() =>
    editing?.targetSalePrice ? formatCurrency(editing.targetSalePrice) : ""
  );
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const editingVehicleId = editing?.vehicleId ?? null;

  const filteredModels = lookups.models
    .filter((model) => (!form.brandId || model.brandId === form.brandId))
    .filter((model) => (!form.segmentId || !model.segmentId || model.segmentId === form.segmentId))
    .map((model) => ({ id: model.id, name: model.name }));

  function handleCurrencyInput(value: string, setter: (value: string) => void) {
    const digits = value.replace(/\D/g, "");
    setter(digits ? formatCurrency(Number(digits)) : "");
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!form.segmentId || !form.brandId || !form.modelId) {
      setError("Segment, marka ve model seçimi zorunludur.");
      return;
    }

    setSaving(true);
    setError("");

    try {
      const numericPurchasePrice = Number.parseInt(purchasePriceInput.replace(/\D/g, ""), 10);
      const numericTradeAmount = Number.parseInt(tradeAmountInput.replace(/\D/g, ""), 10);
      const numericTargetSalePrice = Number.parseInt(targetSalePriceInput.replace(/\D/g, ""), 10);
      const requiresFinanceInfo = isPurchaseFinanceMethod(form.paymentMethod);

      if (!numericPurchasePrice) {
        setError("Alış fiyatı girmeniz gerekiyor.");
        setSaving(false);
        return;
      }

      const hasTradeInfo = isTradePaymentMethod(form.paymentMethod);
      if (hasTradeInfo && (!form.tradePlate?.trim() || !numericTradeAmount)) {
        setError("Takas bilgisi giriyorsanız plaka ve takas bedelini birlikte doldurun.");
        setSaving(false);
        return;
      }

      if (requiresFinanceInfo && (!form.counterpartyName?.trim() || !form.dueDate)) {
        setError("Çek, senet veya vadeli alışta taraf ve vade tarihi zorunludur.");
        setSaving(false);
        return;
      }

      if (form.paymentMethod === 6 && (!form.installmentCount || !form.installmentIntervalMonths)) {
        setError("Senet alışında taksit sayısı ve taksit aralığı zorunludur.");
        setSaving(false);
        return;
      }

      const payload = {
        ...form,
        description: form.description || "",
        purchasePrice: numericPurchasePrice,
        notaryRegistryNumber: form.notaryRegistryNumber?.trim() || null,
        counterpartyName: requiresFinanceInfo ? form.counterpartyName?.trim() ?? null : null,
        dueDate: requiresFinanceInfo ? form.dueDate : null,
        documentNumber: requiresFinanceInfo ? form.documentNumber?.trim() ?? null : null,
        installmentCount: form.paymentMethod === 6 ? form.installmentCount : null,
        installmentIntervalMonths: form.paymentMethod === 6 ? form.installmentIntervalMonths : null,
        tradePlate: hasTradeInfo ? form.tradePlate?.trim().toUpperCase() ?? null : null,
        tradeAmount: hasTradeInfo ? numericTradeAmount || null : null,
        targetSalePrice: numericTargetSalePrice || null,
        year: Number(form.year)
      };

      if (editingVehicleId) {
        await api.updatePurchase(token, editingVehicleId, payload);
      } else {
        await api.createPurchase(token, payload);
      }

      onSaved();
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : "Satınalma kaydedilemedi.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="modal-backdrop">
      <form className="panel modal-panel data-form" onSubmit={handleSubmit}>
        <div className="modal-header">
          <div className="panel-heading">
            <h2>{editingVehicleId ? "Satınalma düzenle" : "Yeni satınalma"}</h2>
          </div>
          <button type="button" className="modal-close" onClick={onClose} aria-label="Kapat">
            X
          </button>
        </div>

        <div className="form-grid">
          <label>
            <span className="field-label required">Plaka</span>
            <input value={form.plate} onChange={(event) => setForm((current) => ({ ...current, plate: event.target.value }))} required />
          </label>
          <SearchableSelect
            label="Segment"
            value={form.segmentId}
            options={lookups.segments}
            placeholder="Segment seçin"
            onChange={(segmentId) => setForm((current) => ({ ...current, segmentId, modelId: "" }))}
            required
          />
          <SearchableSelect
            label="Marka"
            value={form.brandId}
            options={lookups.brands}
            placeholder="Marka seçin"
            onChange={(brandId) => setForm((current) => ({ ...current, brandId, modelId: "" }))}
            required
          />
          <SearchableSelect
            label="Model"
            value={form.modelId}
            options={filteredModels}
            placeholder="Model seçin"
            onChange={(modelId) => setForm((current) => ({ ...current, modelId }))}
            disabled={!form.brandId}
            required
          />
          <label>
            <span className="field-label required">Yıl</span>
            <input type="number" value={form.year} onChange={(event) => setForm((current) => ({ ...current, year: Number(event.target.value) }))} required />
          </label>
          <label>
            <span className="field-label required">Renk</span>
            <input value={form.color} onChange={(event) => setForm((current) => ({ ...current, color: event.target.value }))} required />
          </label>
          <label>
            <span className="field-label required">Satınalma tarihi</span>
            <input
              type="datetime-local"
              value={toDateTimeLocalValue(form.purchaseDate)}
              onChange={(event) => setForm((current) => ({ ...current, purchaseDate: toIsoFromLocalValue(event.target.value) }))}
              required
            />
          </label>
          <label>
            <span className="field-label required">Alış fiyatı</span>
            <input inputMode="numeric" value={purchasePriceInput} placeholder="Örn. 850.000 TL" onChange={(event) => handleCurrencyInput(event.target.value, setPurchasePriceInput)} required />
          </label>
          <label>
            <span className="field-label required">Ödeme yöntemi</span>
            <select
              value={form.paymentMethod}
              onChange={(event) =>
                setForm((current) => ({
                  ...current,
                  paymentMethod: Number(event.target.value) as 1 | 2 | 3 | 4 | 5 | 6 | 7,
                  counterpartyName: Number(event.target.value) >= 5 ? current.counterpartyName : null,
                  dueDate: Number(event.target.value) >= 5 ? current.dueDate : null,
                  documentNumber: Number(event.target.value) >= 5 ? current.documentNumber : null,
                  installmentCount: Number(event.target.value) === 6 ? current.installmentCount : null,
                  installmentIntervalMonths: Number(event.target.value) === 6 ? current.installmentIntervalMonths : null,
                  tradePlate: Number(event.target.value) === 4 ? current.tradePlate : null,
                  tradeAmount: Number(event.target.value) === 4 ? current.tradeAmount : null
                }))
              }
            >
              <option value={1}>Nakit</option>
              <option value={2}>Havale</option>
              <option value={3}>Kredi Kartı</option>
              <option value={4}>Takas</option>
              <option value={5}>Çek</option>
              <option value={6}>Senet</option>
              <option value={7}>Vadeli</option>
            </select>
          </label>
          <label>
            <span>Noter yevmiye no</span>
            <input
              value={form.notaryRegistryNumber ?? ""}
              placeholder="Örn. 2026/1458"
              onChange={(event) => setForm((current) => ({ ...current, notaryRegistryNumber: event.target.value || null }))}
            />
          </label>
          <label>
            <span>Hedef satış</span>
            <input inputMode="numeric" value={targetSalePriceInput} placeholder="Örn. 1.050.000 TL" onChange={(event) => handleCurrencyInput(event.target.value, setTargetSalePriceInput)} />
          </label>

          {form.paymentMethod === 4 ? (
            <>
              <label>
                <span className="field-label required">Takas aracının plakası</span>
                <input value={form.tradePlate ?? ""} placeholder="Örn. 34ABC657" onChange={(event) => setForm((current) => ({ ...current, tradePlate: event.target.value || null }))} />
              </label>
              <label>
                <span className="field-label required">Takas bedeli</span>
                <input inputMode="numeric" value={tradeAmountInput} placeholder="Örn. 110.000 TL" onChange={(event) => handleCurrencyInput(event.target.value, setTradeAmountInput)} />
              </label>
            </>
          ) : null}

          {isPurchaseFinanceMethod(form.paymentMethod) ? (
            <>
              <label>
                <span className="field-label required">Taraf bilgisi</span>
                <input value={form.counterpartyName ?? ""} placeholder="Örn. Mehmet Yılmaz / Auto Expert" onChange={(event) => setForm((current) => ({ ...current, counterpartyName: event.target.value || null }))} />
              </label>
              <label>
                <span className="field-label required">{form.paymentMethod === 6 ? "İlk taksit tarihi" : "Vade tarihi"}</span>
                <input type="date" value={toDateOnlyInputValue(form.dueDate)} onChange={(event) => setForm((current) => ({ ...current, dueDate: toIsoDateValue(event.target.value) }))} />
              </label>
              <label>
                <span>Belge numarası</span>
                <input value={form.documentNumber ?? ""} placeholder="Örn. CEK-2026-001" onChange={(event) => setForm((current) => ({ ...current, documentNumber: event.target.value || null }))} />
              </label>
              {form.paymentMethod === 6 ? (
                <>
                  <label>
                    <span className="field-label required">Taksit aralığı (ay)</span>
                    <input
                      type="number"
                      min={1}
                      value={form.installmentIntervalMonths ?? ""}
                      placeholder="Örn. 1"
                      onChange={(event) => setForm((current) => ({ ...current, installmentIntervalMonths: event.target.value ? Number(event.target.value) : null }))}
                    />
                  </label>
                  <label>
                    <span className="field-label required">Taksit sayısı</span>
                    <input
                      type="number"
                      min={1}
                      value={form.installmentCount ?? ""}
                      placeholder="Örn. 4"
                      onChange={(event) => setForm((current) => ({ ...current, installmentCount: event.target.value ? Number(event.target.value) : null }))}
                    />
                  </label>
                </>
              ) : null}
            </>
          ) : null}

          <label>
            <span className="field-label required">Motor no</span>
            <input value={form.engineNumber} onChange={(event) => setForm((current) => ({ ...current, engineNumber: event.target.value }))} required />
          </label>
          <label>
            <span className="field-label required">Şasi no</span>
            <input value={form.chassisNumber} onChange={(event) => setForm((current) => ({ ...current, chassisNumber: event.target.value }))} required />
          </label>
        </div>

        <label>
          <span>Açıklama</span>
          <textarea value={form.description} onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))} rows={3} />
        </label>

        {error ? <div className="alert error">{error}</div> : null}

        <div className="inline-actions">
          <button type="submit" className="primary-button" disabled={saving}>
            {saving ? "Kaydediliyor..." : editingVehicleId ? "Değişiklikleri kaydet" : "Satınalmayı kaydet"}
          </button>
          <button type="button" className="ghost-button dark" onClick={onClose}>
            İptal
          </button>
        </div>
      </form>
    </div>
  );
}
