import { useEffect, useState } from "react";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { PageHeader } from "../components/PageHeader";
import { SearchableSelect } from "../components/SearchableSelect";
import { api } from "../lib/api";
import { formatCurrency, formatDate, getPaymentMethodLabel, toDateTimeLocalValue, toIsoFromLocalValue } from "../lib/format";
import { useAuth } from "../state/AuthContext";
import type { ExpenseCategory, PaymentMethod, Transaction, TransactionType } from "../types";

interface GeneralExpenseForm {
  description: string;
  amount: number;
  transactionDate: string;
  categoryId: string | null;
  paymentMethod: PaymentMethod;
}

const createGeneralExpenseForm = (): GeneralExpenseForm => ({
  description: "",
  amount: 0,
  transactionDate: new Date().toISOString(),
  categoryId: null,
  paymentMethod: 1
});

export function GeneralExpensesPage() {
  const { session } = useAuth();
  const [expenses, setExpenses] = useState<Transaction[]>([]);
  const [categories, setCategories] = useState<ExpenseCategory[]>([]);
  const [form, setForm] = useState<GeneralExpenseForm>(createGeneralExpenseForm);
  const [amountInput, setAmountInput] = useState("");
  const [editingTransactionId, setEditingTransactionId] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [deletingTransactionId, setDeletingTransactionId] = useState<string | null>(null);
  const [pendingDeleteExpense, setPendingDeleteExpense] = useState<Transaction | null>(null);
  const [error, setError] = useState("");
  const [expandedIds, setExpandedIds] = useState<string[]>([]);

  async function loadData() {
    if (!session) {
      return;
    }

    setLoading(true);
    try {
      const [transactions, categoryData] = await Promise.all([
        api.getTransactions(session.token),
        api.getExpenseCategories(session.token, 2)
      ]);

      setExpenses(
        transactions
          .filter((transaction) => transaction.type === 2 && transaction.relatedEntityType === "GeneralExpense")
          .sort((left, right) => new Date(right.transactionDate).getTime() - new Date(left.transactionDate).getTime())
      );
      setCategories(categoryData);
      setError("");
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Diger giderler yuklenemedi.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadData();
  }, [session]);

  function openCreateModal() {
    setForm(createGeneralExpenseForm());
    setAmountInput("");
    setEditingTransactionId(null);
    setError("");
    setIsModalOpen(true);
  }

  function closeModal() {
    setIsModalOpen(false);
    setForm(createGeneralExpenseForm());
    setAmountInput("");
    setEditingTransactionId(null);
    setError("");
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
    if (!session) {
      return;
    }

    const numericAmount = Number.parseInt(amountInput.replace(/\D/g, ""), 10);
    if (!numericAmount) {
      setError("Masraf tutari girmeniz gerekiyor.");
      return;
    }

    if (!form.categoryId) {
      setError("Masraf turu secmeniz gerekiyor.");
      return;
    }

    setSaving(true);
    setError("");

    try {
      const payload = {
        type: 2 as TransactionType,
        amount: numericAmount,
        transactionDate: form.transactionDate,
        description: form.description.trim(),
        paymentMethod: form.paymentMethod,
        categoryId: form.categoryId,
        relatedEntityId: null,
        relatedEntityType: "GeneralExpense"
      };

      const savedExpense = editingTransactionId
        ? await api.updateTransaction(session.token, editingTransactionId, payload)
        : await api.createTransaction(session.token, payload);

      if (editingTransactionId) {
        setExpenses((current) =>
          current
            .map((expense) => (expense.id === savedExpense.id ? savedExpense : expense))
            .sort((left, right) => new Date(right.transactionDate).getTime() - new Date(left.transactionDate).getTime())
        );
      } else {
        setExpenses((current) =>
          [savedExpense, ...current].sort(
            (left, right) => new Date(right.transactionDate).getTime() - new Date(left.transactionDate).getTime()
          )
        );
      }

      closeModal();
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : "Genel gider kaydedilemedi.");
    } finally {
      setSaving(false);
    }
  }

  function handleEdit(expense: Transaction) {
    setEditingTransactionId(expense.id);
    setForm({
      description: expense.description,
      amount: expense.amount,
      transactionDate: expense.transactionDate,
      categoryId: expense.categoryId,
      paymentMethod: expense.paymentMethod
    });
    setAmountInput(formatCurrency(expense.amount));
    setError("");
    setIsModalOpen(true);
  }

  function requestDeleteExpense(expense: Transaction) {
    setPendingDeleteExpense(expense);
    setError("");
  }

  async function handleDeleteExpense() {
    if (!session || !pendingDeleteExpense) {
      return;
    }

    try {
      setDeletingTransactionId(pendingDeleteExpense.id);
      setError("");
      await api.deleteTransaction(session.token, pendingDeleteExpense.id);
      setExpenses((current) => current.filter((expense) => expense.id !== pendingDeleteExpense.id));
      setPendingDeleteExpense(null);
    } catch (deleteError) {
      setError(deleteError instanceof Error ? deleteError.message : "Genel gider silinemedi.");
    } finally {
      setDeletingTransactionId(null);
    }
  }

  return (
    <section className="page-stack">
      <PageHeader
        eyebrow="Diger Giderler"
        title="Genel operasyon giderlerini yonetin"
        description="Kira, aidat, guvenlik ve benzeri araca bagli olmayan giderleri ayri takip edin."
        actions={
          <button type="button" className="primary-button" onClick={openCreateModal}>
            Diger masraf ekle
          </button>
        }
      />

      {error && !isModalOpen ? <div className="alert error">{error}</div> : null}

      <article className="panel">
        <div className="records-sticky-stack">
          <div className="panel-heading records-heading">
            <h2>Diger gider gecmisi</h2>
            <p>{loading ? "Liste hazirlaniyor..." : `${expenses.length} genel gider kaydi var.`}</p>
          </div>

          <div className="records-table-head general-expense-table-head records-table-head-sticky">
            <span>Kategori</span>
            <span>Aciklama</span>
            <span>Tutar ve tarih</span>
            <span>Odeme</span>
            <span>Islem</span>
          </div>
        </div>

        <div className="records-table">
          <div className="records-table-body">
            {expenses.map((expense) => {
              const isExpanded = expandedIds.includes(expense.id);

              return (
                <article key={expense.id} className={`records-row general-expense-row record-card-shell ${isExpanded ? "expanded" : ""}`}>
                  <button
                    type="button"
                    className={`record-mobile-summary ${isExpanded ? "expanded" : ""}`}
                    onClick={() => toggleExpanded(expense.id)}
                    aria-expanded={isExpanded}
                  >
                    <div className="record-mobile-summary-main">
                      <span className="status-badge status-warning">{expense.categoryName ?? "Genel gider"}</span>
                      <strong>{expense.description}</strong>
                      <span>
                        {formatDate(expense.transactionDate)} · {formatCurrency(expense.amount)}
                      </span>
                    </div>
                    <span className="record-mobile-summary-icon" aria-hidden="true">
                      {isExpanded ? "−" : "+"}
                    </span>
                  </button>

                  <div className="records-card-body">
                    <div className="records-cell records-main">
                      <strong>{expense.categoryName ?? "Kategorisiz"}</strong>
                      <small>Genel operasyon gideri</small>
                    </div>

                    <div className="records-cell records-finance">
                      <div className="finance-stack">
                        <span>Aciklama</span>
                        <strong>{expense.description || "-"}</strong>
                      </div>
                    </div>

                    <div className="records-cell records-finance">
                      <div className="finance-stack">
                        <span>Tutar</span>
                        <strong className="finance-strong">{formatCurrency(expense.amount)}</strong>
                        <span>Tarih</span>
                        <strong>{formatDate(expense.transactionDate)}</strong>
                      </div>
                    </div>

                    <div className="records-cell records-finance">
                      <div className="finance-stack">
                        <span>Odeme metodu</span>
                        <strong>{getPaymentMethodLabel(expense.paymentMethod)}</strong>
                      </div>
                    </div>

                    <div className="records-cell records-actions">
                      <button type="button" className="ghost-button dark" onClick={() => handleEdit(expense)}>
                        Duzenle
                      </button>
                      <button type="button" className="ghost-button danger" onClick={() => requestDeleteExpense(expense)}>
                        Sil
                      </button>
                    </div>
                  </div>
                </article>
              );
            })}

            {!loading && expenses.length === 0 ? (
              <div className="empty-panel">Henuz genel gider girilmemis.</div>
            ) : null}
          </div>
        </div>
      </article>

      {isModalOpen ? (
        <div className="modal-backdrop">
          <form className="panel modal-panel data-form" onSubmit={handleSubmit}>
            <div className="modal-header">
              <div className="panel-heading">
                <h2>Diger masraf ekle</h2>
              </div>
              <button type="button" className="modal-close" onClick={closeModal} aria-label="Kapat">
                X
              </button>
            </div>

            <div className="form-grid">
              <SearchableSelect
                label="Masraf turu"
                value={form.categoryId ?? ""}
                options={categories.map((category) => ({ id: category.id, name: category.name }))}
                placeholder="Genel gider turu secin"
                onChange={(categoryId) => setForm((current) => ({ ...current, categoryId }))}
              />
              <label>
                <span>Tutar</span>
                <input
                  inputMode="numeric"
                  value={amountInput}
                  placeholder="Orn. 45.000 TL"
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
                      paymentMethod: Number(event.target.value) as PaymentMethod
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
                value={toDateTimeLocalValue(form.transactionDate)}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    transactionDate: toIsoFromLocalValue(event.target.value)
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
              <button type="submit" className="primary-button" disabled={saving}>
                {saving ? "Kaydediliyor..." : editingTransactionId ? "Degisiklikleri kaydet" : "Gideri kaydet"}
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
          title="Genel gider kaydini sil"
          description={`${pendingDeleteExpense.categoryName ?? "Bu gider"} kaydi silinecek.`}
          error={error}
          confirmLabel="Gideri sil"
          busy={deletingTransactionId === pendingDeleteExpense.id}
          onCancel={() => {
            if (!deletingTransactionId) {
              setError("");
              setPendingDeleteExpense(null);
            }
          }}
          onConfirm={() => void handleDeleteExpense()}
        />
      ) : null}
    </section>
  );
}
