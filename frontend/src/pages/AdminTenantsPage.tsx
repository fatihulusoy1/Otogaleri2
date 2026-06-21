import { useEffect, useState } from "react";
import { PageHeader } from "../components/PageHeader";
import { SearchableSelect } from "../components/SearchableSelect";
import { api } from "../lib/api";
import { useAuth } from "../state/AuthContext";
import type { AdminTenant, AdminUser } from "../types";

export function AdminTenantsPage() {
  const { session } = useAuth();
  const [tenants, setTenants] = useState<AdminTenant[]>([]);
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [selectedTenantId, setSelectedTenantId] = useState("");

  async function loadTenantsAndUsers(tenantId?: string) {
    if (!session) {
      return;
    }

    const [tenantData, userData] = await Promise.all([
      api.adminGetTenants(session.token),
      api.adminGetUsers(session.token, tenantId)
    ]);
    setTenants(tenantData);
    setUsers(userData);
  }

  useEffect(() => {
    void loadTenantsAndUsers();
  }, [session]);

  useEffect(() => {
    void loadTenantsAndUsers(selectedTenantId || undefined);
  }, [selectedTenantId]);

  async function toggleTenant(tenant: AdminTenant) {
    if (!session) {
      return;
    }

    await api.adminUpdateTenantStatus(session.token, tenant.id, !tenant.isActive);
    await loadTenantsAndUsers(selectedTenantId || undefined);
  }

  async function toggleUser(user: AdminUser) {
    if (!session) {
      return;
    }

    await api.adminUpdateUserStatus(session.token, user.id, !user.isActive);
    await loadTenantsAndUsers(selectedTenantId || undefined);
  }

  return (
    <section className="page-stack">
      <PageHeader eyebrow="Super Admin" title="Tenant ve user yonetimi" description="Tum tenantlari ve kullanicilari gorun, aktiflik durumlarini buradan degistirin." />
      <article className="panel">
        <SearchableSelect label="Tenant filtre" value={selectedTenantId} options={tenants.map((item) => ({ id: item.id, name: item.name }))} placeholder="Tum tenantlar" onChange={setSelectedTenantId} />
      </article>
      <div className="content-grid two-columns">
        <article className="panel">
          <div className="panel-heading"><h2>Tenantlar</h2><p>Pasif tenantlar sisteme giris yapamaz.</p></div>
          <div className="records-list">
            {tenants.map((tenant) => (
              <article key={tenant.id} className="record-card spacious">
                <div className="record-main">
                  <strong>{tenant.name}</strong>
                  <span>{tenant.identifier ?? "-"}</span>
                </div>
                <div className="record-grid">
                  <div><span>User</span><strong>{tenant.userCount}</strong></div>
                  <div><span>Durum</span><strong>{tenant.isActive ? "Aktif" : "Pasif"}</strong></div>
                </div>
                <button type="button" className="primary-button" onClick={() => void toggleTenant(tenant)}>
                  {tenant.isActive ? "Pasif yap" : "Aktif yap"}
                </button>
              </article>
            ))}
          </div>
        </article>
        <article className="panel">
          <div className="panel-heading"><h2>Kullanicilar</h2><p>Super admin ve tenant kullanicilarinin durumlarini yonetin.</p></div>
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
                <button type="button" className="primary-button" onClick={() => void toggleUser(user)}>
                  {user.isActive ? "Pasif yap" : "Aktif yap"}
                </button>
              </article>
            ))}
          </div>
        </article>
      </div>
    </section>
  );
}
