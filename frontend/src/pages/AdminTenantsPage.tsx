import { useEffect, useMemo, useState } from "react";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { PageHeader } from "../components/PageHeader";
import { SearchableSelect } from "../components/SearchableSelect";
import { api } from "../lib/api";
import { formatCurrency, formatDate, formatDateOnly } from "../lib/format";
import { useAuth } from "../state/AuthContext";
import type { AdminSubscriptionPlan, AdminTenant, AdminUser, TenantActivity } from "../types";

function initials(name: string) {
  return (
    name
      .split(" ")
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase() ?? "")
      .join("") || "?"
  );
}

function toDateInput(iso: string) {
  if (!iso) {
    return "";
  }
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return "";
  }
  const pad = (part: number) => part.toString().padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

function dateInputToIso(value: string) {
  return value ? `${value}T00:00:00.000Z` : "";
}

function defaultEndDate() {
  const date = new Date();
  date.setFullYear(date.getFullYear() + 1);
  return toDateInput(date.toISOString());
}

function daysRemaining(iso: string) {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return Number.NaN;
  }
  return Math.ceil((date.getTime() - Date.now()) / 86400000);
}

// (eski isExpired kaldırıldı — yerini expiryBadge aldı)

function expiryBadge(iso: string): { label: string; tone: string } {
  const days = daysRemaining(iso);
  if (Number.isNaN(days)) {
    return { label: "Tarih yok", tone: "status-neutral" };
  }
  if (days < 0) {
    return { label: `Süresi doldu (${Math.abs(days)} gün önce)`, tone: "status-danger" };
  }
  if (days === 0) {
    return { label: "Bugün doluyor", tone: "status-danger" };
  }
  if (days <= 30) {
    return { label: `${days} gün kaldı`, tone: "status-warning" };
  }
  return { label: `${days} gün kaldı`, tone: "status-neutral" };
}

type TenantStatusFilter = "all" | "active" | "passive";

interface TenantFormState {
  name: string;
  identifier: string;
  subscriptionPlanId: string;
  subscriptionEndDate: string;
  isActive: boolean;
  adminFirstName: string;
  adminLastName: string;
  adminEmail: string;
  adminPassword: string;
}

interface UserFormState {
  tenantId: string;
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  isSuperAdmin: boolean;
  isActive: boolean;
}

type ConfirmTarget =
  | { kind: "tenant"; id: string; name: string }
  | { kind: "user"; id: string; name: string };

