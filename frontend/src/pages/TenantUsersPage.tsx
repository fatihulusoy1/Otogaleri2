import { useEffect, useState } from "react";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { PageHeader } from "../components/PageHeader";
import { api } from "../lib/api";
import { formatDate } from "../lib/format";
import { useAuth } from "../state/AuthContext";
import type { TenantUser } from "../types";

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

interface UserFormState {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  isTenantAdmin: boolean;
}

export function TenantUsersPage() {
  const { session } = useAuth();
  const [users, setUsers] = useState<TenantUser[]>([]);
  const [search, setSearch] = useState("");
  const [error, setError] = useState("");

  const [modal, setModal] = useState<{ mode: "create" | "edit"; user?: TenantUser } | null>(null);
  const [form, setForm] = useState<UserFormState | null>(null);
  const [passwordTarget, setPasswordTarget] = useState<TenantUser | null>(null);
  const [passwordValue, setPasswordValue] = useState("");
  const [confirmTarget, setConfirmTarget] = useState<TenantUser | null>(null);
  const [modalError, setModalError] = useState("");
  const [busy, setBusy] = useState(false);

  async function load() {
    if (!session) {
      return;
    }
    try {
      setError("");
      setUsers(await api.tenantGetUsers(session.token));
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Kullanıcılar yüklenemedi.");
    }
  }

  useEffect(() => {
    void load();
  }, [session?.token]);

  function openCreate() {
    setModalError("");
    setModal({ mode: "create" });
    setForm({ firstName: "", lastName: "", email: "", password: "", isTenantAdmin: false });
  }

  function openEdit(user: TenantUser) {
    setModalError("");
    setModal({ mode: "edit", user });
    setForm({
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      password: "",
      isTenantAdmin: user.isTenantAdmin
    });
  }

  function closeModals() {
    setModal(null);
    setForm(null);
    setPasswordTarget(null);
    setPasswordValue("");
    setConfirmTarget(null);
    setModalError("");
  }

  async function submitForm() {
    if (!session || !form || !modal) {
      return;
    }
    setBusy(true);
    setModalError("");
    try {
      if (modal.mode === "create") {
        await api.tenantCreateUser(session.token, {
          firstName: form.firstName,
          lastName: form.lastName,
          email: form.email,
          password: form.password,
          isTenantAdmin: form.isTenantAdmin
        });
      } else if (modal.user) {
        await api.tenantUpdateUser(session.token, modal.user.id, {
          firstName: form.firstName,
          lastName: form.lastName,
          email: form.email,
          isTenantAdmin: form.isTenantAdmin
        });
      }
      closeModals();
      await load();
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
      await api.tenantSetUserPassword(session.token, passwordTarget.id, passwordValue);
      closeModals();
    } catch (requestError) {
      setModalError(requestError instanceof Error ? requestError.message : "Şifre güncellenemedi.");
    } finally {
      setBusy(false);
    }
  }

  async function toggleStatus(user: TenantUser) {
    if (!session) {
      return;
    }
    try {
      await api.tenantSetUserStatus(session.token, user.id, !user.isActive);
      await load();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Durum değiştirilemedi.");
    }
  }

  async function confirmDelete() {
    if (!session || !confirmTarget) {
      return;
    }
    setBusy(true);
    setModalError("");
    try {
      await api.tenantDeleteUser(session.token, confirmTarget.id);
      closeModals();
      await load();
    } catch (requestError) {
      setModalError(requestError instanceof Error ? requestError.message : "Kullanıcı silinemedi.");
    } finally {
      setBusy(false);
    }
  }

  const query = search.trim().toLowerCase();
  const filteredUsers = query
    ? users.filter((user) => user.fullName.toLowerCase().includes(query) || user.email.toLowerCase().includes(query))
    : users;

  return (
    <section className="page-stack">
      <PageHeader
        eyebrow="Ekip"
        title="Kullanıcılar"
        description="Tenant kullanıcılarınızı ekleyin, düzenleyin, aktif/pasif yapın ve şifrelerini sıfırlayın."
      />

      {error ? <div className="alert error">{error}</div> : null}

      <article className="panel">
        <div className="panel-heading admin-panel-heading">
          <div>
            <h2>Kullanıcılar ({users.length})</h2>
            <p>Yönetici yetkisini yalnızca tenant yöneticileri verebilir.</p>
          </div>
          <button type="button" className="primary-button" onClick={openCreate}>
            + Kullanıcı ekle
          </button>
        </div>

        <input
          className="admin-search"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Ad veya e-posta ara..."
        />

        <div className="records-list">
          {filteredUsers.map((user) => (
            <article key={user.id} className="record-card admin-user-card">
              <span className="admin-avatar user">{initials(user.fullName)}</span>
              <div className="record-main">
                <strong>{user.fullName || "—"}</strong>
                <span>{user.email}</span>
                <span>Son giriş: {user.lastLoginAt ? formatDate(user.lastLoginAt) : "—"}</span>
              </div>
              <div className="admin-user-tags">
                <span className={`status-badge ${user.isTenantAdmin ? "status-warning" : "status-neutral"}`}>
                  {user.isTenantAdmin ? "Yönetici" : "Kullanıcı"}
                </span>
                <span className={`status-badge ${user.isActive ? "status-success" : "status-danger"}`}>
                  {user.isActive ? "Aktif" : "Pasif"}
                </span>
              </div>
              <div className="admin-user-actions">
                <button type="button" className="ghost-button dark" onClick={() => openEdit(user)}>
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
                  onClick={() => void toggleStatus(user)}
                >
                  {user.isActive ? "Pasif yap" : "Aktif yap"}
                </button>
                <button
                  type="button"
                  className="ghost-button danger"
                  onClick={() => {
                    setModalError("");
                    setConfirmTarget(user);
                  }}
                >
                  Sil
                </button>
              </div>
            </article>
          ))}
          {filteredUsers.length === 0 ? (
            <div className="empty-panel">{users.length === 0 ? "Kullanıcı bulunamadı." : "Aramayla eşleşen kullanıcı yok."}</div>
          ) : null}
        </div>
      </article>

      {modal && form ? (
        <div className="modal-backdrop">
          <div className="panel modal-panel">
            <div className="modal-header">
              <div className="panel-heading">
                <h2>{modal.mode === "create" ? "Yeni kullanıcı" : "Kullanıcı düzenle"}</h2>
              </div>
              <button type="button" className="modal-close" onClick={closeModals} aria-label="Kapat">
                X
              </button>
            </div>
            <div className="data-form">
              <div className="form-grid two-columns">
                <label>
                  <span className="field-label">Ad</span>
                  <input value={form.firstName} onChange={(event) => setForm({ ...form, firstName: event.target.value })} />
                </label>
                <label>
                  <span className="field-label">Soyad</span>
                  <input value={form.lastName} onChange={(event) => setForm({ ...form, lastName: event.target.value })} />
                </label>
              </div>
              <label>
                <span className="field-label required">E-posta</span>
                <input
                  type="email"
                  value={form.email}
                  onChange={(event) => setForm({ ...form, email: event.target.value })}
                  placeholder="kullanici@firma.com"
                />
              </label>
              {modal.mode === "create" ? (
                <label>
                  <span className="field-label required">Şifre (min. 6 karakter)</span>
                  <input
                    type="text"
                    value={form.password}
                    onChange={(event) => setForm({ ...form, password: event.target.value })}
                    placeholder="Geçici şifre"
                  />
                </label>
              ) : null}
              <label className="admin-checkbox">
                <input
                  type="checkbox"
                  checked={form.isTenantAdmin}
                  onChange={(event) => setForm({ ...form, isTenantAdmin: event.target.checked })}
                />
                <span>Tenant yöneticisi yetkisi</span>
              </label>
              {modalError ? <div className="alert error">{modalError}</div> : null}
              <div className="inline-actions">
                <button type="button" className="ghost-button dark" onClick={closeModals} disabled={busy}>
                  Vazgeç
                </button>
                <button type="button" className="primary-button" onClick={() => void submitForm()} disabled={busy}>
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

      {confirmTarget ? (
        <ConfirmDialog
          title="Kullanıcı silinsin mi?"
          description={`"${confirmTarget.fullName || confirmTarget.email}" kullanıcısı silinecek. Bu işlem geri alınamaz.`}
          error={modalError}
          busy={busy}
          onCancel={closeModals}
          onConfirm={() => void confirmDelete()}
        />
      ) : null}
    </section>
  );
}
