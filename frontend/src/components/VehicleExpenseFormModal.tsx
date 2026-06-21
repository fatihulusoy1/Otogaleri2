import { useState } from "react";
import { SearchableSelect } from "./SearchableSelect";
import { api } from "../lib/api";
import { formatCurrency, toDateTimeLocalValue, toIsoFromLocalValue } from "../lib/format";
import type { CreateVehicleExpenseRequest, ExpenseCategory, Vehicle } from "../types";

const createInitialForm = (): CreateVehicleExpenseRequest => ({
  description: "",
  amount: 0,
  expenseDate: new Date().toISOString(),
  categoryId: null,
  paymentMethod: 1
});

function getVehicleDisplayName(vehicle: Vehicle) {
  return [vehicle.plate, vehicle.segment, vehicle.brand, vehicle.model].filter(Boolean).join(" / ");
}

interface VehicleExpenseFormModalProps {
  token: string;
  vehicles: Vehicle[];
  categories: ExpenseCategory[];
  initialVehicleId: string;
  onClose: () => void;
  onSaved: () => void;
}

export function VehicleExpenseFormModal({
  token,
  vehicles,
  categories,
  initialVehicleId,
  onClose,
  onSaved
}: VehicleExpenseFormModalProps) {
  const [selectedVehicleId, setSelectedVehicleId] = useState(initialVehicleId);
  const [form, setForm] = useState<CreateVehicleExpenseRequest>(createInitialForm);
  const [amountInput, setAmountInput] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const vehicleOptions = vehicles.map((vehicle) => ({ id: vehicle.id, name: getVehicleDisplayName(vehicle) }));

  function handleCurrencyInput(value: string) {
    const digits = value.replace(/\D/g, "");
    setAmountInput(digits ? formatCurrency(Number(digits)) : "");
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selectedVehicleId) {
      return;
    }

    const numericAmount = Number.parseInt(amountInput.replace(/\D/g, ""), 10);
    if (!numericAmount) {
      setError("Masraf tutari girmeniz gerekiyor.");
      return;
    }

    if (!form.categoryId) {
      setError("Masraf kategorisi secmeniz gerekiyor.");
      return;
    }

    setSaving(true);
    setError("");

    try {
      await api.createExpense(token, selectedVehicleId, { ...form, amount: numericAmount });
      onSaved();
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : "Arac masrafi kaydedilemedi.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="modal-backdrop">
      <form className="panel modal-panel data-form" onSubmit={handleSubmit}>
        <div className="modal-header">
          <div className="panel-heading">
            <h2>Arac masrafi ekle</h2>
          </div>
          <button type="button" className="modal-close" onClick={onClose} aria-label="Kapat">
            X
          </button>
        </div>

        <SearchableSelect
          label="Arac"
          value={selectedVehicleId}
          options={vehicleOptions}
          placeholder="Plaka ile arac secin"
          onChange={setSelectedVehicleId}
        />

        <div className="form-grid">
          <SearchableSelect
            label="Masraf kategorisi"
            value={form.categoryId ?? ""}
            options={categories.map((category) => ({ id: category.id, name: category.name }))}
            placeholder="Kategori secin"
            onChange={(categoryId) => setForm((current) => ({ ...current, categoryId }))}
          />
          <label>
            <span>Tutar</span>
            <input
              inputMode="numeric"
              value={amountInput}
              placeholder="Orn. 25.000 TL"
              onChange={(event) => handleCurrencyInput(event.target.value)}
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
                  paymentMethod: Number(event.target.value) as 1 | 2 | 3
                }))
              }
            >
              <option value={1}>Nakit</option>
              <option value={2}>Havale</option>
              <option value={3}>Kredi Karti</option>
            </select>
          </label>
        </div>

        <label>
          <span>Masraf tarihi</span>
          <input
            type="datetime-local"
            value={toDateTimeLocalValue(form.expenseDate)}
            onChange={(event) => setForm((current) => ({ ...current, expenseDate: toIsoFromLocalValue(event.target.value) }))}
            required
          />
        </label>

        <label>
          <span>Aciklama</span>
          <textarea
            rows={3}
            value={form.description}
            onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
            required
          />
        </label>

        {error ? <div className="alert error">{error}</div> : null}

        <div className="inline-actions">
          <button type="submit" className="primary-button" disabled={saving || !selectedVehicleId}>
            {saving ? "Kaydediliyor..." : "Masrafi kaydet"}
          </button>
          <button type="button" className="ghost-button dark" onClick={onClose}>
            Iptal
          </button>
        </div>
      </form>
    </div>
  );
}