export function AdminTenantsPage() {
  const { session } = useAuth();
  const [tenants, setTenants] = useState<AdminTenant[]>([]);
  const [plans, setPlans] = useState<AdminSubscriptionPlan[]>([]);
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [selectedTenantId, setSelectedTenantId] = useState("");
  const [tenantSearch, setTenantSearch] = useState("");
  const [userSearch, setUserSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<TenantStatusFilter>("all");
  const [maxDays, setMaxDays] = useState("");
  const [error, setError] = useState("");

  const [tenantModal, setTenantModal] = useState<{ mode: "create" | "edit"; tenant?: AdminTenant } | null>(null);
  const [tenantForm, setTenantForm] = useState<TenantFormState | null>(null);
  const [userModal, setUserModal] = useState<{ mode: "create" | "edit"; user?: AdminUser } | null>(null);
  const [userForm, setUserForm] = useState<UserFormState | null>(null);
  const [passwordTarget, setPasswordTarget] = useState<AdminUser | null>(null);
  const [passwordValue, setPasswordValue] = useState("");
  const [confirmTarget, setConfirmTarget] = useState<ConfirmTarget | null>(null);
  const [activityTenant, setActivityTenant] = useState<AdminTenant | null>(null);
  const [activities, setActivities] = useState<TenantActivity[]>([]);
  const [activityLoading, setActivityLoading] = useState(false);
  const [modalError, setModalError] = useState("");
  const [busy, setBusy] = useState(false);

  async function loadTenantsAndUsers(tenantId?: string) {
    if (!session) {
      return;
    }
    try {
      setError("");
      const [tenantData, userData] = await Promise.all([
        api.adminGetTenants(session.token),
        api.adminGetUsers(session.token, tenantId)
      ]);
      setTenants(tenantData);
      setUsers(userData);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Veriler yüklenemedi.");
    }
  }

  useEffect(() => {
    if (!session) {
      return;
    }
    void loadTenantsAndUsers();
    void api.adminGetSubscriptionPlans(session.token).then(setPlans).catch(() => undefined);
  }, [session]);

  useEffect(() => {
    void loadTenantsAndUsers(selectedTenantId || undefined);
  }, [selectedTenantId]);

  async function toggleTenant(tenant: AdminTenant) {
    if (!session) {
      return;
    }
    try {
      await api.adminUpdateTenantStatus(session.token, tenant.id, !tenant.isActive);
      await loadTenantsAndUsers(selectedTenantId || undefined);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Tenant durumu değiştirilemedi.");
    }
  }

  async function toggleUser(user: AdminUser) {
    if (!session) {
      return;
    }
    try {
      await api.adminUpdateUserStatus(session.token, user.id, !user.isActive);
      await loadTenantsAndUsers(selectedTenantId || undefined);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Kullanıcı durumu değiştirilemedi.");
    }
  }

  function openCreateTenant() {
    setModalError("");
    setTenantModal({ mode: "create" });
    setTenantForm({
      name: "",
      identifier: "",
      subscriptionPlanId: plans[0]?.id ?? "",
      subscriptionEndDate: defaultEndDate(),
      isActive: true,
      adminFirstName: "",
      adminLastName: "",
      adminEmail: "",
      adminPassword: ""
    });
  }

  function openEditTenant(tenant: AdminTenant) {
    setModalError("");
    setTenantModal({ mode: "edit", tenant });
    setTenantForm({
      name: tenant.name,
      identifier: tenant.identifier ?? "",
      subscriptionPlanId: tenant.subscriptionPlanId,
      subscriptionEndDate: toDateInput(tenant.subscriptionEndDate),
      isActive: tenant.isActive,
      adminFirstName: "",
      adminLastName: "",
      adminEmail: "",
      adminPassword: ""
    });
  }

  function openCreateUser(tenantId?: string) {
    setModalError("");
    setUserModal({ mode: "create" });
    setUserForm({
      tenantId: tenantId || selectedTenantId || tenants[0]?.id || "",
      firstName: "",
      lastName: "",
      email: "",
      password: "",
      isSuperAdmin: false,
      isActive: true
    });
  }

  function openEditUser(user: AdminUser) {
    setModalError("");
    setUserModal({ mode: "edit", user });
    setUserForm({
      tenantId: user.tenantId,
      firstName: user.fullName.split(" ").slice(0, -1).join(" ") || user.fullName,
      lastName: user.fullName.split(" ").slice(-1).join(" ") || "",
      email: user.email,
      password: "",
      isSuperAdmin: user.isSuperAdmin,
      isActive: user.isActive
    });
  }

  function closeModals() {
    setTenantModal(null);
    setTenantForm(null);
    setUserModal(null);
    setUserForm(null);
    setPasswordTarget(null);
    setPasswordValue("");
    setConfirmTarget(null);
    setActivityTenant(null);
    setActivities([]);
    setModalError("");
  }

  async function openActivities(tenant: AdminTenant) {
    if (!session) {
      return;
    }
    setActivityTenant(tenant);
    setActivities([]);
    setActivityLoading(true);
    try {
      setActivities(await api.adminGetTenantActivities(session.token, tenant.id));
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "İşlem geçmişi yüklenemedi.");
      setActivityTenant(null);
    } finally {
      setActivityLoading(false);
    }
  }

  async function submitTenant() {
    if (!session || !tenantForm || !tenantModal) {
      return;
    }
    setBusy(true);
    setModalError("");
    try {
      if (tenantModal.mode === "create") {
        await api.adminCreateTenant(session.token, {
          name: tenantForm.name,
          identifier: tenantForm.identifier.trim() || null,
          subscriptionPlanId: tenantForm.subscriptionPlanId,
          subscriptionEndDate: dateInputToIso(tenantForm.subscriptionEndDate),
          adminFirstName: tenantForm.adminFirstName,
          adminLastName: tenantForm.adminLastName,
          adminEmail: tenantForm.adminEmail,
          adminPassword: tenantForm.adminPassword
        });
      } else if (tenantModal.tenant) {
        await api.adminUpdateTenant(session.token, tenantModal.tenant.id, {
          name: tenantForm.name,
          identifier: tenantForm.identifier.trim() || null,
          subscriptionPlanId: tenantForm.subscriptionPlanId,
          subscriptionEndDate: dateInputToIso(tenantForm.subscriptionEndDate),
          isActive: tenantForm.isActive
        });
      }
      closeModals();
      await loadTenantsAndUsers(selectedTenantId || undefined);
    } catch (requestError) {
      setModalError(requestError instanceof Error ? requestError.message : "Tenant kaydedilemedi.");
    } finally {
      setBusy(false);
    }
  }

  async function submitUser() {
    if (!session || !userForm || !userModal) {
      return;
    }
    setBusy(true);
    setModalError("");
    try {
      if (userModal.mode === "create") {
        await api.adminCreateUser(session.token, {
          tenantId: userForm.tenantId,
          firstName: userForm.firstName,
          lastName: userForm.lastName,
          email: userForm.email,
          password: userForm.password,
          isSuperAdmin: userForm.isSuperAdmin
        });
      } else if (userModal.user) {
        await api.adminUpdateUser(session.token, userModal.user.id, {
          firstName: userForm.firstName,
          lastName: userForm.lastName,
          email: userForm.email,
          isActive: userForm.isActive,
          isSuperAdmin: userForm.isSuperAdmin
        });
      }
      closeModals();
      await loadTenantsAndUsers(selectedTenantId || undefined);
    } catch (requestError) {
      setModalError(requestError instanceof Error ? requestError.message : "Kullanıcı kaydedilemedi.");
    } finally {
      setBusy(false);
    }
  }

  async function submitPassword() {
    if (!session || !passwordTarget) {
      return;
    }
    setBusy(true);
    setModalError("");
    try {
      await api.adminSetUserPassword(session.token, passwordTarget.id, passwordValue);
      closeModals();
    } catch (requestError) {
      setModalError(requestError instanceof Error ? requestError.message : "Şifre güncellenemedi.");
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
      if (confirmTarget.kind === "tenant") {
        await api.adminDeleteTenant(session.token, confirmTarget.id);
        const wasSelected = selectedTenantId === confirmTarget.id;
        if (wasSelected) {
          setSelectedTenantId("");
        }
        closeModals();
        await loadTenantsAndUsers(wasSelected ? undefined : selectedTenantId || undefined);
      } else {
        await api.adminDeleteUser(session.token, confirmTarget.id);
        closeModals();
        await loadTenantsAndUsers(selectedTenantId || undefined);
      }
    } catch (requestError) {
      setModalError(requestError instanceof Error ? requestError.message : "Silme işlemi başarısız oldu.");
    } finally {
      setBusy(false);
    }
  }

  const selectedTenant = tenants.find((tenant) => tenant.id === selectedTenantId) ?? null;

  const filteredTenants = useMemo(() => {
    const query = tenantSearch.trim().toLowerCase();
    const maxDaysNum = maxDays.trim() === "" ? null : Number(maxDays);
    return tenants.filter((tenant) => {
      if (
        query &&
        !(tenant.name.toLowerCase().includes(query) || (tenant.identifier ?? "").toLowerCase().includes(query))
      ) {
        return false;
      }
      if (statusFilter === "active" && !tenant.isActive) {
        return false;
      }
      if (statusFilter === "passive" && tenant.isActive) {
        return false;
      }
      if (maxDaysNum !== null && !Number.isNaN(maxDaysNum)) {
        const days = daysRemaining(tenant.subscriptionEndDate);
        if (Number.isNaN(days) || days > maxDaysNum) {
          return false;
        }
      }
      return true;
    });
  }, [tenants, tenantSearch, statusFilter, maxDays]);

  const hasActiveFilters = tenantSearch.trim() !== "" || statusFilter !== "all" || maxDays.trim() !== "";

  // Arama yapıldığında ilk sonucu otomatik seç.
  const firstFilteredId = filteredTenants[0]?.id ?? "";
  useEffect(() => {
    if (tenantSearch.trim() && firstFilteredId) {
      setSelectedTenantId(firstFilteredId);
    }
  }, [tenantSearch, firstFilteredId]);

  const filteredUsers = useMemo(() => {
    const query = userSearch.trim().toLowerCase();
    if (!query) {
      return users;
    }
    return users.filter(
      (user) => user.fullName.toLowerCase().includes(query) || user.email.toLowerCase().includes(query)
    );
  }, [users, userSearch]);

  const groupedUsers = useMemo(() => {
    const groups = new Map<string, { tenantId: string; tenantName: string; users: AdminUser[] }>();
    for (const user of filteredUsers) {
      const group = groups.get(user.tenantId);
      if (group) {
        group.users.push(user);
      } else {
        groups.set(user.tenantId, { tenantId: user.tenantId, tenantName: user.tenantName, users: [user] });
      }
    }
    return Array.from(groups.values()).sort((a, b) => a.tenantName.localeCompare(b.tenantName, "tr"));
  }, [filteredUsers]);

  const planOptions = plans.map((plan) => ({
    id: plan.id,
    name: `${plan.name} · ${formatCurrency(plan.monthlyPrice)}/ay · ${plan.maxUsers} kullanıcı`
  }));
  const tenantOptions = tenants.map((tenant) => ({ id: tenant.id, name: tenant.name }));

  function renderUserCard(user: AdminUser) {
    return (
      <article key={user.id} className="record-card admin-user-card">
        <span className="admin-avatar user">{initials(user.fullName)}</span>
        <div className="record-main">
          <strong>{user.fullName}</strong>
          <span>{user.email}</span>
          <span>Son giriş: {user.lastLoginAt ? formatDate(user.lastLoginAt) : "—"}</span>
        </div>
        <div className="admin-user-tags">
          <span className={`status-badge ${user.isSuperAdmin ? "status-warning" : "status-neutral"}`}>
            {user.isSuperAdmin ? "Super Admin" : "Kullanıcı"}
          </span>
          <span className={`status-badge ${user.isActive ? "status-success" : "status-danger"}`}>
            {user.isActive ? "Aktif" : "Pasif"}
          </span>
        </div>
        <div className="admin-user-actions">
          <button type="button" className="ghost-button dark" onClick={() => openEditUser(user)}>
            Düzenle
          </button>
          <button
            type="button"
            className="ghost-button"
            onClick={() => {
              setModalError("");
              setPasswordValue("");
              setPasswordTarget(user);
            }}
          >
            Şifre ata
          </button>
          <button
            type="button"
            className={user.isActive ? "ghost-button" : "primary-button"}
            onClick={() => void toggleUser(user)}
          >
            {user.isActive ? "Pasif yap" : "Aktif yap"}
          </button>
          <button
            type="button"
            className="ghost-button danger"
            onClick={() => {
              setModalError("");
              setConfirmTarget({ kind: "user", id: user.id, name: user.fullName });
            }}
          >
            Sil
          </button>
        </div>
      </article>
    );
  }

  return (
    <section className="page-stack">
      <PageHeader
        eyebrow="Super Admin"
        title="Tenant ve Kullanıcı Yönetimi"
        description="Soldan tenant arayıp seçin; sağ panelde o tenant'ın kullanıcılarını ve paket bilgisini yönetin."
      />

      {error ? <div className="alert error">{error}</div> : null}

      <article className="panel admin-filters">
        <div className="admin-filter-bar">
          <input
            className="admin-search"
            value={tenantSearch}
            onChange={(event) => setTenantSearch(event.target.value)}
            placeholder="Tenant adı veya tanımlayıcı ara..."
          />
          <div className="admin-status-toggle" role="group" aria-label="Durum filtresi">
            <button
              type="button"
              className={statusFilter === "all" ? "primary-button" : "ghost-button"}
              onClick={() => setStatusFilter("all")}
            >
              Tümü
            </button>
            <button
              type="button"
              className={statusFilter === "active" ? "primary-button" : "ghost-button"}
              onClick={() => setStatusFilter("active")}
            >
              Aktif
            </button>
            <button
              type="button"
              className={statusFilter === "passive" ? "primary-button" : "ghost-button"}
              onClick={() => setStatusFilter("passive")}
            >
              Pasif
            </button>
          </div>
          <label className="admin-days-filter">
            <span className="field-label">Kalan gün ≤</span>
            <input
              type="number"
              min="0"
              value={maxDays}
              onChange={(event) => setMaxDays(event.target.value)}
              placeholder="örn. 30"
            />
          </label>
          {hasActiveFilters ? (
            <button
              type="button"
              className="ghost-button"
              onClick={() => {
                setTenantSearch("");
                setStatusFilter("all");
                setMaxDays("");
              }}
            >
              Filtreleri temizle
            </button>
          ) : null}
        </div>
        <p className="admin-filter-summary">
          {filteredTenants.length} / {tenants.length} tenant gösteriliyor
        </p>
      </article>

      <div className="admin-layout">
        <article className="panel admin-list-panel">
          <div className="panel-heading admin-panel-heading">
            <div>
              <h2>Tenantlar ({tenants.length})</h2>
              <p>Yönetmek için bir tenant'a tıklayın.</p>
            </div>
            <button type="button" className="primary-button" onClick={openCreateTenant}>
              + Tenant ekle
            </button>
          </div>

          <div className="admin-tenant-list">
            {filteredTenants.map((tenant) => {
              const isSelected = tenant.id === selectedTenantId;
              const expiry = expiryBadge(tenant.subscriptionEndDate);
              return (
                <article key={tenant.id} className={`admin-tenant-row ${isSelected ? "selected" : ""}`}>
                  <button
                    type="button"
                    className="admin-tenant-row-main"
                    onClick={() => setSelectedTenantId(isSelected ? "" : tenant.id)}
                    aria-pressed={isSelected}
                  >
                    <span className="admin-avatar tenant">{initials(tenant.name)}</span>
                    <span className="record-main">
                      <strong>{tenant.name}</strong>
                      <span>{tenant.identifier ?? "—"}</span>
                    </span>
                    <span className={`status-badge ${tenant.isActive ? "status-success" : "status-danger"}`}>
                      {tenant.isActive ? "Aktif" : "Pasif"}
                    </span>
                  </button>
                  <div className="admin-tenant-row-meta">
                    <span className="status-badge status-neutral">{tenant.subscriptionPlanName}</span>
                    <span
                      className={`status-badge ${expiry.tone}`}
                      title={`Bitiş: ${formatDateOnly(tenant.subscriptionEndDate)}`}
                    >
                      {expiry.label}
                    </span>
                    <span className="admin-usercount">{tenant.userCount} kullanıcı</span>
                  </div>
                  <div className="admin-tenant-row-actions">
                    <button type="button" className="ghost-button dark" onClick={() => openEditTenant(tenant)}>
                      Düzenle
                    </button>
                    <button
                      type="button"
                      className={tenant.isActive ? "ghost-button" : "primary-button"}
                      onClick={() => void toggleTenant(tenant)}
                    >
                      {tenant.isActive ? "Pasif yap" : "Aktif yap"}
                    </button>
                    <button
                      type="button"
                      className="ghost-button danger"
                      onClick={() => {
                        setModalError("");
                        setConfirmTarget({ kind: "tenant", id: tenant.id, name: tenant.name });
                      }}
                    >
                      Sil
                    </button>
                  </div>
                </article>
              );
            })}
            {filteredTenants.length === 0 ? (
              <div className="empty-panel">{tenants.length === 0 ? "Tenant bulunamadı." : "Aramayla eşleşen tenant yok."}</div>
            ) : null}
          </div>
        </article>

        <article className="panel admin-detail-panel">
          <div className="panel-heading admin-panel-heading">
            <div>
              <h2>{selectedTenant ? selectedTenant.name : "Tüm kullanıcılar"} ({filteredUsers.length})</h2>
              <p>
                {selectedTenant
                  ? "Bu tenant'a ait kullanıcılar. Yeni kullanıcı ekleyebilirsiniz."
                  : "Bir tenant seçilmedi — tüm kullanıcılar tenant'a göre gruplanmıştır."}
              </p>
            </div>
            <div className="inline-actions">
              {selectedTenant ? (
                <>
                  <button type="button" className="ghost-button" onClick={() => void openActivities(selectedTenant)}>
                    📜 İşlem Geçmişi
                  </button>
                  <button type="button" className="ghost-button" onClick={() => setSelectedTenantId("")}>
                    Seçimi kaldır
                  </button>
                </>
              ) : null}
              <button
                type="button"
                className="primary-button"
                onClick={() => openCreateUser(selectedTenant?.id)}
                disabled={tenants.length === 0}
              >
                + Kullanıcı ekle
              </button>
            </div>
          </div>

          <input
            className="admin-search"
            value={userSearch}
            onChange={(event) => setUserSearch(event.target.value)}
            placeholder="Kullanıcı adı veya e-posta ara..."
          />

          {selectedTenant ? (
            <div className="admin-detail-summary">
              <span className="status-badge status-neutral">{selectedTenant.subscriptionPlanName}</span>
              <span
                className={`status-badge ${expiryBadge(selectedTenant.subscriptionEndDate).tone}`}
                title={`Bitiş: ${formatDateOnly(selectedTenant.subscriptionEndDate)}`}
              >
                {expiryBadge(selectedTenant.subscriptionEndDate).label}
              </span>
              <span className={`status-badge ${selectedTenant.isActive ? "status-success" : "status-danger"}`}>
                {selectedTenant.isActive ? "Aktif" : "Pasif"}
              </span>
            </div>
          ) : null}

          {selectedTenant ? (
            <div className="records-list">
              {filteredUsers.map((user) => renderUserCard(user))}
              {filteredUsers.length === 0 ? (
                <div className="empty-panel">{users.length === 0 ? "Bu tenant'ta kullanıcı yok." : "Aramayla eşleşen kullanıcı yok."}</div>
              ) : null}
            </div>
          ) : (
            <div className="admin-user-groups">
              {groupedUsers.map((group) => (
                <div key={group.tenantId} className="admin-user-group">
                  <button
                    type="button"
                    className="admin-group-header admin-group-header-button"
                    onClick={() => setSelectedTenantId(group.tenantId)}
                  >
                    <span className="admin-avatar tenant sm">{initials(group.tenantName)}</span>
                    <strong>{group.tenantName}</strong>
                    <span className="admin-group-count">{group.users.length}</span>
                  </button>
                  <div className="records-list">{group.users.map((user) => renderUserCard(user))}</div>
                </div>
              ))}
              {filteredUsers.length === 0 ? (
                <div className="empty-panel">{users.length === 0 ? "Kullanıcı bulunamadı." : "Aramayla eşleşen kullanıcı yok."}</div>
              ) : null}
            </div>
          )}
        </article>
      </div>

      {tenantModal && tenantForm ? (
        <div className="modal-backdrop">
          <div className="panel modal-panel">
            <div className="modal-header">
              <div className="panel-heading">
                <h2>{tenantModal.mode === "create" ? "Yeni tenant" : "Tenant düzenle"}</h2>
              </div>
              <button type="button" className="modal-close" onClick={closeModals} aria-label="Kapat">
                X
              </button>
            </div>
            <div className="data-form">
              <label>
                <span className="field-label required">Tenant adı</span>
                <input
                  value={tenantForm.name}
                  onChange={(event) => setTenantForm({ ...tenantForm, name: event.target.value })}
                  placeholder="Örn: Yıldız Otomotiv"
                />
              </label>
              <label>
                <span className="field-label">Tanımlayıcı (subdomain/slug)</span>
                <input
                  value={tenantForm.identifier}
                  onChange={(event) => setTenantForm({ ...tenantForm, identifier: event.target.value })}
                  placeholder="Boş bırakılırsa otomatik üretilir"
                />
              </label>
              <div className="form-grid two-columns">
                <SearchableSelect
                  label="Abonelik paketi"
                  value={tenantForm.subscriptionPlanId}
                  options={planOptions}
                  placeholder="Paket seçin"
                  onChange={(value) => setTenantForm({ ...tenantForm, subscriptionPlanId: value })}
                  required
                />
                <label>
                  <span className="field-label required">Geçerlilik bitişi</span>
                  <input
                    type="date"
                    value={tenantForm.subscriptionEndDate}
                    onChange={(event) => setTenantForm({ ...tenantForm, subscriptionEndDate: event.target.value })}
                  />
                </label>
              </div>

              {tenantModal.mode === "create" ? (
                <>
                  <div className="admin-form-divider">Yönetici hesabı</div>
                  <div className="form-grid two-columns">
                    <label>
                      <span className="field-label">Ad</span>
                      <input
                        value={tenantForm.adminFirstName}
                        onChange={(event) => setTenantForm({ ...tenantForm, adminFirstName: event.target.value })}
                      />
                    </label>
                    <label>
                      <span className="field-label">Soyad</span>
                      <input
                        value={tenantForm.adminLastName}
                        onChange={(event) => setTenantForm({ ...tenantForm, adminLastName: event.target.value })}
                      />
                    </label>
                  </div>
                  <label>
                    <span className="field-label required">Yönetici e-posta</span>
                    <input
                      type="email"
                      value={tenantForm.adminEmail}
                      onChange={(event) => setTenantForm({ ...tenantForm, adminEmail: event.target.value })}
                      placeholder="admin@firma.com"
                    />
                  </label>
                  <label>
                    <span className="field-label required">Şifre (min. 6 karakter)</span>
                    <input
                      type="text"
                      value={tenantForm.adminPassword}
                      onChange={(event) => setTenantForm({ ...tenantForm, adminPassword: event.target.value })}
                      placeholder="Geçici şifre"
                    />
                  </label>
                </>
              ) : (
                <label className="admin-checkbox">
                  <input
                    type="checkbox"
                    checked={tenantForm.isActive}
                    onChange={(event) => setTenantForm({ ...tenantForm, isActive: event.target.checked })}
                  />
                  <span>Tenant aktif</span>
                </label>
              )}

              {modalError ? <div className="alert error">{modalError}</div> : null}
              <div className="inline-actions">
                <button type="button" className="ghost-button dark" onClick={closeModals} disabled={busy}>
                  Vazgeç
                </button>
                <button type="button" className="primary-button" onClick={() => void submitTenant()} disabled={busy}>
                  {busy ? "Kaydediliyor..." : "Kaydet"}
                </button>
              </div>
            </div>
          </div>
        </div>
      ) : null}

      {userModal && userForm ? (
        <div className="modal-backdrop">
          <div className="panel modal-panel">
            <div className="modal-header">
              <div className="panel-heading">
                <h2>{userModal.mode === "create" ? "Yeni kullanıcı" : "Kullanıcı düzenle"}</h2>
              </div>
              <button type="button" className="modal-close" onClick={closeModals} aria-label="Kapat">
                X
              </button>
            </div>
            <div className="data-form">
              {userModal.mode === "create" ? (
                <SearchableSelect
                  label="Tenant"
                  value={userForm.tenantId}
                  options={tenantOptions}
                  placeholder="Tenant seçin"
                  onChange={(value) => setUserForm({ ...userForm, tenantId: value })}
                  required
                />
              ) : null}
              <div className="form-grid two-columns">
                <label>
                  <span className="field-label">Ad</span>
                  <input
                    value={userForm.firstName}
                    onChange={(event) => setUserForm({ ...userForm, firstName: event.target.value })}
                  />
                </label>
                <label>
                  <span className="field-label">Soyad</span>
                  <input
                    value={userForm.lastName}
                    onChange={(event) => setUserForm({ ...userForm, lastName: event.target.value })}
                  />
                </label>
              </div>
              <label>
                <span className="field-label required">E-posta</span>
                <input
                  type="email"
                  value={userForm.email}
                  onChange={(event) => setUserForm({ ...userForm, email: event.target.value })}
                  placeholder="kullanici@firma.com"
                />
              </label>
              {userModal.mode === "create" ? (
                <label>
                  <span className="field-label required">Şifre (min. 6 karakter)</span>
                  <input
                    type="text"
                    value={userForm.password}
                    onChange={(event) => setUserForm({ ...userForm, password: event.target.value })}
                    placeholder="Geçici şifre"
                  />
                </label>
              ) : null}
              <label className="admin-checkbox">
                <input
                  type="checkbox"
                  checked={userForm.isSuperAdmin}
                  onChange={(event) => setUserForm({ ...userForm, isSuperAdmin: event.target.checked })}
                />
                <span>Super Admin yetkisi</span>
              </label>
              {userModal.mode === "edit" ? (
                <label className="admin-checkbox">
                  <input
                    type="checkbox"
                    checked={userForm.isActive}
                    onChange={(event) => setUserForm({ ...userForm, isActive: event.target.checked })}
                  />
                  <span>Kullanıcı aktif</span>
                </label>
              ) : null}
              {modalError ? <div className="alert error">{modalError}</div> : null}
              <div className="inline-actions">
                <button type="button" className="ghost-button dark" onClick={closeModals} disabled={busy}>
                  Vazgeç
                </button>
                <button type="button" className="primary-button" onClick={() => void submitUser()} disabled={busy}>
                  {busy ? "Kaydediliyor..." : "Kaydet"}
                </button>
              </div>
            </div>
          </div>
        </div>
      ) : null}

      {passwordTarget ? (
        <div className="modal-backdrop">
          <div className="panel modal-panel confirm-panel">
            <div className="modal-header">
              <div className="panel-heading">
                <h2>Şifre ata</h2>
              </div>
              <button type="button" className="modal-close" onClick={closeModals} aria-label="Kapat">
                X
              </button>
            </div>
            <div className="data-form">
              <p className="confirm-copy">
                <strong>{passwordTarget.fullName}</strong> ({passwordTarget.email}) için yeni şifre belirleyin. Mevcut oturumları kapanır.
              </p>
              <label>
                <span className="field-label required">Yeni şifre (min. 6 karakter)</span>
                <input
                  type="text"
                  value={passwordValue}
                  onChange={(event) => setPasswordValue(event.target.value)}
                  placeholder="Yeni şifre"
                  autoFocus
                />
              </label>
              {modalError ? <div className="alert error">{modalError}</div> : null}
              <div className="inline-actions">
                <button type="button" className="ghost-button dark" onClick={closeModals} disabled={busy}>
                  Vazgeç
                </button>
                <button type="button" className="primary-button" onClick={() => void submitPassword()} disabled={busy}>
                  {busy ? "Kaydediliyor..." : "Şifreyi kaydet"}
                </button>
              </div>
            </div>
          </div>
        </div>
      ) : null}

      {activityTenant ? (
        <div className="modal-backdrop">
          <div className="panel modal-panel">
            <div className="modal-header">
              <div className="panel-heading">
                <h2>İşlem Geçmişi — {activityTenant.name}</h2>
              </div>
              <button type="button" className="modal-close" onClick={closeModals} aria-label="Kapat">
                X
              </button>
            </div>
            <div className="records-list">
              {activityLoading ? <div className="empty-panel">Yükleniyor...</div> : null}
              {!activityLoading && activities.length === 0 ? (
                <div className="empty-panel">İşlem kaydı yok.</div>
              ) : null}
              {activities.map((activity) => (
                <article key={activity.id} className="record-card admin-user-card">
                  <div className="record-main">
                    <strong>{activity.typeLabel}</strong>
                    <span>{activity.description}</span>
                  </div>
                  <div className="admin-user-tags">
                    {activity.amount != null ? (
                      <span className="status-badge status-neutral">{formatCurrency(activity.amount)}</span>
                    ) : null}
                    <span className="status-badge status-neutral">{formatDate(activity.createdAt)}</span>
                  </div>
                  <div className="admin-activity-by">{activity.performedBy ?? "Sistem"}</div>
                </article>
              ))}
            </div>
          </div>
        </div>
      ) : null}

      {confirmTarget ? (
        <ConfirmDialog
          title={confirmTarget.kind === "tenant" ? "Tenant silinsin mi?" : "Kullanıcı silinsin mi?"}
          description={
            confirmTarget.kind === "tenant"
              ? `"${confirmTarget.name}" tenant'ı ve tüm kullanıcıları pasifleştirilip silinecek. Bu işlem geri alınamaz.`
              : `"${confirmTarget.name}" kullanıcısı silinecek. Bu işlem geri alınamaz.`
          }
          error={modalError}
          busy={busy}
          onCancel={closeModals}
          onConfirm={() => void confirmDelete()}
        />
      ) : null}
    </section>
  );
}
