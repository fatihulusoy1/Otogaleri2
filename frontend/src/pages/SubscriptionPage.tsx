import { useEffect, useState } from "react";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { PageHeader } from "../components/PageHeader";
import { api } from "../lib/api";
import { formatCurrency, formatDateOnly } from "../lib/format";
import { useAuth } from "../state/AuthContext";
import type { BillingCycle, SubscriptionPlanPublic, SubscriptionStatus } from "../types";

export function SubscriptionPage({ expiredGate = false }: { expiredGate?: boolean }) {
  const { session, updateSession, logout } = useAuth();
  const [status, setStatus] = useState<SubscriptionStatus | null>(null);
  const [plans, setPlans] = useState<SubscriptionPlanPublic[]>([]);
  const [selectedPlanId, setSelectedPlanId] = useState("");
  const [cycle, setCycle] = useState<BillingCycle>(1);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [busy, setBusy] = useState(false);
  const [confirmDowngrade, setConfirmDowngrade] = useState(false);

  async function load() {
    if (!session) {
      return;
    }
    try {
      setError("");
      const [statusData, planData] = await Promise.all([api.getSubscription(session.token), api.getPublicPlans()]);
      setStatus(statusData);
      setPlans(planData);
      setSelectedPlanId((current) => current || statusData.planId);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Abonelik bilgisi yüklenemedi.");
    }
  }

  useEffect(() => {
    void load();
  }, [session?.token]);

  async function performCheckout() {
    if (!session || !selectedPlanId) {
      return;
    }
    setBusy(true);
    setError("");
    setSuccess("");
    try {
      const updated = await api.checkoutSubscription(session.token, selectedPlanId, cycle);
      setStatus(updated);
      updateSession({ subscriptionExpired: updated.isExpired, subscriptionEndDate: updated.subscriptionEndDate });
      setSuccess(`Aboneliğiniz ${formatDateOnly(updated.subscriptionEndDate)} tarihine kadar uzatıldı.`);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "İşlem başarısız oldu.");
    } finally {
      setBusy(false);
    }
  }

  const selectedPlan = plans.find((plan) => plan.id === selectedPlanId) ?? null;
  const selectedPrice = selectedPlan ? (cycle === 2 ? selectedPlan.yearlyPrice : selectedPlan.monthlyPrice) : 0;

  const isDowngradeBelowUsage = Boolean(
    selectedPlan &&
      status &&
      (selectedPlan.maxUsers < status.currentUsers || selectedPlan.maxVehicles < status.currentVehicles)
  );

  function requestCheckout() {
    if (isDowngradeBelowUsage) {
      setConfirmDowngrade(true);
      return;
    }
    void performCheckout();
  }

  return (
    <section className="page-stack subscription-page">
      <PageHeader
        eyebrow="Üyelik"
        title="Abonelik ve Paket"
        description="Mevcut paketinizi görüntüleyin, yenileyin veya değiştirin."
      />

      {expiredGate ? (
        <div className="alert error">
          Aboneliğinizin süresi doldu. Panele devam edebilmek için lütfen paketinizi yenileyin.
        </div>
      ) : null}
      {error ? <div className="alert error">{error}</div> : null}
      {success ? <div className="alert success">{success}</div> : null}

      {status ? (
        <article className="panel">
          <div className="panel-heading">
            <h2>Mevcut abonelik</h2>
            <p>{status.tenantName}</p>
          </div>
          <div className="record-grid">
            <div>
              <span>Paket</span>
              <strong>{status.planName}</strong>
            </div>
            <div>
              <span>Bitiş tarihi</span>
              <strong>{formatDateOnly(status.subscriptionEndDate)}</strong>
            </div>
            <div>
              <span>Kalan</span>
              <strong>
                {status.isExpired ? `${Math.abs(status.daysRemaining)} gün geçti` : `${status.daysRemaining} gün`}
              </strong>
            </div>
            <div>
              <span>Durum</span>
              <strong>{status.isExpired ? "Süresi doldu" : "Aktif"}</strong>
            </div>
            <div>
              <span>Kullanıcı</span>
              <strong>
                {status.currentUsers} / {status.maxUsers}
              </strong>
            </div>
            <div>
              <span>Araç</span>
              <strong>
                {status.currentVehicles} / {status.maxVehicles}
              </strong>
            </div>
          </div>
        </article>
      ) : null}

      <article className="panel">
        <div className="panel-heading">
          <h2>Paket seç</h2>
          <p>Yenilemek veya paketinizi değiştirmek için bir paket ve faturalama döngüsü seçin.</p>
        </div>

        <div className="subscription-cycle">
          <button type="button" className={cycle === 1 ? "primary-button" : "ghost-button dark"} onClick={() => setCycle(1)}>
            Aylık
          </button>
          <button type="button" className={cycle === 2 ? "primary-button" : "ghost-button dark"} onClick={() => setCycle(2)}>
            Yıllık
          </button>
        </div>

        <div className="subscription-plans">
          {plans.map((plan) => {
            const isSelected = plan.id === selectedPlanId;
            const planPrice = cycle === 2 ? plan.yearlyPrice : plan.monthlyPrice;
            return (
              <button
                key={plan.id}
                type="button"
                className={`subscription-plan-card ${isSelected ? "selected" : ""}`}
                onClick={() => setSelectedPlanId(plan.id)}
                aria-pressed={isSelected}
              >
                <strong>{plan.name}</strong>
                <span className="subscription-price">
                  {formatCurrency(planPrice)}
                  <small>/{cycle === 2 ? "yıl" : "ay"}</small>
                </span>
                <span className="subscription-plan-desc">{plan.description}</span>
                <span className="subscription-plan-limits">
                  {plan.maxUsers} kullanıcı · {plan.maxVehicles} araç
                </span>
              </button>
            );
          })}
          {plans.length === 0 ? <div className="empty-panel">Paket bulunamadı.</div> : null}
        </div>

        <button type="button" className="primary-button" onClick={() => requestCheckout()} disabled={busy || !selectedPlanId}>
          {busy ? "İşleniyor..." : `Seç ve yenile (${formatCurrency(selectedPrice)})`}
        </button>
        {isDowngradeBelowUsage ? (
          <p className="alert error" style={{ marginTop: 0 }}>
            Uyarı: Bu paketin limitleri mevcut kullanımınızın altında. Devam ederseniz yeni kullanıcı/araç ekleyemezsiniz.
          </p>
        ) : null}
        <p className="subscription-note">
          Ödeme altyapısı yakında eklenecek. Şimdilik seçtiğiniz paket için abonelik süresi doğrudan uzatılır.
        </p>
      </article>

      {expiredGate ? (
        <button type="button" className="ghost-button dark" onClick={logout}>
          Oturumu kapat
        </button>
      ) : null}

      {confirmDowngrade && selectedPlan && status ? (
        <ConfirmDialog
          title="Paket düşürülüyor"
          description={`"${selectedPlan.name}" paketi ${selectedPlan.maxUsers} kullanıcı / ${selectedPlan.maxVehicles} araç limitine sahip; şu an ${status.currentUsers} kullanıcı ve ${status.currentVehicles} aracınız var. Mevcut kayıtlarınız korunur ama limitin üzerinde olduğunuz sürece yeni ekleyemezsiniz. Devam edilsin mi?`}
          confirmLabel="Devam et"
          cancelLabel="Vazgeç"
          busy={busy}
          onConfirm={() => {
            setConfirmDowngrade(false);
            void performCheckout();
          }}
          onCancel={() => setConfirmDowngrade(false)}
        />
      ) : null}
    </section>
  );
}
