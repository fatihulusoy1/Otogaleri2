import { useEffect, useState } from "react";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { PageHeader } from "../components/PageHeader";
import { api } from "../lib/api";
import { formatCurrency } from "../lib/format";
import { useAuth } from "../state/AuthContext";
import type { AdminSubscriptionPlan } from "../types";

interface PlanFormState {
  name: string;
  description: string;
  monthlyPrice: string;
  yearlyPrice: string;
  maxUsers: string;
  maxVehicles: string;
  isActive: boolean;
}

const emptyForm: PlanFormState = {
  name: "",
  description: "",
  monthlyPrice: "0",
  yearlyPrice: "0",
  maxUsers: "1",
  maxVehicles: "1",
  isActive: true
};

export function AdminPlansPage() {
  const { session } = useAuth();
  const [plans, setPlans] = useState<AdminSubscriptionPlan[]>([]);
  const [error, setError] = useState("");
  const [modal, setModal] = useState<{ mode: "create" | "edit"; plan?: AdminSubscriptionPlan } | null>(null);
  const [form, setForm] = useState<PlanFormState | null>(null);
  const [confirmTarget, setConfirmTarget] = useState<AdminSubscriptionPlan | null>(null);
  const [modalError, setModalError] = useState("");
  const [busy, setBusy] = useState(false);

  async function load() {
    if (!session) {
      return;
    }
    try {
      setError("");
      setPlans(await api.adminGetSubscriptionPlans(session.token));
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Paketler yüklenemedi.");
    }
  }

  useEffect(() => {
    void load();
  }, [session?.token]);

  function openCreate() {
    setModalError("");
    setModal({ mode: "create" });
    setForm({ ...emptyForm });
  }

  function openEdit(plan: AdminSubscriptionPlan) {
    setModalError("");
    setModal({ mode: "edit", plan });
    setForm({
      name: plan.name,
      description: plan.description,
      monthlyPrice: String(plan.monthlyPrice),
      yearlyPrice: String(plan.yearlyPrice),
      maxUsers: String(plan.maxUsers),
      maxVehicles: String(plan.maxVehicles),
      isActive: plan.isActive
    });
  }

  function closeModals() {
    setModal(null);
    setForm(null);
    setConfirmTarget(null);
    setModalError("");
  }

  async function submit() {
    if (!session || !form || !modal) {
      return;
    }
    setBusy(true);
    setModalError("");
    const payload = {
      name: form.name,
      description: form.description,
      monthlyPrice: Number(form.monthlyPrice) || 0,
      yearlyPrice: Number(form.yearlyPrice) || 0,
      maxUsers: Number(form.maxUsers) || 0,
      maxVehicles: Number(form.maxVehicles) || 0,
      isActive: form.isActive
    };
    try {
      if (modal.mode === "create") {
        await api.adminCreatePlan(session.token, payload);
      } else if (modal.plan) {
        await api.adminUpdatePlan(session.token, modal.plan.id, payload);
      }
      closeModals();
      await load();
    } catch (requestError) {
      setModalError(requestError instanceof Error ? requestError.message : "Paket kaydedilemedi.");
    } finally {
      setBusy(false);
    }
  }

  async function confirmDelete() {
    if (!session || !confirmTarget) {
      return;
    }
    setBusy(true);
    setModalError("");
    try {
      await api.adminDeletePlan(session.token, confirmTarget.id);
      closeModals();
      await load();
    } catch (requestError) {
      setModalError(requestError instanceof Error ? requestError.message : "Paket silinemedi.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="page-stack">
      <PageHeader
        eyebrow="Super Admin"
        title="Paketler"
        description="Abonelik paketlerini buradan tanımlayın. Paket içeriği koda gömülü değildir; buradaki değerlere göre çalışır."
      />

      {error ? <div className="alert error">{error}</div> : null}

      <article className="panel">
        <div className="panel-heading admin-panel-heading">
          <div>
            <h2>Paketler ({plans.length})</h2>
            <p>Bir paketi değiştirmek, mevcut abonelerin devam eden dönemini etkilemez; yeni değerler bir sonraki yenilemede geçerli olur.</p>
          </div>
          <button type="button" className="primary-button" onClick={openCreate}>
            + Paket ekle
          </button>
        </div>

        <div className="subscription-plans">
          {plans.map((plan) => (
            <article key={plan.id} className="subscription-plan-card">
              <div className="admin-tenant-meta">
                <strong style={{ fontSize: "1.1rem" }}>{plan.name}</strong>
                <span className={`status-badge ${plan.isActive ? "status-success" : "status-danger"}`}>
                  {plan.isActive ? "Aktif" : "Pasif"}
                </span>
              </div>
              <span className="subscription-plan-desc">{plan.description || "—"}</span>
              <span className="subscription-price">
                {formatCurrency(plan.monthlyPrice)}
                <small>/ay</small>
              </span>
              <span className="subscription-plan-limits">{formatCurrency(plan.yearlyPrice)} / yıl</span>
              <span className="subscription-plan-limits">
                {plan.maxUsers} kullanıcı · {plan.maxVehicles} araç
              </span>
              <div className="inline-actions">
                <button type="button" className="ghost-button dark" onClick={() => openEdit(plan)}>
                  Düzenle
                </button>
                <button type="button" className="ghost-button danger" onClick={() => { setModalError(""); setConfirmTarget(plan); }}>
                  Sil
                </button>
              </div>
            </article>
          ))}
          {plans.length === 0 ? <div className="empty-panel">Paket bulunamadı.</div> : null}
        </div>
      </article>

      {modal && form ? (
        <div className="modal-backdrop">
          <div className="panel modal-panel">
            <div className="modal-header">
              <div className="panel-heading">
                <h2>{modal.mode === "create" ? "Yeni paket" : "Paket düzenle"}</h2>
              </div>
              <button type="button" className="modal-close" onClick={closeModals} aria-label="Kapat">
                X
              </button>
            </div>
            <div className="data-form">
              <label>
                <span className="field-label required">Paket adı</span>
                <input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} placeholder="Örn: Pro" />
              </label>
              <label>
                <span className="field-label">Açıklama</span>
                <input value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} />
              </label>
              <div className="form-grid two-columns">
                <label>
                  <span className="field-label">Aylık ücret (₺)</span>
                  <input type="number" min="0" value={form.monthlyPrice} onChange={(event) => setForm({ ...form, monthlyPrice: event.target.value })} />
                </label>
                <label>
                  <span className="field-label">Yıllık ücret (₺)</span>
                  <input type="number" min="0" value={form.yearlyPrice} onChange={(event) => setForm({ ...form, yearlyPrice: event.target.value })} />
                </label>
              </div>
              <div className="form-grid two-columns">
                <label>
                  <span className="field-label required">Maks. kullanıcı</span>
                  <input type="number" min="0" value={form.maxUsers} onChange={(event) => setForm({ ...form, maxUsers: event.target.value })} />
                </label>
                <label>
                  <span className="field-label required">Maks. araç</span>
                  <input type="number" min="0" value={form.maxVehicles} onChange={(event) => setForm({ ...form, maxVehicles: event.target.value })} />
                </label>
              </div>
              <label className="admin-checkbox">
                <input type="checkbox" checked={form.isActive} onChange={(event) => setForm({ ...form, isActive: event.target.checked })} />
                <span>Paket aktif (yeni kayıt/yenilemede seçilebilir)</span>
              </label>
              {modalError ? <div className="alert error">{modalError}</div> : null}
              <div className="inline-actions">
                <button type="button" className="ghost-button dark" onClick={closeModals} disabled={busy}>
                  Vazgeç
                </button>
                <button type="button" className="primary-button" onClick={() => void submit()} disabled={busy}>
                  {busy ? "Kaydediliyor..." : "Kaydet"}
                </button>
              </div>
            </div>
          </div>
        </div>
      ) : null}

      {confirmTarget ? (
        <ConfirmDialog
          title="Paket silinsin mi?"
          description={`"${confirmTarget.name}" paketi silinecek. Kullanımdaysa silinemez (pasif yapabilirsiniz).`}
          error={modalError}
          busy={busy}
          onCancel={closeModals}
          onConfirm={() => void confirmDelete()}
        />
      ) : null}
    </section>
  );
}
