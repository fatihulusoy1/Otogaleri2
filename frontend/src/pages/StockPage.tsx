import { useEffect, useMemo, useState } from "react";
import { PageHeader } from "../components/PageHeader";
import { SearchableSelect } from "../components/SearchableSelect";
import { StatusBadge } from "../components/StatusBadge";
import { api } from "../lib/api";
import { formatCurrency, formatDate, getPaymentMethodLabel, toDateTimeLocalValue, toIsoFromLocalValue } from "../lib/format";
import { useAuth } from "../state/AuthContext";
import type { CreateVehicleExpenseRequest, ExpenseCategory, Vehicle, VehicleExpense } from "../types";

const createInitialExpenseForm = (): CreateVehicleExpenseRequest => ({
  description: "",
  amount: 0,
  expenseDate: new Date().toISOString(),
  categoryId: null,
  paymentMethod: 1
});

export function StockPage() {
  const { session } = useAuth();
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [expenses, setExpenses] = useState<VehicleExpense[]>([]);
  const [selectedVehicleId, setSelectedVehicleId] = useState("");
  const [categories, setCategories] = useState<ExpenseCategory[]>([]);
  const [expenseForm, setExpenseForm] = useState<CreateVehicleExpenseRequest>(createInitialExpenseForm);
  const [editingExpenseId, setEditingExpenseId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const stockVehicles = useMemo(() => vehicles.filter((vehicle) => vehicle.status === 1), [vehicles]);

  async function loadStockData() {
    if (!session) {
      return;
    }

    setLoading(true);
    try {
      const [vehicleData, expenseData, categoryData] = await Promise.all([
        api.getVehicles(session.token),
        api.getExpenses(session.token),
        api.getExpenseCategories(session.token)
      ]);
      setVehicles(vehicleData);
      setExpenses(expenseData);
      setCategories(categoryData);
      setSelectedVehicleId((current) => current || vehicleData.find((vehicle) => vehicle.status === 1)?.id || "");
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Stok ekrani yuklenemedi.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadStockData();
  }, [session]);

  function resetExpenseForm() {
    setExpenseForm(createInitialExpenseForm());
    setEditingExpenseId(null);
  }

  async function handleExpenseSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!session || !selectedVehicleId) {
      return;
    }

    setSaving(true);
    setError("");

    try {
      if (editingExpenseId) {
        await api.updateExpense(session.token, editingExpenseId, expenseForm);
      } else {
        await api.createExpense(session.token, selectedVehicleId, expenseForm);
      }
      resetExpenseForm();
      await loadStockData();
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : "Masraf kaydi eklenemedi.");
    } finally {
      setSaving(false);
    }
  }

  function handleEditExpense(expense: VehicleExpense) {
    setEditingExpenseId(expense.id);
    setSelectedVehicleId(expense.vehicleId);
    setExpenseForm({
      description: expense.description,
      amount: expense.amount,
      expenseDate: expense.expenseDate,
      categoryId: expense.categoryId,
      paymentMethod: expense.paymentMethod
    });
    setError("");
  }

  const visibleExpenses = expenses
    .filter((expense) => !selectedVehicleId || expense.vehicleId === selectedVehicleId)
    .slice(0, 6);

  return (
    <section className="page-stack">
      <PageHeader
        eyebrow="Stok"
        title="Stoktaki araclari ve masraflari izleyin"
        description="Arac bazinda toplam maliyetleri gorun, ek masraf girislerini hizlica isleyin."
      />

      <div className="content-grid sidebar-layout">
        <form className="panel data-form" onSubmit={handleExpenseSubmit}>
          <div className="panel-heading">
            <h2>{editingExpenseId ? "Masraf duzenle" : "Masraf ekle"}</h2>
            <p>Ekspertiz, bakim veya diger maliyetleri stok kartina baglayin.</p>
          </div>

          <label>
            <span>Arac</span>
            <select value={selectedVehicleId} onChange={(event) => setSelectedVehicleId(event.target.value)} required>
              <option value="">Arac secin</option>
              {stockVehicles.map((vehicle) => (
                <option key={vehicle.id} value={vehicle.id}>
                  {vehicle.plate} - {vehicle.brand} {vehicle.model}
                </option>
              ))}
            </select>
          </label>

          <div className="form-grid">
            <SearchableSelect
              label="Masraf kategorisi"
              value={expenseForm.categoryId ?? ""}
              options={categories}
              placeholder="Kategori secin"
              onChange={(categoryId) => setExpenseForm((current) => ({ ...current, categoryId }))}
            />
            <label>
              <span>Tutar</span>
              <input
                type="number"
                value={expenseForm.amount}
                onChange={(event) => setExpenseForm((current) => ({ ...current, amount: Number(event.target.value) }))}
                required
              />
            </label>
            <label>
              <span>Odeme yontemi</span>
              <select
                value={expenseForm.paymentMethod}
                onChange={(event) =>
                  setExpenseForm((current) => ({
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
            <span>Tarih</span>
            <input
              type="datetime-local"
              value={toDateTimeLocalValue(expenseForm.expenseDate)}
              onChange={(event) =>
                setExpenseForm((current) => ({
                  ...current,
                  expenseDate: toIsoFromLocalValue(event.target.value)
                }))
              }
              required
            />
          </label>

          <label>
            <span>Aciklama</span>
            <textarea
              rows={3}
              value={expenseForm.description}
              onChange={(event) => setExpenseForm((current) => ({ ...current, description: event.target.value }))}
              required
            />
          </label>

          {error ? <div className="alert error">{error}</div> : null}

          <div className="inline-actions">
            <button type="submit" className="primary-button" disabled={saving || !selectedVehicleId}>
              {saving ? "Kaydediliyor..." : editingExpenseId ? "Degisiklikleri kaydet" : "Masrafi kaydet"}
            </button>
            {editingExpenseId ? (
              <button type="button" className="ghost-button dark" onClick={resetExpenseForm}>
                Iptal
              </button>
            ) : null}
          </div>

          <div className="mini-list">
            {visibleExpenses.map((expense) => (
              <article key={expense.id} className="mini-list-item">
                <div>
                  <strong>{expense.vehiclePlate}</strong>
                  <span>{expense.description}</span>
                </div>
                <div>
                  <strong>{formatCurrency(expense.amount)}</strong>
                  <small>
                    {expense.categoryName ?? "Kategorisiz"} / {getPaymentMethodLabel(expense.paymentMethod)}
                  </small>
                </div>
                <button type="button" className="ghost-button dark" onClick={() => handleEditExpense(expense)}>
                  Duzenle
                </button>
              </article>
            ))}
          </div>
        </form>

        <article className="panel">
          <div className="panel-heading">
            <h2>Aktif stok kartlari</h2>
            <p>{loading ? "Stok kartlari hazirlaniyor..." : `${stockVehicles.length} arac stokta.`}</p>
          </div>

          <div className="cards-grid">
            {stockVehicles.map((vehicle) => {
              const latestExpense = expenses.find((expense) => expense.vehicleId === vehicle.id);

              return (
                <article key={vehicle.id} className="vehicle-card">
                  <div className="vehicle-card-head">
                    <div>
                      <strong>{vehicle.plate}</strong>
                      <span>
                        {[vehicle.segment, vehicle.brand, vehicle.model].filter(Boolean).join(" / ")}
                      </span>
                    </div>
                    <StatusBadge status={vehicle.status} />
                  </div>

                  <div className="vehicle-metrics">
                    <div>
                      <span>Alis</span>
                      <strong>{formatCurrency(vehicle.purchasePrice)}</strong>
                    </div>
                    <div>
                      <span>Masraf</span>
                      <strong>{formatCurrency(vehicle.totalExpenseCost)}</strong>
                    </div>
                    <div>
                      <span>Toplam</span>
                      <strong>{formatCurrency(vehicle.totalCost)}</strong>
                    </div>
                    <div>
                      <span>Hedef satis</span>
                      <strong>{vehicle.targetSalePrice ? formatCurrency(vehicle.targetSalePrice) : "-"}</strong>
                    </div>
                  </div>

                  <small className="stock-note">
                    Son guncelleme: {latestExpense ? formatDate(latestExpense.expenseDate) : "Henuz masraf yok"}
                  </small>
                </article>
              );
            })}

            {!loading && stockVehicles.length === 0 ? (
              <div className="empty-panel">Stokta arac bulunmuyor.</div>
            ) : null}
          </div>
        </article>
      </div>
    </section>
  );
}
