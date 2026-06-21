import { useEffect, useState } from "react";
import { PageHeader } from "../components/PageHeader";
import { SearchableSelect } from "../components/SearchableSelect";
import { api } from "../lib/api";
import { useAuth } from "../state/AuthContext";
import type { AdminSubscriptionPlan, AdminTenant, AdminUser, SaveSubscriptionPlanRequest } from "../types";

interface SubscriptionForm {
  subscriptionPlanId: string;
  endDate: string;
  isTrial: boolean;
}

const emptyPlanForm: SaveSubscriptionPlanRequest = {
  name: "",
  description: "",
  monthlyPrice: 0,
  yearlyPrice: 0,
  maxUsers: 1,
  maxVehicles: 1,
  isActive: true
};

function formatDate(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "-" : date.toLocaleDateString("tr-TR");
}

function daysLeft(value: string): number {
  const diff = new Date(value).getTime() - Date.now();
  return Math.ceil(diff / (1000 * 60 * 60 * 24));
}

export function AdminTenantsPage() {
  const { session } = useAuth();
  const [tenants, setTenants] = useState<AdminTenant[]>([]);
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [plans, setPlans] = useState<AdminSubscriptionPlan[]>([]);
  const [selectedTenantId, setSelectedTenantId] = useState("");
  const [error, setError] = useState("");

  const [editingTenantId, setEditingTenantId] = useState<string | null>(null);
  const [subForm, setSubForm] = useState<SubscriptionForm>({ subscriptionPlanId: "", endDate: "", isTrial: false });

  const [editingPlanId, setEditingPlanId] = useState<string | null>(null);
  const [planForm, setPlanForm] = useState<SaveSubscriptionPlanRequest>(emptyPlanForm);

  async function loadAll(tenantId?: string) {
    if (!session) {
      return;
    }

    const [tenantData, userData, planData] = await Promise.all([
      api.adminGetTenants(session.token),
      api.adminGetUsers(session.token, tenantId),
      api.adminGetPlans(session.token)
    ]);
    setTenants(tenantData);
    setUsers(userData);
    setPlans(planData);
  }

  useEffect(() => {
    void loadAll();
  }, [session]);

  useEffect(() => {
    if (!session) {
      return;
    }
    void api.adminGetUsers(session.token, selectedTenantId || undefined).then(setUsers);
  }, [selectedTenantId]);

  async function run(action: () => Promise<unknown>) {
    setError("");
    try {
      await action();
      await loadAll(selectedTenantId || undefined);
    } catch (actionError) {
      setError(actionError instanceof Error ? actionError.message : "İşlem sırasında bir hata oluştu.");
    }
  }

  function toggleTenant(tenant: AdminTenant) {
    void run(() => api.adminUpdateTenantStatus(session!.token, tenant.id, !tenant.isActive));
  }

  function toggleUser(user: AdminUser) {
    void run(() => api.adminUpdateUserStatus(session!.token, user.id, !user.isActive));
  }

  function deleteTenant(tenant: AdminTenant) {
    if (!window.confirm(`"${tenant.name}" tenant'ı silinsin mi? Bu işlem tenant'ı pasifleştirir ve listeden kaldırır.`)) {
      return;
    }
    void run(() => api.adminDeleteTenant(session!.token, tenant.id));
  }

  function startEditSubscription(tenant: AdminTenant) {
    setEditingTenantId(tenant.id);
    setSubForm({
      subscriptionPlanId: tenant.subscriptionPlanId,
      endDate: tenant.subscriptionEndDate.slice(0, 10),
      isTrial: tenant.isTrial
    });
  }

  function saveSubscription(tenantId: string) {
    void run(() =>
      api.adminUpdateTenantSubscription(session!.token, tenantId, {
        subscriptionPlanId: subForm.subscriptionPlanId,
        subscriptionEndDate: new Date(`${subForm.endDate}T00:00:00Z`).toISOString(),
        isTrial: subForm.isTrial
      })
    ).then(() => setEditingTenantId(null));
  }

  function startEditPlan(plan: AdminSubscriptionPlan) {
    setEditingPlanId(plan.id);
    setPlanForm({
      name: plan.name,
      description: plan.description,
      monthlyPrice: plan.monthlyPrice,
      yearlyPrice: plan.yearlyPrice,
      maxUsers: plan.maxUsers,
      maxVehicles: plan.maxVehicles,
      isActive: plan.isActive
    });
  }

  function resetPlanForm() {
    setEditingPlanId(null);
    setPlanForm(emptyPlanForm);
  }

  function savePlan() {
    const payload = { ...planForm, name: planForm.name.trim() };
    void run(() =>
      editingPlanId
        ? api.adminUpdatePlan(session!.token, editingPlanId, payload)
        : api.adminCreatePlan(session!.token, payload)
    ).then(resetPlanForm);
  }

  return (
    <section className="page-stack">
      <PageHeader
        eyebrow="Super Admin"
        title="Tenant ve abonelik yönetimi"
        description="Tüm tenantları, planlarını ve abonelik durumlarını buradan yönetin."
      />

      {error ? <div className="alert error">{error}</div> : null}

      <article className="panel">
        <SearchableSelect
          label="Tenant filtre"
          value={selectedTenantId}
          options={tenants.map((item) => ({ id: item.id, name: item.name }))}
          placeholder="Tüm tenantlar"
          onChange={setSelectedTenantId}
        />
      </article>

      <article className="panel">
        <div className="panel-heading">
          <h2>Tenantlar</h2>
          <p>Plan, abonelik bitişi ve kullanım. Pasif tenantlar sisteme giriş yapamaz.</p>
        </div>
        <div className="records-list">
          {tenants.map((tenant) => {
            const remaining = daysLeft(tenant.subscriptionEndDate);
            return (
              <article key={tenant.id} className="record-card spacious">
                <div className="record-main">
                  <strong>
                    {tenant.name}
                    {tenant.isTrial ? <span className="badge"> Deneme</span> : null}
                  </strong>
                  <span>{tenant.identifier ?? "-"}</span>
                </div>
                <div className="record-grid">
                  <div><span>Plan</span><strong>{tenant.subscriptionPlanName ?? "-"}</strong></div>
                  <div><span>Bitiş</span><strong>{formatDate(tenant.subscriptionEndDate)}</strong></div>
                  <div><span>Kalan</span><strong>{remaining < 0 ? "Süresi doldu" : `${remaining} gün`}</strong></div>
                  <div><span>Araç</span><strong>{tenant.vehicleCount}</strong></div>
                  <div><span>User</span><strong>{tenant.userCount}</strong></div>
                  <div><span>Durum</span><strong>{tenant.isActive ? "Aktif" : "Pasif"}</strong></div>
                </div>

                {editingTenantId === tenant.id ? (
                  <div className="panel inline-editor">
                    <label>
                      <span>Plan</span>
                      <select
                        value={subForm.subscriptionPlanId}
                        onChange={(event) => setSubForm((current) => ({ ...current, subscriptionPlanId: event.target.value }))}
                      >
                        {plans.map((plan) => (
                          <option key={plan.id} value={plan.id}>{plan.name}</option>
                        ))}
                      </select>
                    </label>
                    <label>
                      <span>Abonelik bitişi</span>
                      <input
                        type="date"
                        value={subForm.endDate}
                        onChange={(event) => setSubForm((current) => ({ ...current, endDate: event.target.value }))}
                      />
                    </label>
                    <label className="checkbox-row">
                      <input
                        type="checkbox"
                        checked={subForm.isTrial}
                        onChange={(event) => setSubForm((current) => ({ ...current, isTrial: event.target.checked }))}
                      />
                      <span>Deneme sürümü</span>
                    </label>
                    <div className="button-row">
                      <button type="button" className="primary-button" onClick={() => saveSubscription(tenant.id)}>Kaydet</button>
                      <button type="button" className="ghost-button" onClick={() => setEditingTenantId(null)}>Vazgeç</button>
                    </div>
                  </div>
                ) : (
                  <div className="button-row">
                    <button type="button" className="primary-button" onClick={() => startEditSubscription(tenant)}>Abonelik düzenle</button>
                    <button type="button" className="ghost-button" onClick={() => toggleTenant(tenant)}>
                      {tenant.isActive ? "Pasif yap" : "Aktif yap"}
                    </button>
                    <button type="button" className="ghost-button danger" onClick={() => deleteTenant(tenant)}>Sil</button>
                  </div>
                )}
              </article>
            );
          })}
        </div>
      </article>

      <article className="panel">
        <div className="panel-heading">
          <h2>Abonelik planları</h2>
          <p>Fiyat, limit ve aktiflik. Pasif planlar yeni atamalarda gösterilmemelidir.</p>
        </div>
        <div className="records-list">
          {plans.map((plan) => (
            <article key={plan.id} className="record-card spacious">
              <div className="record-main">
                <strong>{plan.name}{plan.isActive ? "" : <span className="badge"> Pasif</span>}</strong>
                <span>{plan.description}</span>
              </div>
              <div className="record-grid">
                <div><span>Aylık</span><strong>{plan.monthlyPrice} ₺</strong></div>
                <div><span>Yıllık</span><strong>{plan.yearlyPrice} ₺</strong></div>
                <div><span>Maks. kullanıcı</span><strong>{plan.maxUsers}</strong></div>
                <div><span>Maks. araç</span><strong>{plan.maxVehicles}</strong></div>
              </div>
              <div className="button-row">
                <button type="button" className="ghost-button" onClick={() => startEditPlan(plan)}>Düzenle</button>
              </div>
            </article>
          ))}
        </div>

        <div className="panel inline-editor">
          <h3>{editingPlanId ? "Planı düzenle" : "Yeni plan"}</h3>
          <div className="form-grid">
            <label>
              <span>Ad</span>
              <input value={planForm.name} onChange={(event) => setPlanForm((current) => ({ ...current, name: event.target.value }))} placeholder="Pro" />
            </label>
            <label>
              <span>Açıklama</span>
              <input value={planForm.description} onChange={(event) => setPlanForm((current) => ({ ...current, description: event.target.value }))} placeholder="Pro plan" />
            </label>
            <label>
              <span>Aylık fiyat (₺)</span>
              <input type="number" min={0} value={planForm.monthlyPrice} onChange={(event) => setPlanForm((current) => ({ ...current, monthlyPrice: Number(event.target.value) }))} />
            </label>
            <label>
              <span>Yıllık fiyat (₺)</span>
              <input type="number" min={0} value={planForm.yearlyPrice} onChange={(event) => setPlanForm((current) => ({ ...current, yearlyPrice: Number(event.target.value) }))} />
            </label>
            <label>
              <span>Maks. kullanıcı</span>
              <input type="number" min={1} value={planForm.maxUsers} onChange={(event) => setPlanForm((current) => ({ ...current, maxUsers: Number(event.target.value) }))} />
            </label>
            <label>
              <span>Maks. araç</span>
              <input type="number" min={1} value={planForm.maxVehicles} onChange={(event) => setPlanForm((current) => ({ ...current, maxVehicles: Number(event.target.value) }))} />
            </label>
            <label className="checkbox-row">
              <input type="checkbox" checked={planForm.isActive} onChange={(event) => setPlanForm((current) => ({ ...current, isActive: event.target.checked }))} />
              <span>Aktif</span>
            </label>
          </div>
          <div className="button-row">
            <button type="button" className="primary-button" onClick={savePlan}>{editingPlanId ? "Kaydet" : "Plan oluştur"}</button>
            {editingPlanId ? <button type="button" className="ghost-button" onClick={resetPlanForm}>Vazgeç</button> : null}
          </div>
        </div>
      </article>

      <article className="panel">
        <div className="panel-heading">
          <h2>Kullanıcılar</h2>
          <p>Super admin ve tenant kullanıcılarının durumlarını yönetin.</p>
        </div>
        <div className="records-list">
          {users.map((user) => (
            <article key={user.id} className="record-card spacious">
              <div className="record-main">
                <strong>{user.fullName}</strong>
                <span>{user.email} / {user.tenantName}</span>
              </div>
              <div className="record-grid">
                <div><span>Rol</span><strong>{user.isSuperAdmin ? "Super Admin" : "User"}</strong></div>
                <div><span>Durum</span><strong>{user.isActive ? "Aktif" : "Pasif"}</strong></div>
              </div>
              <button type="button" className="primary-button" onClick={() => toggleUser(user)}>
                {user.isActive ? "Pasif yap" : "Aktif yap"}
              </button>
            </article>
          ))}
        </div>
      </article>
    </section>
  );
}
