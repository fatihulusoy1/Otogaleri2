import { useEffect, useMemo, useState } from "react";
import { api } from "../lib/api";
import { formatCurrency } from "../lib/format";
import { useAuth } from "../state/AuthContext";
import type { BillingCycle, SubscriptionPlanPublic } from "../types";

type AuthMode = "login" | "register";

const initialRegisterState = {
  tenantName: "",
  firstName: "",
  lastName: "",
  email: "",
  password: "",
  subscriptionPlanId: "",
  billingCycle: 1 as BillingCycle
};

export function AuthPage() {
  const { login, verifyTwoFactor, register } = useAuth();
  const [mode, setMode] = useState<AuthMode>("login");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [loginForm, setLoginForm] = useState({ email: "", password: "" });
  const [registerForm, setRegisterForm] = useState(initialRegisterState);
  const [plans, setPlans] = useState<SubscriptionPlanPublic[]>([]);
  const [twoFactorStep, setTwoFactorStep] = useState(false);
  const [twoFactorCode, setTwoFactorCode] = useState("");
  const [emailEnabled, setEmailEnabled] = useState(false);
  const [forgotStep, setForgotStep] = useState(false);
  const [forgotEmail, setForgotEmail] = useState("");
  const [forgotDone, setForgotDone] = useState(false);

  useEffect(() => {
    void api
      .getEmailEnabled()
      .then((data) => setEmailEnabled(data.enabled))
      .catch(() => undefined);
  }, []);

  async function handleForgot(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setLoading(true);
    try {
      await api.forgotPassword(forgotEmail.trim());
      setForgotDone(true);
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : "İşlem başarısız oldu.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void api
      .getPublicPlans()
      .then((data) => {
        setPlans(data);
        setRegisterForm((current) =>
          current.subscriptionPlanId ? current : { ...current, subscriptionPlanId: data[0]?.id ?? "" }
        );
      })
      .catch(() => undefined);
  }, []);

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
        const response = await login(loginForm);
        if (response.requiresTwoFactor) {
          setTwoFactorStep(true);
        }
      } else {
        await register(registerForm);
      }
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : "İşlem sırasında bir hata oluştu.");
    } finally {
      setLoading(false);
    }
  }

  async function handleVerify(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setLoading(true);
    try {
      await verifyTwoFactor(loginForm.email, twoFactorCode.trim());
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : "Doğrulama başarısız oldu.");
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
        {twoFactorStep ? (
          <form className="panel auth-form" onSubmit={handleVerify}>
            <div className="panel-heading">
              <h2>İki adımlı doğrulama</h2>
              <p>{loginForm.email} adresine gönderilen 6 haneli kodu girin.</p>
            </div>
            <label>
              <span>Doğrulama kodu</span>
              <input
                value={twoFactorCode}
                onChange={(event) => setTwoFactorCode(event.target.value)}
                placeholder="______"
                inputMode="numeric"
                maxLength={6}
                autoFocus
                required
              />
            </label>
            {error ? <div className="alert error">{error}</div> : null}
            <button type="submit" className="primary-button" disabled={loading}>
              {loading ? "Doğrulanıyor..." : "Doğrula ve giriş yap"}
            </button>
            <button
              type="button"
              className="ghost-button dark"
              onClick={() => {
                setTwoFactorStep(false);
                setTwoFactorCode("");
                setError("");
              }}
            >
              Geri dön
            </button>
          </form>
        ) : forgotStep ? (
          <form className="panel auth-form" onSubmit={handleForgot}>
            <div className="panel-heading">
              <h2>Şifremi unuttum</h2>
              <p>Hesap e-postanızı girin; sıfırlama bağlantısı gönderelim.</p>
            </div>
            {forgotDone ? (
              <div className="alert success">
                Eğer bu e-posta kayıtlıysa, şifre sıfırlama bağlantısı gönderildi. Lütfen e-postanızı kontrol edin.
              </div>
            ) : (
              <label>
                <span>E-posta</span>
                <input
                  type="email"
                  value={forgotEmail}
                  onChange={(event) => setForgotEmail(event.target.value)}
                  placeholder="ornek@galeri.com"
                  required
                  autoFocus
                />
              </label>
            )}
            {error ? <div className="alert error">{error}</div> : null}
            {!forgotDone ? (
              <button type="submit" className="primary-button" disabled={loading}>
                {loading ? "Gönderiliyor..." : "Sıfırlama bağlantısı gönder"}
              </button>
            ) : null}
            <button
              type="button"
              className="ghost-button dark"
              onClick={() => {
                setForgotStep(false);
                setForgotDone(false);
                setForgotEmail("");
                setError("");
              }}
            >
              Girişe dön
            </button>
          </form>
        ) : (
        <>
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

              <div className="auth-plan-block">
                <span className="field-label">Paket seçimi</span>
                <div className="subscription-cycle">
                  <button
                    type="button"
                    className={registerForm.billingCycle === 1 ? "primary-button" : "ghost-button dark"}
                    onClick={() => setRegisterForm((current) => ({ ...current, billingCycle: 1 }))}
                  >
                    Aylık
                  </button>
                  <button
                    type="button"
                    className={registerForm.billingCycle === 2 ? "primary-button" : "ghost-button dark"}
                    onClick={() => setRegisterForm((current) => ({ ...current, billingCycle: 2 }))}
                  >
                    Yıllık
                  </button>
                </div>
                <div className="subscription-plans">
                  {plans.map((plan) => {
                    const isSelected = plan.id === registerForm.subscriptionPlanId;
                    const price = registerForm.billingCycle === 2 ? plan.yearlyPrice : plan.monthlyPrice;
                    return (
                      <button
                        key={plan.id}
                        type="button"
                        className={`subscription-plan-card ${isSelected ? "selected" : ""}`}
                        onClick={() => setRegisterForm((current) => ({ ...current, subscriptionPlanId: plan.id }))}
                        aria-pressed={isSelected}
                      >
                        <strong>{plan.name}</strong>
                        <span className="subscription-price">
                          {formatCurrency(price)}
                          <small>/{registerForm.billingCycle === 2 ? "yıl" : "ay"}</small>
                        </span>
                        <span className="subscription-plan-limits">
                          {plan.maxUsers} kullanıcı · {plan.maxVehicles} araç
                        </span>
                      </button>
                    );
                  })}
                </div>
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

          {mode === "login" && emailEnabled ? (
            <button
              type="button"
              className="auth-text-link"
              onClick={() => {
                setForgotStep(true);
                setForgotEmail(loginForm.email);
                setError("");
              }}
            >
              Şifremi unuttum
            </button>
          ) : null}

          <button type="submit" className="primary-button" disabled={loading}>
            {loading
              ? "İşleniyor..."
              : mode === "login"
                ? "Panele giriş yap"
                : "Tenant oluştur ve başla"}
          </button>
        </form>
        </>
        )}
      </section>
    </div>
  );
}
