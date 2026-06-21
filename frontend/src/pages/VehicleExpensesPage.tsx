import { useEffect, useMemo, useState } from "react";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { PageHeader } from "../components/PageHeader";
import { SearchableSelect } from "../components/SearchableSelect";
import { api } from "../lib/api";
import { formatCurrency, formatDate, getPaymentMethodLabel, toDateTimeLocalValue, toIsoFromLocalValue } from "../lib/format";
import { useAuth } from "../state/AuthContext";
import type { CreateVehicleExpenseRequest, ExpenseCategory, Vehicle, VehicleExpense } from "../types";

const createVehicleExpenseForm = (): CreateVehicleExpenseRequest => ({
  description: "",
  amount: 0,
  expenseDate: new Date().toISOString(),
  categoryId: null,
  paymentMethod: 1
});

function getVehicleDisplayName(vehicle: Vehicle) {
  return [vehicle.plate, vehicle.segment, vehicle.brand, vehicle.model].filter(Boolean).join(" / ");
}

function sortVehiclesForExpenseFlow(left: Vehicle, right: Vehicle) {
  const leftStock = left.status === 1;
  const rightStock = right.status === 1;

  if (leftStock !== rightStock) {
    return leftStock ? -1 : 1;
  }

  if (!leftStock && !rightStock) {
    const rightSale = right.saleDate ? new Date(right.saleDate).getTime() : 0;
    const leftSale = left.saleDate ? new Date(left.saleDate).getTime() : 0;
    return rightSale - leftSale;
  }

  return new Date(right.purchaseDate).getTime() - new Date(left.purchaseDate).getTime();
}

