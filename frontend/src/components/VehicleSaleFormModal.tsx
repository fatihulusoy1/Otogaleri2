import { useState } from "react";
import { api } from "../lib/api";
import { formatCurrency, toDateTimeLocalValue, toIsoFromLocalValue } from "../lib/format";
import type { CompleteVehicleSaleRequest, Vehicle } from "../types";

const createInitialForm = (): CompleteVehicleSaleRequest => ({
  salePrice: 0,
  saleDate: new Date().toISOString(),
  paymentMethod: 1,
  notaryRegistryNumber: null,
  counterpartyName: null,
  dueDate: null,
  documentNumber: null,
  installmentCount: null,
  installmentIntervalMonths: null,
  tradePlate: null,
  tradeAmount: null,
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

interface VehicleSaleFormModalProps {
  token: string;
  vehicle: Vehicle;
  onClose: () => void;
  onSaved: () => void;
}

export function VehicleSaleFormModal({ token, vehicle, onClose, onSaved }: VehicleSaleFormModalProps) {
  const [form, setForm] = useState<CompleteVehicleSaleRequest>(createInitialForm);
  const [salePriceInput, setSalePriceInput] = useState("");
  const [tradeAmountInput, setTradeAmountInput] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const vehicleName = [vehicle.segment, vehicle.brand, vehicle.model].filter(Boolean).join(" / ");

  function handleCurrencyInput(value: string, setter: (value: string) => void) {
    const digits = value.replace(/\D/g, "");
    setter(digits ? formatCurrency(Number(digits)) : "");
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    setSaving(true);
    setError("");

    try {
      const parsedSalePrice = Number.parseInt(salePriceInput.replace(/\D/g, ""), 10);
      const parsedTradeAmount = Number.parseInt(tradeAmountInput.replace(/\D/g, ""), 10);
      const requiresFinanceInfo = form.paymentMethod === 5 || form.paymentMethod === 6 || form.paymentMethod === 7;

      if (!parsedSalePrice) {
        setError("Satis fiyati girmeniz gerekiyor.");
        setSaving(false);
        return;
      }

      const hasTradeInfo = form.paymentMethod === 4;
      if (hasTradeInfo && (!form.tradePlate?.trim() || !parsedTradeAmount)) {
        setError("Takas bilgisi giriyorsaniz plaka ve takas bedelini birlikte doldurun.");
        setSaving(false);
        return;
      }

      if (requiresFinanceInfo && (!form.counterpartyName?.trim() || !form.dueDate)) {
        setError("Cek, senet veya vadeli satista taraf ve vade tarihi zorunludur.");
        setSaving(false);
        return;
      }

      if (form.paymentMethod === 6 && (!form.installmentCount || !form.installmentIntervalMonths)) {
        setError("Senet satisinda taksit sayisi ve taksit araligi zorunludur.");
        setSaving(false);
        return;
      }

      const payload = {
        ...form,
        salePrice: parsedSalePrice,
        notaryRegistryNumber: form.notaryRegistryNumber?.trim() || null,
        counterpartyName: requiresFinanceInfo ? form.counterpartyName?.trim() ?? null : null,
        dueDate: requiresFinanceInfo ? form.dueDate : null,
        documentNumber: requiresFinanceInfo ? form.documentNumber?.trim() ?? null : null,
        installmentCount: form.paymentMethod === 6 ? form.installmentCount : null,
        installmentIntervalMonths: form.paymentMethod === 6 ? form.installmentIntervalMonths : null,
        tradePlate: hasTradeInfo ? form.tradePlate?.trim().toUpperCase() ?? null : null,
        tradeAmount: hasTradeInfo ? parsedTradeAmount || null : null
      };

      await api.completeSale(token, vehicle.id, payload);
      onSaved();
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : "Satis kaydi tamamlanamadi.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="modal-backdrop">
      <form className="panel modal-panel data-form" onSubmit={handleSubmit}>
        <div className="modal-header">
          <div className="panel-heading">
            <h2>Yeni satis islemi</h2>
          </div>
          <button type="button" className="modal-close" onClick={onClose} aria-label="Kapat">
            X
          </button>
        </div>

        <div className="panel-heading">
          <h2>{vehicle.plate}</h2>
          <p>{vehicleName}</p>
        </div>

        <div className="form-grid">
          <label>
            <span>Satis fiyati</span>
            <input
              inputMode="numeric"
              value={salePriceInput}
              placeholder="Orn. 1.250.000 TL"
              onChange={(event) => handleCurrencyInput(event.target.value, setSalePriceInput)}
              required
            />
          </label>
          <label>
            <span>Odeme yontemi</span>
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
              <option value={3}>Kredi Karti</option>
              <option value={4}>Takas</option>
              <option value={5}>Cek</option>
              <option value={6}>Senet</option>
              <option value={7}>Vadeli</option>
            </select>
          </label>
          <label>
            <span>Noter yevmiye no</span>
            <input
              value={form.notaryRegistryNumber ?? ""}
              placeholder="Orn. 2026/1458"
              onChange={(event) => setForm((current) => ({ ...current, notaryRegistryNumber: event.target.value || null }))}
            />
          </label>
          {form.paymentMethod === 4 ? (
            <>
              <label>
                <span>Takas aracinin plakasi</span>
                <input
                  value={form.tradePlate ?? ""}
                  placeholder="Orn. 34XYZ657"
                  onChange={(event) => setForm((current) => ({ ...current, tradePlate: event.target.value || null }))}
                />
              </label>
              <label>
                <span>Takas bedeli</span>
                <input
                  inputMode="numeric"
                  value={tradeAmountInput}
                  placeholder="Orn. 50.000 TL"
                  onChange={(event) => handleCurrencyInput(event.target.value, setTradeAmountInput)}
                />
              </label>
            </>
          ) : null}
          {form.paymentMethod === 5 || form.paymentMethod === 6 || form.paymentMethod === 7 ? (
            <>
              <label>
                <span>Taraf bilgisi</span>
                <input
                  value={form.counterpartyName ?? ""}
                  placeholder="Orn. Ali Demir / XYZ Ticaret"
                  onChange={(event) => setForm((current) => ({ ...current, counterpartyName: event.target.value || null }))}
                />
              </label>
              <label>
                <span>{form.paymentMethod === 6 ? "Ilk taksit tarihi" : "Vade tarihi"}</span>
                <input
                  type="date"
                  value={toDateOnlyInputValue(form.dueDate)}
                  onChange={(event) => setForm((current) => ({ ...current, dueDate: toIsoDateValue(event.target.value) }))}
                />
              </label>
              <label>
                <span>Belge numarasi</span>
                <input
                  value={form.documentNumber ?? ""}
                  placeholder="Orn. SNT-2026-014"
                  onChange={(event) => setForm((current) => ({ ...current, documentNumber: event.target.value || null }))}
                />
              </label>
              {form.paymentMethod === 6 ? (
                <>
                  <label>
                    <span>Taksit araligi (ay)</span>
                    <input
                      type="number"
                      min={1}
                      value={form.installmentIntervalMonths ?? ""}
                      placeholder="Orn. 1"
                      onChange={(event) =>
                        setForm((current) => ({
                          ...current,
                          installmentIntervalMonths: event.target.value ? Number(event.target.value) : null
                        }))
                      }
                    />
                  </label>
                  <label>
                    <span>Taksit sayisi</span>
                    <input
                      type="number"
                      min={1}
                      value={form.installmentCount ?? ""}
                      placeholder="Orn. 4"
                      onChange={(event) =>
                        setForm((current) => ({
                          ...current,
                          installmentCount: event.target.value ? Number(event.target.value) : null
                        }))
                      }
                    />
                  </label>
                </>
              ) : null}
            </>
          ) : null}
          <label>
            <span>Satis tarihi</span>
            <input
              type="datetime-local"
              value={toDateTimeLocalValue(form.saleDate)}
              onChange={(event) => setForm((current) => ({ ...current, saleDate: toIsoFromLocalValue(event.target.value) }))}
              required
            />
          </label>
        </div>

        <label>
          <span>Aciklama</span>
          <textarea
            rows={3}
            value={form.description}
            onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
          />
        </label>

        {error ? <div className="alert error">{error}</div> : null}

        <div className="inline-actions">
          <button type="submit" className="primary-button" disabled={saving}>
            {saving ? "Isleniyor..." : "Satisi tamamla"}
          </button>
          <button type="button" className="ghost-button dark" onClick={onClose}>
            Iptal
          </button>
        </div>
      </form>
    </div>
  );
}
