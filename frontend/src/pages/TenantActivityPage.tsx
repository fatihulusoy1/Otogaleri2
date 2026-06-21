import { useEffect, useState } from "react";
import { PageHeader } from "../components/PageHeader";
import { api } from "../lib/api";
import { formatCurrency, formatDate } from "../lib/format";
import { useAuth } from "../state/AuthContext";
import type { TenantActivity } from "../types";

export function TenantActivityPage() {
  const { session } = useAuth();
  const [activities, setActivities] = useState<TenantActivity[]>([]);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!session) {
      return;
    }
    setError("");
    api
      .tenantGetActivities(session.token)
      .then(setActivities)
      .catch((requestError) =>
        setError(requestError instanceof Error ? requestError.message : "İşlem geçmişi yüklenemedi.")
      );
  }, [session?.token]);

  return (
    <section className="page-stack">
      <PageHeader
        eyebrow="Kayıtlar"
        title="İşlem Geçmişi"
        description="Üyelik ve kullanıcı işlemleriniz satır satır burada tutulur."
      />

      {error ? <div className="alert error">{error}</div> : null}

      <article className="panel">
        <div className="panel-heading">
          <h2>İşlemler ({activities.length})</h2>
          <p>En yeni işlemler üstte gösterilir.</p>
        </div>
        <div className="records-list">
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
          {activities.length === 0 ? <div className="empty-panel">Henüz işlem kaydı yok.</div> : null}
        </div>
      </article>
    </section>
  );
}