export function VehicleExpensesPage() {
  const { session } = useAuth();
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [vehicleExpenses, setVehicleExpenses] = useState<VehicleExpense[]>([]);
  const [categories, setCategories] = useState<ExpenseCategory[]>([]);
  const [selectedVehicleId, setSelectedVehicleId] = useState("");
  const [form, setForm] = useState<CreateVehicleExpenseRequest>(createVehicleExpenseForm);
  const [amountInput, setAmountInput] = useState("");
  const [editingExpenseId, setEditingExpenseId] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [deletingExpenseId, setDeletingExpenseId] = useState<string | null>(null);
  const [pendingDeleteExpense, setPendingDeleteExpense] = useState<VehicleExpense | null>(null);
  const [error, setError] = useState("");
  const [expandedIds, setExpandedIds] = useState<string[]>([]);

  const sortedVehicles = useMemo(() => [...vehicles].sort(sortVehiclesForExpenseFlow), [vehicles]);
  const vehicleOptions = useMemo(
    () => sortedVehicles.map((vehicle) => ({ id: vehicle.id, name: getVehicleDisplayName(vehicle) })),
    [sortedVehicles]
  );

  async function loadData() {
    if (!session) {
      return;
    }

    setLoading(true);
    try {
      const [vehicleData, expenseData, categoryData] = await Promise.all([
        api.getVehicles(session.token),
        api.getExpenses(session.token),
        api.getExpenseCategories(session.token, 1)
      ]);

      setVehicles(vehicleData);
      setVehicleExpenses(expenseData);
      setCategories(categoryData);
      setSelectedVehicleId((current) => current || [...vehicleData].sort(sortVehiclesForExpenseFlow)[0]?.id || "");
      setError("");
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Arac masraf verileri yuklenemedi.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadData();
  }, [session]);

  function resetState() {
    setSelectedVehicleId(sortedVehicles[0]?.id || "");
    setForm(createVehicleExpenseForm());
    setAmountInput("");
    setEditingExpenseId(null);
  }

  function openCreateModal() {
    resetState();
    setError("");
    setIsModalOpen(true);
  }

  function closeModal() {
    setIsModalOpen(false);
    setError("");
    resetState();
  }

  function handleCurrencyInput(value: string) {
    const digits = value.replace(/\D/g, "");
    setAmountInput(digits ? formatCurrency(Number(digits)) : "");
  }

  function toggleExpanded(id: string) {
    setExpandedIds((current) => (current.includes(id) ? current.filter((item) => item !== id) : [...current, id]));
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!session || !selectedVehicleId) {
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
      const payload = {
        ...form,
        amount: numericAmount
      };

      if (editingExpenseId) {
        await api.updateExpense(session.token, editingExpenseId, payload);
      } else {
        await api.createExpense(session.token, selectedVehicleId, payload);
      }

      closeModal();
      await loadData();
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : "Arac masrafi kaydedilemedi.");
    } finally {
      setSaving(false);
    }
  }

  function handleEdit(expense: VehicleExpense) {
    setEditingExpenseId(expense.id);
    setSelectedVehicleId(expense.vehicleId);
    setForm({
      description: expense.description,
      amount: expense.amount,
      expenseDate: expense.expenseDate,
      categoryId: expense.categoryId,
      paymentMethod: expense.paymentMethod
    });
    setAmountInput(formatCurrency(expense.amount));
    setError("");
    setIsModalOpen(true);
  }

  function requestDeleteExpense(expense: VehicleExpense) {
    setPendingDeleteExpense(expense);
    setError("");
  }

  async function handleDeleteExpense() {
    if (!session) {
      return;
    }

    if (!pendingDeleteExpense) {
      return;
    }

    try {
      setDeletingExpenseId(pendingDeleteExpense.id);
      setError("");
      await api.deleteExpense(session.token, pendingDeleteExpense.id);
      setPendingDeleteExpense(null);
      await loadData();
    } catch (deleteError) {
      setError(deleteError instanceof Error ? deleteError.message : "Masraf kaydi silinemedi.");
    } finally {
      setDeletingExpenseId(null);
    }
  }

  return (
    <section className="page-stack">
      <PageHeader
        eyebrow="Araç Masrafları"
        title="Araca bağlı masrafları yönetin"
        description="Ekspertiz, bakım ve benzeri araç giderlerini araca bağlayarak takip edin."
        actions={
          <button type="button" className="primary-button" onClick={openCreateModal}>
            Araç masrafı ekle
          </button>
        }
      />

      {error && !isModalOpen ? <div className="alert error">{error}</div> : null}

      <article className="panel">
        <div className="records-sticky-stack">
          <div className="panel-heading records-heading">
            <h2>Araç masraf geçmişi</h2>
            <p>{loading ? "Liste hazırlanıyor..." : `${vehicleExpenses.length} araç masrafı kaydı var.`}</p>
          </div>

          <div className="records-table-head expense-table-head records-table-head-sticky">
            <span>Araç</span>
            <span>Kategori</span>
            <span>Tutar ve tarih</span>
            <span>Ödeme</span>
            <span>İşlem</span>
          </div>
        </div>

        <div className="records-table">
          <div className="records-table-body">
            {vehicleExpenses.map((expense) => {
              const isExpanded = expandedIds.includes(expense.id);

              return (
                <article key={expense.id} className={`records-row expense-row record-card-shell ${isExpanded ? "expanded" : ""}`}>
                  <button
                    type="button"
                    className={`record-mobile-summary ${isExpanded ? "expanded" : ""}`}
                    onClick={() => toggleExpanded(expense.id)}
                    aria-expanded={isExpanded}
                  >
                    <div className="record-mobile-summary-main">
                      <span className="status-badge status-warning">{expense.categoryName ?? "Masraf"}</span>
                      <strong>{expense.vehiclePlate}</strong>
                      <span>{expense.description}</span>
                      <span>
                        {formatDate(expense.expenseDate)} · {formatCurrency(expense.amount)}
                      </span>
                    </div>
                    <span className="record-mobile-summary-icon" aria-hidden="true">
                      {isExpanded ? "−" : "+"}
                    </span>
                  </button>

                  <div className="records-card-body">
                    <div className="records-cell records-main">
                      <strong>{expense.vehiclePlate}</strong>
                      <span>{expense.description}</span>
                    </div>

                    <div className="records-cell records-main">
                      <strong>{expense.categoryName ?? "Kategorisiz"}</strong>
                      <small>Araca bağlı gider</small>
                    </div>

                    <div className="records-cell records-finance">
                      <div className="finance-stack">
                        <span>Tutar</span>
                        <strong className="finance-strong">{formatCurrency(expense.amount)}</strong>
                        <span>Tarih</span>
                        <strong>{formatDate(expense.expenseDate)}</strong>
                      </div>
                    </div>

                    <div className="records-cell records-main">
                      <strong>{getPaymentMethodLabel(expense.paymentMethod)}</strong>
                    </div>

                    <div className="records-cell records-actions">
                      <button type="button" className="ghost-button dark" onClick={() => handleEdit(expense)}>
                        Düzenle
                      </button>
                      <button type="button" className="ghost-button danger" onClick={() => requestDeleteExpense(expense)}>
                        Sil
                      </button>
                    </div>
                  </div>
                </article>
              );
            })}

            {!loading && vehicleExpenses.length === 0 ? (
              <div className="empty-panel">Henüz araç masrafı girilmemiş.</div>
            ) : null}
          </div>
        </div>
      </article>

      {isModalOpen ? (
        <div className="modal-backdrop">
          <form className="panel modal-panel data-form" onSubmit={handleSubmit}>
            <div className="modal-header">
              <div className="panel-heading">
                <h2>{editingExpenseId ? "Arac masrafi duzenle" : "Arac masrafi ekle"}</h2>
              </div>
              <button type="button" className="modal-close" onClick={closeModal} aria-label="Kapat">
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
                onChange={(event) =>
                  setForm((current) => ({
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
                value={form.description}
                onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
                required
              />
            </label>

            {error ? <div className="alert error">{error}</div> : null}

            <div className="inline-actions">
              <button type="submit" className="primary-button" disabled={saving || !selectedVehicleId}>
                {saving ? "Kaydediliyor..." : editingExpenseId ? "Degisiklikleri kaydet" : "Masrafi kaydet"}
              </button>
              <button type="button" className="ghost-button dark" onClick={closeModal}>
                Iptal
              </button>
            </div>
          </form>
        </div>
      ) : null}

      {pendingDeleteExpense ? (
        <ConfirmDialog
          title="Masraf kaydini sil"
          description={`${pendingDeleteExpense.vehiclePlate} aracina ait bu masraf kaydi silinecek.`}
          confirmLabel="Masrafi sil"
          busy={deletingExpenseId === pendingDeleteExpense.id}
          onCancel={() => {
            if (!deletingExpenseId) {
              setPendingDeleteExpense(null);
            }
          }}
          onConfirm={() => void handleDeleteExpense()}
        />
      ) : null}
    </section>
  );
}
