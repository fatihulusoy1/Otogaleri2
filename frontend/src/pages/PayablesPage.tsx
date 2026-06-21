import { useEffect, useMemo, useState } from "react";
import { PageHeader } from "../components/PageHeader";
import { api } from "../lib/api";
import {
  formatCurrency,
  formatDateOnly,
  getFinancialDocumentTypeLabel,
  getPaymentMethodLabel,
  getReceivablePayableStatusLabel
} from "../lib/format";
import { useAuth } from "../state/AuthContext";
import type { ReceivablePayable } from "../types";

function getRemainingDueLabel(item: ReceivablePayable): string {
  if (item.status !== 1 && item.status !== 2 && item.status !== 4) {
    return "-";
  }

  if (!item.dueDate) {
    return "-";
  }

  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const due = new Date(item.dueDate);

  if (Number.isNaN(due.getTime())) {
    return "-";
  }

  due.setHours(0, 0, 0, 0);
  const diffDays = Math.ceil((due.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));

  if (diffDays === 0) {
    return "Bugün";
  }

  if (diffDays > 0) {
    return `${diffDays} gün kaldı`;
  }

  return `${Math.abs(diffDays)} gün geçti`;
}

function toStatusClass(status: number): string {
  switch (status) {
    case 3:
      return "status-success";
    case 4:
      return "status-danger";
    case 2:
      return "status-warning";
    default:
      return "status-neutral";
  }
}

function getPaidAmount(item: ReceivablePayable): number {
  return Math.max(0, item.originalAmount - item.remainingAmount);
}

function getStatusMarker(item: ReceivablePayable): string | null {
  if (item.status === 3) {
    return "✓";
  }

  if (item.status === 4) {
    return "×";
  }

  if ((item.status !== 1 && item.status !== 2) || !item.dueDate) {
    return null;
  }

  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const due = new Date(item.dueDate);

  if (Number.isNaN(due.getTime())) {
    return null;
  }

  due.setHours(0, 0, 0, 0);
  const diffDays = Math.ceil((due.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));

  return diffDays >= 0 && diffDays < 7 ? "!" : null;
}

function getTodayLocalDateValue(): string {
  const now = new Date();
  const local = new Date(now.getTime() - now.getTimezoneOffset() * 60 * 1000);
  return local.toISOString().slice(0, 10);
}

