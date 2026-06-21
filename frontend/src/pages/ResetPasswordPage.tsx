import { useState } from "react";
import { api } from "../lib/api";

function getTokenFromUrl() {
  if (typeof window === "undefined") {
    return "";
  }
  return new URLSearchParams(window.location.search).get("token") ?? "";
}

export function ResetPasswordPage() {
  const [token] = useState(getTokenFromUrl);
  const [newPassword, setNewPassword] = useState("");
  const [error, setError] = useState("");
  const [done, setDone] = useState(false);
  const [busy, setBusy] = useState(false);

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setBusy(true);
    try {
      await api.resetPassword(token, newPassword);
      setDone(true);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Şifre sıfırlanamadı.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="auth-shell">
      <section className="auth-panel">
        <form className="panel auth-form" onSubmit={submit}>
          <div className="panel-heading">
            <h2>Şifre sıfırlama</h2>
            <p>Yeni şifrenizi belirleyin.</p>
          </div>

          {!token ? (
            <div className="alert error">Geçersiz bağlantı (token bulunamadı).</div>
          ) : done ? (
            <>
              <div className="alert success">Şifreniz güncellendi. Artık yeni şifrenizle giriş yapabilirsiniz.</div>
              <a className="primary-button" href="/" style={{ textAlign: "center" }}>
                Giriş ekranına dön
              </a>
            </>
          ) : (
            <>
              <label>
                <span>Yeni şifre (min. 6 karakter)</span>
                <input
                  type="password"
                  value={newPassword}
                  onChange={(event) => setNewPassword(event.target.value)}
                  placeholder="••••••••"
                  required
                  minLength={6}
                />
              </label>
              {error ? <div className="alert error">{error}</div> : null}
              <button type="submit" className="primary-button" disabled={busy}>
                {busy ? "İşleniyor..." : "Şifreyi sıfırla"}
              </button>
              <a className="ghost-button dark" href="/" style={{ textAlign: "center" }}>
                Vazgeç
              </a>
            </>
          )}
        </form>
      </section>
    </div>
  );
}
