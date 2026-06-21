import { useMemo, useState } from "react";
import { useAuth } from "../state/AuthContext";

type AuthMode = "login" | "register";

const initialRegisterState = {
  tenantName: "",
  firstName: "",
  lastName: "",
  email: "",
  password: ""
};

export function AuthPage() {
  const { login, register } = useAuth();
  const [mode, setMode] = useState<AuthMode>("login");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [loginForm, setLoginForm] = useState({ email: "", password: "" });
  const [registerForm, setRegisterForm] = useState(initialRegisterState);

  const modeMeta = useMemo(
    () =>
      mode === "login"
        ? {
            eyebrow: "Hoş geldiniz",
            title: "Galeri operasyonlarınızı tek panelden yönetin",
            description:
              "Stok, satınalma, satış ve maliyet görünürlüğünü modern bir yönetim ekranında takip edin."
          }
        : {
            eyebrow: "Yeni başlangıç",
            title: "Kendi tenant alanınızı birkaç adımda oluşturun",
            description:
              "Kayıt olduğunuz anda size özel tenant oluşturulur ve verileriniz diğer galerilerden tamamen ayrılır."
          },
    [mode]
  );

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setLoading(true);

    try {
      if (mode === "login") {
        await login(loginForm);
      } else {
        await register(registerForm);
      }
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : "İşlem sırasında bir hata oluştu.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth-shell">
      <section className="auth-hero">
        <span className="eyebrow">{modeMeta.eyebrow}</span>
        <h1>{modeMeta.title}</h1>
        <p>{modeMeta.description}</p>

        <div className="feature-list">
          <article>
            <strong>Tenant izolasyonu</strong>
            <span>Her galeri yalnızca kendi verisini görür.</span>
          </article>
          <article>
            <strong>Maliyet takibi</strong>
            <span>Satınalma ve masraf dahil gerçek stok maliyeti görünür.</span>
          </article>
          <article>
            <strong>Mobil uyumlu panel</strong>
            <span>Sahada hızlı kullanım için kart görünümüne otomatik geçiş yapar.</span>
          </article>
        </div>
      </section>

      <section className="auth-panel">
        <div className="auth-switch">
          <button
            type="button"
            className={mode === "login" ? "active" : ""}
            onClick={() => setMode("login")}
          >
            Giriş Yap
          </button>
          <button
            type="button"
            className={mode === "register" ? "active" : ""}
            onClick={() => setMode("register")}
          >
            Kayıt Ol
          </button>
        </div>

        <form className="panel auth-form" onSubmit={handleSubmit}>
          <div className="panel-heading">
            <h2>{mode === "login" ? "Hesabınıza giriş yapın" : "Yeni tenant oluşturun"}</h2>
            <p>
              {mode === "login"
                ? "Devam etmek için e-posta ve şifrenizi girin."
                : "Galeri bilgilerinizi doldurun, tenant alanınız otomatik oluşsun."}
            </p>
          </div>

          {mode === "register" ? (
            <>
              <label>
                <span>Galeri adı</span>
                <input
                  value={registerForm.tenantName}
                  onChange={(event) =>
                    setRegisterForm((current) => ({ ...current, tenantName: event.target.value }))
                  }
                  placeholder="Örnek Oto"
                  required
                />
              </label>
              <div className="form-grid">
                <label>
                  <span>Ad</span>
                  <input
                    value={registerForm.firstName}
                    onChange={(event) =>
                      setRegisterForm((current) => ({ ...current, firstName: event.target.value }))
                    }
                    placeholder="Fatih"
                    required
                  />
                </label>
                <label>
                  <span>Soyad</span>
                  <input
                    value={registerForm.lastName}
                    onChange={(event) =>
                      setRegisterForm((current) => ({ ...current, lastName: event.target.value }))
                    }
                    placeholder="Ulusoy"
                    required
                  />
                </label>
              </div>
            </>
          ) : null}

          <label>
            <span>E-posta</span>
            <input
              type="email"
              value={mode === "login" ? loginForm.email : registerForm.email}
              onChange={(event) =>
                mode === "login"
                  ? setLoginForm((current) => ({ ...current, email: event.target.value }))
                  : setRegisterForm((current) => ({ ...current, email: event.target.value }))
              }
              placeholder="ornek@galeri.com"
              required
            />
          </label>

          <label>
            <span>Şifre</span>
            <input
              type="password"
              value={mode === "login" ? loginForm.password : registerForm.password}
              onChange={(event) =>
                mode === "login"
                  ? setLoginForm((current) => ({ ...current, password: event.target.value }))
                  : setRegisterForm((current) => ({ ...current, password: event.target.value }))
              }
              placeholder="••••••••"
              required
            />
          </label>

          {error ? <div className="alert error">{error}</div> : null}

          <button type="submit" className="primary-button" disabled={loading}>
            {loading
              ? "İşleniyor..."
              : mode === "login"
                ? "Panele giriş yap"
                : "Tenant oluştur ve başla"}
          </button>
        </form>
      </section>
    </div>
  );
}
