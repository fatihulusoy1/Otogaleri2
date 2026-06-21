import { useEffect, useState } from "react";
import { PageHeader } from "../components/PageHeader";
import { StatCard } from "../components/StatCard";
import { api } from "../lib/api";
import { formatCurrency, formatPercent } from "../lib/format";
import { useAuth } from "../state/AuthContext";
import type { DashboardSummary } from "../types";

export function DashboardPage() {
  const { session } = useAuth();
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!session) {
      return;
    }

    setLoading(true);
    api.getDashboard(session.token)
      .then(setSummary)
      .catch((requestError) =>
        setError(requestError instanceof Error ? requestError.message : "Dashboard yüklenemedi.")
      )
      .finally(() => setLoading(false));
  }, [session]);

  if (loading) {
    return <div className="panel empty-panel">Dashboard hazırlanıyor...</div>;
  }

  if (error || !summary) {
    return <div className="panel empty-panel">{error || "Dashboard verisi bulunamadı."}</div>;
  }

  return (
    <section className="page-stack">
      <PageHeader
        eyebrow="Dashboard"
        title="Galeri performans özeti"
        description="Satınalma maliyetleri, masraf etkisi, satış geliri ve stoktaki sermaye durumunu tek bakışta takip edin."
      />

      <div className="hero-card">
        <div>
          <span className="eyebrow">Aylık operasyon akışı</span>
          <h2>{formatCurrency(summary.currentMonthSalesRevenue)} ciro</h2>
          <p>Bu ayki satış geliri, satınalma ve masraf yüküne karşı anlık karlılık görünümü.</p>
        </div>
        <div className="hero-metrics">
          <article>
            <span>Bu ay satınalma</span>
            <strong>{formatCurrency(summary.currentMonthPurchaseCost)}</strong>
          </article>
          <article>
            <span>Bu ay masraf</span>
            <strong>{formatCurrency(summary.currentMonthExpenseCost)}</strong>
          </article>
          <article>
            <span>Brüt kar marjı</span>
            <strong>{formatPercent(summary.grossProfitMargin)}</strong>
          </article>
        </div>
      </div>

      <div className="stats-grid">
        <StatCard
          label="Toplam satınalma"
          value={formatCurrency(summary.totalPurchaseCost)}
          meta={`${summary.totalVehicles} araç işlemde`}
          tone="primary"
        />
        <StatCard
          label="Araç masrafları"
          value={formatCurrency(summary.totalVehicleExpenseCost)}
          meta="Servis, ekspertiz, bakım ve diğer giderler"
          tone="danger"
        />
        <StatCard
          label="Satış geliri"
          value={formatCurrency(summary.totalSalesRevenue)}
          meta={`${summary.soldVehicles} araç satıldı`}
          tone="success"
        />
        <StatCard
          label="Brüt kar"
          value={formatCurrency(summary.grossProfit)}
          meta={`${formatPercent(summary.grossProfitMargin)} marj`}
          tone={summary.grossProfit >= 0 ? "success" : "danger"}
        />
      </div>

      <div className="content-grid two-columns">
        <article className="panel">
          <div className="panel-heading">
            <h2>Stok sermayesi</h2>
            <p>Stoktaki araçlara bağlanan net maliyet görünümü.</p>
          </div>
          <div className="metric-list">
            <div>
              <span>Stok adedi</span>
              <strong>{summary.inStockVehicles}</strong>
            </div>
            <div>
              <span>Alış maliyeti</span>
              <strong>{formatCurrency(summary.currentStockPurchaseCost)}</strong>
            </div>
            <div>
              <span>Ek masraflar</span>
              <strong>{formatCurrency(summary.currentStockExpenseCost)}</strong>
            </div>
            <div>
              <span>Toplam stok maliyeti</span>
              <strong>{formatCurrency(summary.currentStockTotalCost)}</strong>
            </div>
          </div>
        </article>

        <article className="panel">
          <div className="panel-heading">
            <h2>Karlılık dengesi</h2>
            <p>Satışların toplu maliyet ve gelir karşılaştırması.</p>
          </div>
          <div className="progress-card">
            <div>
              <span>Toplam maliyet</span>
              <strong>{formatCurrency(summary.totalCostWithExpenses)}</strong>
            </div>
            <div className="progress-bar">
              <span style={{ width: `${Math.min(Math.max(summary.grossProfitMargin, 8), 100)}%` }} />
            </div>
            <small>Marj pozitif kaldıkça satış performansı daha sağlıklı görünür.</small>
          </div>
        </article>
      </div>
    </section>
  );
}
