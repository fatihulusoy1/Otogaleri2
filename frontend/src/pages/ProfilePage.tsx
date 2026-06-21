import { useEffect, useState } from "react";
import { PageHeader } from "../components/PageHeader";
import { api } from "../lib/api";
import { formatDate, formatDateOnly } from "../lib/format";
import { useAuth } from "../state/AuthContext";
import type { Profile } from "../types";

export function ProfilePage() {
  const { session, updateSession } = useAuth();
  const [profile, setProfile] = useState<Profile | null>(null);
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");

  const [error, setError] = useState("");
  const [profileMessage, setProfileMessage] = useState("");
  const [passwordError, setPasswordError] = useState("");
  const [passwordMessage, setPasswordMessage] = useState("");
  const [savingProfile, setSavingProfile] = useState(false);
  const [savingPassword, setSavingPassword] = useState(false);
  const [twoFaBusy, setTwoFaBusy] = useState(false);
  const [twoFaMessage, setTwoFaMessage] = useState("");

  async function toggleTwoFactor() {
    if (!session || !profile) {
      return;
    }
    setTwoFaBusy(true);
    setError("");
    setTwoFaMessage("");
    try {
      const updated = await api.setTwoFactor(session.token, !profile.twoFactorEnabled);
      setProfile(updated);
      setTwoFaMessage(updated.twoFactorEnabled ? "İki adımlı doğrulama açıldı." : "İki adımlı doğrulama kapatıldı.");
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "İşlem başarısız oldu.");
    } finally {
      setTwoFaBusy(false);
    }
  }

  async function load() {
    if (!session) {
      return;
    }
    try {
      setError("");
      const data = await api.getProfile(session.token);
      setProfile(data);
      setFirstName(data.firstName);
      setLastName(data.lastName);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Profil yüklenemedi.");
    }
  }

  useEffect(() => {
    void load();
  }, [session?.token]);

  async function saveProfile() {
    if (!session) {
      return;
    }
    setSavingProfile(true);
    setError("");
    setProfileMessage("");
    try {
      const updated = await api.updateProfile(session.token, { firstName, lastName });
      setProfile(updated);
      updateSession({ userFullName: `${updated.firstName} ${updated.lastName}`.trim() });
      setProfileMessage("Profil bilgileriniz güncellendi.");
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Profil güncellenemedi.");
    } finally {
      setSavingProfile(false);
    }
  }

  async function changePassword() {
    if (!session) {
      return;
    }
    setSavingPassword(true);
    setPasswordError("");
    setPasswordMessage("");
    try {
      await api.changePassword(session.token, { currentPassword, newPassword });
      setCurrentPassword("");
      setNewPassword("");
      setPasswordMessage("Şifreniz güncellendi.");
    } catch (requestError) {
      setPasswordError(requestError instanceof Error ? requestError.message : "Şifre güncellenemedi.");
    } finally {
      setSavingPassword(false);
    }
  }

  return (
    <section className="page-stack">
      <PageHeader eyebrow="Hesabım" title="Profil" description="Hesap bilgilerinizi görüntüleyin ve güncelleyin." />

      {error ? <div className="alert error">{error}</div> : null}

      {profile ? (
        <article className="panel">
          <div className="panel-heading">
            <h2>Hesap bilgileri</h2>
            <p>{profile.email}</p>
          </div>
          <div className="record-grid">
            <div>
              <span>Ad Soyad</span>
              <strong>{`${profile.firstName} ${profile.lastName}`.trim() || "—"}</strong>
            </div>
            <div>
              <span>E-posta</span>
              <strong>{profile.email}</strong>
            </div>
            <div>
              <span>Tenant</span>
              <strong>{profile.tenantName}</strong>
            </div>
            <div>
              <span>Rol</span>
              <strong>{profile.roleLabel}</strong>
            </div>
            <div>
              <span>Üyelik tarihi</span>
              <strong>{formatDateOnly(profile.createdAt)}</strong>
            </div>
            <div>
              <span>Son giriş</span>
              <strong>{profile.lastLoginAt ? formatDate(profile.lastLoginAt) : "—"}</strong>
            </div>
            {!profile.isSuperAdmin ? (
              <div>
                <span>Abonelik bitişi</span>
                <strong>{formatDateOnly(profile.subscriptionEndDate)}</strong>
              </div>
            ) : null}
          </div>
        </article>
      ) : null}

      {profile ? (
        <article className="panel">
          <div className="panel-heading admin-panel-heading">
            <div>
              <h2>Güvenlik · İki adımlı doğrulama</h2>
              <p>
                Açıkken her girişte e-postanıza gönderilen 6 haneli kodu istemekle hesabınızı korur.
                {" "}
                Durum: <strong>{profile.twoFactorEnabled ? "Açık" : "Kapalı"}</strong>
              </p>
            </div>
            <button
              type="button"
              className={profile.twoFactorEnabled ? "ghost-button dark" : "primary-button"}
              onClick={() => void toggleTwoFactor()}
              disabled={twoFaBusy}
            >
              {twoFaBusy ? "İşleniyor..." : profile.twoFactorEnabled ? "Kapat" : "Aç"}
            </button>
          </div>
          {twoFaMessage ? <div className="alert success">{twoFaMessage}</div> : null}
        </article>
      ) : null}

      <div className="content-grid two-columns">
        <article className="panel">
          <div className="panel-heading">
            <h2>Bilgileri düzenle</h2>
            <p>Ad ve soyadınızı güncelleyebilirsiniz.</p>
          </div>
          <div className="data-form">
            <div className="form-grid two-columns">
              <label>
                <span className="field-label required">Ad</span>
                <input value={firstName} onChange={(event) => setFirstName(event.target.value)} placeholder="Ad" />
              </label>
              <label>
                <span className="field-label">Soyad</span>
                <input value={lastName} onChange={(event) => setLastName(event.target.value)} placeholder="Soyad" />
              </label>
            </div>
            {profileMessage ? <div className="alert success">{profileMessage}</div> : null}
            <button type="button" className="primary-button" onClick={() => void saveProfile()} disabled={savingProfile}>
              {savingProfile ? "Kaydediliyor..." : "Kaydet"}
            </button>
          </div>
        </article>

        <article className="panel">
          <div className="panel-heading">
            <h2>Şifre değiştir</h2>
            <p>Güvenliğiniz için güçlü bir şifre seçin (min. 6 karakter).</p>
          </div>
          <div className="data-form">
            <label>
              <span className="field-label required">Mevcut şifre</span>
              <input
                type="password"
                value={currentPassword}
                onChange={(event) => setCurrentPassword(event.target.value)}
                placeholder="••••••••"
              />
            </label>
            <label>
              <span className="field-label required">Yeni şifre</span>
              <input
                type="password"
                value={newPassword}
                onChange={(event) => setNewPassword(event.target.value)}
                placeholder="••••••••"
              />
            </label>
            {passwordError ? <div className="alert error">{passwordError}</div> : null}
            {passwordMessage ? <div className="alert success">{passwordMessage}</div> : null}
            <button
              type="button"
              className="primary-button"
              onClick={() => void changePassword()}
              disabled={savingPassword || !currentPassword || !newPassword}
            >
              {savingPassword ? "Kaydediliyor..." : "Şifreyi değiştir"}
            </button>
          </div>
        </article>
      </div>
    </section>
  );
}