export function PayablesPage() {
  const { session } = useAuth();
  const [items, setItems] = useState<ReceivablePayable[]>([]);
  const [loading, setLoading] = useState(true);
  const [processingId, setProcessingId] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState("");
  const [error, setError] = useState("");
  const [partialTarget, setPartialTarget] = useState<ReceivablePayable | null>(null);
  const [settleTarget, setSettleTarget] = useState<ReceivablePayable | null>(null);
  const [partialAmountInput, setPartialAmountInput] = useState("");
  const [settlementDate, setSettlementDate] = useState(getTodayLocalDateValue());
  const [fullSettlementDate, setFullSettlementDate] = useState(getTodayLocalDateValue());
  const [expandedIds, setExpandedIds] = useState<string[]>([]);

  async function loadItems() {
    if (!session) {
      return;
    }

    setLoading(true);
    try {
      const response = await api.getReceivablePayables(session.token, 2);
      setItems(response);
      setError("");
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Borç listesi yüklenemedi.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadItems();
  }, [session]);

  const filteredItems = useMemo(
    () => items.filter((item) => !statusFilter || item.status.toString() === statusFilter),
    [items, statusFilter]
  );

  function toggleExpanded(id: string) {
    setExpandedIds((current) => (current.includes(id) ? current.filter((item) => item !== id) : [...current, id]));
  }

  function handleCurrencyInput(value: string) {
    const digits = value.replace(/\D/g, "");
    setPartialAmountInput(digits ? formatCurrency(Number(digits)) : "");
  }

  async function handleSettle() {
    if (!session || !settleTarget) {
      return;
    }

    try {
      setProcessingId(settleTarget.id);
      setError("");
      const settled = await api.settleReceivablePayable(session.token, settleTarget.id, new Date(fullSettlementDate).toISOString());
      setItems((current) => current.map((item) => (item.id === settleTarget.id ? settled : item)));
      setSettleTarget(null);
      setFullSettlementDate(getTodayLocalDateValue());
    } catch (settleError) {
      setError(settleError instanceof Error ? settleError.message : "Ödeme işlemi kaydedilemedi.");
    } finally {
      setProcessingId(null);
    }
  }

  async function handlePartialSettle() {
    if (!session || !partialTarget) {
      return;
    }

    const amount = Number.parseInt(partialAmountInput.replace(/\D/g, ""), 10);
    if (!amount) {
      setError("Kısmi ödeme tutarı girmeniz gerekiyor.");
      return;
    }

    if (amount > partialTarget.remainingAmount) {
      setError("Girilen tutar kalan borçtan büyük olamaz.");
      return;
    }

    try {
      setProcessingId(partialTarget.id);
      setError("");
      const updated = await api.partialSettleReceivablePayable(
        session.token,
        partialTarget.id,
        amount,
        new Date(settlementDate).toISOString()
      );
      setItems((current) => current.map((item) => (item.id === partialTarget.id ? updated : item)));
      setPartialTarget(null);
      setPartialAmountInput("");
      setSettlementDate(getTodayLocalDateValue());
    } catch (settleError) {
      setError(settleError instanceof Error ? settleError.message : "Kısmi ödeme kaydedilemedi.");
    } finally {
      setProcessingId(null);
    }
  }

  async function handleMarkOverdue(id: string) {
    if (!session) {
      return;
    }

    try {
      setProcessingId(id);
      setError("");
      const updated = await api.markReceivablePayableOverdue(session.token, id);
      setItems((current) => current.map((item) => (item.id === id ? updated : item)));
    } catch (markError) {
      setError(markError instanceof Error ? markError.message : "Kayıt gecikmiş olarak işaretlenemedi.");
    } finally {
      setProcessingId(null);
    }
  }

  async function handleReopen(id: string) {
    if (!session) {
      return;
    }

    try {
      setProcessingId(id);
      setError("");
      const updated = await api.reopenReceivablePayable(session.token, id);
      setItems((current) => current.map((item) => (item.id === id ? updated : item)));
    } catch (reopenError) {
      setError(reopenError instanceof Error ? reopenError.message : "Kayıt yeniden açılamadı.");
    } finally {
      setProcessingId(null);
    }
  }

  return (
    <section className="page-stack">
      <PageHeader
        eyebrow="Borçlar"
        title="Vadeli alış, çek ve senet borçları"
        description="Satıcı borçlarını, ödeme planlarını ve kapanan evrakları araçlar ekranındaki düzende izleyin."
      />

      <article className="panel">
        <div className="panel-heading">
          <h2>Açık ve kapanan borçlar</h2>
          <p>{loading ? "Liste hazırlanıyor..." : `${filteredItems.length} kayıt gösteriliyor.`}</p>
        </div>

        {error ? <div className="alert error">{error}</div> : null}

        <div className="filter-bar">
          <label>
            <span>Durum</span>
            <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}>
              <option value="">Tüm durumlar</option>
              <option value="1">Açık</option>
              <option value="2">Kısmen ödendi</option>
              <option value="3">Kapandı</option>
              <option value="4">Gecikmiş</option>
            </select>
          </label>
        </div>

        <div className="vehicles-table">
          <div className="vehicles-table-head debt-table-head">
            <span>Durum</span>
            <span>Taraf</span>
            <span>Tarih</span>
            <span>Tutarlar</span>
            <span>İşlem</span>
          </div>

          <div className="vehicles-table-body">
            {filteredItems.map((item) => {
              const isExpanded = expandedIds.includes(item.id);

              return (
                <article key={item.id} className={`vehicles-row debt-row debt-card ${isExpanded ? "expanded" : ""}`}>
                  <button
                    type="button"
                    className={`debt-mobile-summary ${isExpanded ? "expanded" : ""}`}
                    onClick={() => toggleExpanded(item.id)}
                    aria-expanded={isExpanded}
                  >
                    <div className="debt-mobile-summary-main">
                      <span className={`status-badge ${toStatusClass(item.status)} ${getStatusMarker(item) === "!" ? "status-badge-attention" : ""}`}>
                        {getStatusMarker(item) ? <span className="status-badge-mark">{getStatusMarker(item)}</span> : null}
                        {getReceivablePayableStatusLabel(item.status)}
                      </span>
                      <strong>{item.counterpartyName}</strong>
                      <span>{item.plate || "Araç plakası yok"}</span>
                      <span>{formatDateOnly(item.dueDate)} ({getRemainingDueLabel(item)})</span>
                    </div>
                    <span className="debt-mobile-summary-icon" aria-hidden="true">
                      {isExpanded ? "−" : "+"}
                    </span>
                  </button>

                  <div className="debt-card-body">
                    <div className="vehicles-cell vehicles-origin">
                      <span className={`status-badge ${toStatusClass(item.status)} ${getStatusMarker(item) === "!" ? "status-badge-attention" : ""}`}>
                        {getStatusMarker(item) ? <span className="status-badge-mark">{getStatusMarker(item)}</span> : null}
                        {getReceivablePayableStatusLabel(item.status)}
                      </span>
                    </div>

                    <div className="vehicles-cell vehicles-main">
                      <strong>{item.counterpartyName}</strong>
                      <span>{getPaymentMethodLabel(item.paymentMethod)} / {getFinancialDocumentTypeLabel(item.documentType)}</span>
                      <small>{item.description || "Açıklama girilmemiş."}</small>
                    </div>

                    <div className="vehicles-cell vehicles-dates">
                      <div>
                        <span>Kesim</span>
                        <strong>{formatDateOnly(item.issueDate)}</strong>
                      </div>
                      <div>
                        <span>Vade</span>
                        <strong>{formatDateOnly(item.dueDate)}</strong>
                      </div>
                      <div className="due-time-card">
                        <span>Kalan süre</span>
                        <strong>{getRemainingDueLabel(item)}</strong>
                      </div>
                    </div>

                    <div className="vehicles-cell vehicles-finance">
                      <div className="finance-stack">
                        <span>İlk tutar</span>
                        <strong>{formatCurrency(item.originalAmount)}</strong>
                        <span>Kalan</span>
                        <strong className="finance-strong">{formatCurrency(item.remainingAmount)}</strong>
                      </div>
                      <div className="finance-stack">
                        <span>Ödenen</span>
                        <div className="finance-inline-value">
                          <strong>{formatCurrency(getPaidAmount(item))}</strong>
                          {item.lastSettlementDate ? (
                            <small className="settlement-note">({formatDateOnly(item.lastSettlementDate)})</small>
                          ) : null}
                        </div>
                        <span>Belge no</span>
                        <strong>{item.documentNumber || "-"}</strong>
                      </div>
                    </div>

                    <div className="vehicles-cell vehicles-actions debt-actions">
                      <button
                        type="button"
                        className="action-chip action-chip-success"
                        disabled={item.status === 3 || processingId === item.id}
                        onClick={() => {
                          setSettleTarget(item);
                          setFullSettlementDate(getTodayLocalDateValue());
                          setError("");
                        }}
                      >
                        {processingId === item.id ? "İşleniyor..." : "Ödendi"}
                      </button>
                      <button
                        type="button"
                        className="action-chip action-chip-neutral"
                        disabled={item.status === 3 || processingId === item.id}
                        onClick={() => {
                          setPartialTarget(item);
                          setPartialAmountInput("");
                          setSettlementDate(getTodayLocalDateValue());
                          setError("");
                        }}
                      >
                        Kısmen ödendi
                      </button>
                      <button
                        type="button"
                        className="action-chip action-chip-warning"
                        disabled={item.status === 3 || item.status === 4 || processingId === item.id}
                        onClick={() => void handleMarkOverdue(item.id)}
                      >
                        Gecikti
                      </button>
                      {item.status === 3 ? (
                        <button
                          type="button"
                          className="action-chip action-chip-dark"
                          disabled={processingId === item.id}
                          onClick={() => void handleReopen(item.id)}
                        >
                          Tekrar aç
                        </button>
                      ) : null}
                    </div>
                  </div>
                </article>
              );
            })}

            {!loading && filteredItems.length === 0 ? (
              <div className="empty-panel">Bu filtrede borç kaydı bulunmuyor.</div>
            ) : null}
          </div>
        </div>
      </article>

      {settleTarget ? (
        <div className="modal-backdrop">
          <div className="panel modal-panel data-form">
            <div className="modal-header">
              <div className="panel-heading">
                <h2>Ödeme tarihini girin</h2>
                <p>{settleTarget.counterpartyName} / Kapanacak tutar {formatCurrency(settleTarget.remainingAmount)}</p>
              </div>
              <button type="button" className="modal-close" onClick={() => setSettleTarget(null)} aria-label="Kapat">
                X
              </button>
            </div>

            <label>
              <span>Ödeme tarihi</span>
              <input type="date" value={fullSettlementDate} onChange={(event) => setFullSettlementDate(event.target.value)} />
            </label>

            <div className="inline-actions">
              <button type="button" className="primary-button" disabled={processingId === settleTarget.id} onClick={() => void handleSettle()}>
                {processingId === settleTarget.id ? "Kaydediliyor..." : "Kaydet"}
              </button>
              <button type="button" className="ghost-button dark" onClick={() => setSettleTarget(null)}>
                İptal
              </button>
            </div>
          </div>
        </div>
      ) : null}

      {partialTarget ? (
        <div className="modal-backdrop">
          <div className="panel modal-panel data-form">
            <div className="modal-header">
              <div className="panel-heading">
                <h2>Kısmi ödeme girin</h2>
                <p>{partialTarget.counterpartyName} / Kalan {formatCurrency(partialTarget.remainingAmount)} / Ödenen {formatCurrency(getPaidAmount(partialTarget))}</p>
              </div>
              <button type="button" className="modal-close" onClick={() => setPartialTarget(null)} aria-label="Kapat">
                X
              </button>
            </div>

            <label>
              <span>Ödenen tutar</span>
              <input inputMode="numeric" value={partialAmountInput} placeholder="Örn. 25.000 TL" onChange={(event) => handleCurrencyInput(event.target.value)} />
            </label>

            <label>
              <span>Ödeme tarihi</span>
              <input type="date" value={settlementDate} onChange={(event) => setSettlementDate(event.target.value)} />
            </label>

            <div className="inline-actions">
              <button type="button" className="primary-button" disabled={processingId === partialTarget.id} onClick={() => void handlePartialSettle()}>
                {processingId === partialTarget.id ? "Kaydediliyor..." : "Kaydet"}
              </button>
              <button type="button" className="ghost-button dark" onClick={() => setPartialTarget(null)}>
                İptal
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </section>
  );
}
