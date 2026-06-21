import { useEffect, useMemo, useState } from "react";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { SearchableSelect } from "../components/SearchableSelect";
import { PageHeader } from "../components/PageHeader";
import { api } from "../lib/api";
import { formatCurrency, formatDateOnly, getPaymentMethodLabel, getVehicleStatusLabel, toDateTimeLocalValue, toIsoFromLocalValue } from "../lib/format";
import { useAuth } from "../state/AuthContext";
import type { CreatePurchaseRequest, PurchaseRecord, VehicleLookups } from "../types";

const createInitialForm = (): CreatePurchaseRequest => ({
  plate: "",
  segmentId: "",
  brandId: "",
  modelId: "",
  year: new Date().getFullYear(),
  color: "",
  engineNumber: "",
  chassisNumber: "",
  purchaseDate: new Date().toISOString(),
  purchasePrice: 0,
  paymentMethod: 1,
  notaryRegistryNumber: null,
  counterpartyName: null,
  dueDate: null,
  documentNumber: null,
  installmentCount: null,
  installmentIntervalMonths: null,
  tradePlate: null,
  tradeAmount: null,
  targetSalePrice: null,
  description: ""
});

function toDateOnlyInputValue(value: string | null): string {
  if (!value) {
    return "";
  }

  const date = new Date(value);
  const pad = (part: number) => part.toString().padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

function toIsoDateValue(value: string): string | null {
  if (!value) {
    return null;
  }

  const date = new Date(`${value}T12:00:00`);
  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}

function getDateParts(value: string) {
  const date = new Date(value);
  return {
    year: date.getFullYear().toString(),
    month: (date.getMonth() + 1).toString().padStart(2, "0")
  };
}

function isPurchaseFinanceMethod(paymentMethod: number) {
  return paymentMethod === 5 || paymentMethod === 6 || paymentMethod === 7;
}

function isTradePaymentMethod(paymentMethod: number) {
  return paymentMethod === 4;
}

export function PurchasesPage() {
  const { session } = useAuth();
  const [purchases, setPurchases] = useState<PurchaseRecord[]>([]);
  const [lookups, setLookups] = useState<VehicleLookups>({ segments: [], brands: [], models: [] });
  const [form, setForm] = useState<CreatePurchaseRequest>(createInitialForm);
  const [purchasePriceInput, setPurchasePriceInput] = useState("");
  const [tradeAmountInput, setTradeAmountInput] = useState("");
  const [targetSalePriceInput, setTargetSalePriceInput] = useState("");
  const [editingVehicleId, setEditingVehicleId] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedYear, setSelectedYear] = useState("");
  const [selectedMonth, setSelectedMonth] = useState("");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [deletingVehicleId, setDeletingVehicleId] = useState<string | null>(null);
  const [pendingDeletePurchase, setPendingDeletePurchase] = useState<PurchaseRecord | null>(null);
  const [error, setError] = useState("");
  const [expandedIds, setExpandedIds] = useState<string[]>([]);

  async function loadPurchases() {
    if (!session) {
      return;
    }

    setLoading(true);
    try {
      const [purchaseData, lookupData] = await Promise.all([
        api.getPurchases(session.token),
        api.getVehicleLookups(session.token)
      ]);
      setPurchases(purchaseData);
      setLookups(lookupData);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Satınalma verisi alınamadı.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadPurchases();
  }, [session]);

  const filteredModels = lookups.models
    .filter((model) => (!form.brandId || model.brandId === form.brandId))
    .filter((model) => (!form.segmentId || !model.segmentId || model.segmentId === form.segmentId))
    .map((model) => ({ id: model.id, name: model.name }));

  const yearOptions = useMemo(
    () =>
      [...new Set(purchases.map((purchase) => getDateParts(purchase.purchasedAt).year))]
        .sort((left, right) => Number(right) - Number(left)),
    [purchases]
  );

  const filteredPurchases = useMemo(
    () =>
      purchases.filter((purchase) => {
        const { year, month } = getDateParts(purchase.purchasedAt);
        if (selectedYear && year !== selectedYear) {
          return false;
        }
        if (selectedMonth && month !== selectedMonth) {
          return false;
        }
        return true;
      }),
    [purchases, selectedMonth, selectedYear]
  );

  function resetForm() {
    setForm(createInitialForm());
    setPurchasePriceInput("");
    setTradeAmountInput("");
    setTargetSalePriceInput("");
    setEditingVehicleId(null);
    setError("");
  }

  function openCreateModal() {
    resetForm();
    setIsModalOpen(true);
  }

  function closeModal() {
    setIsModalOpen(false);
    resetForm();
  }

  function clearFilters() {
    setSelectedYear("");
    setSelectedMonth("");
  }

  function toggleExpanded(id: string) {
    setExpandedIds((current) => (current.includes(id) ? current.filter((item) => item !== id) : [...current, id]));
  }

  function handleCurrencyInput(value: string, setter: (value: string) => void) {
    const digits = value.replace(/\D/g, "");
    setter(digits ? formatCurrency(Number(digits)) : "");
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!session) {
      return;
    }

    if (!form.segmentId || !form.brandId || !form.modelId) {
      setError("Segment, marka ve model seçimi zorunludur.");
      return;
    }

    setSaving(true);
    setError("");

    try {
      const numericPurchasePrice = Number.parseInt(purchasePriceInput.replace(/\D/g, ""), 10);
      const numericTradeAmount = Number.parseInt(tradeAmountInput.replace(/\D/g, ""), 10);
      const numericTargetSalePrice = Number.parseInt(targetSalePriceInput.replace(/\D/g, ""), 10);
      const requiresFinanceInfo = isPurchaseFinanceMethod(form.paymentMethod);

      if (!numericPurchasePrice) {
        setError("Alış fiyatı girmeniz gerekiyor.");
        setSaving(false);
        return;
      }

      const hasTradeInfo = isTradePaymentMethod(form.paymentMethod);
      if (hasTradeInfo && (!form.tradePlate?.trim() || !numericTradeAmount)) {
        setError("Takas bilgisi giriyorsanız plaka ve takas bedelini birlikte doldurun.");
        setSaving(false);
        return;
      }

      if (requiresFinanceInfo && (!form.counterpartyName?.trim() || !form.dueDate)) {
        setError("Çek, senet veya vadeli alışta taraf ve vade tarihi zorunludur.");
        setSaving(false);
        return;
      }

      if (form.paymentMethod === 6 && (!form.installmentCount || !form.installmentIntervalMonths)) {
        setError("Senet alışında taksit sayısı ve taksit aralığı zorunludur.");
        setSaving(false);
        return;
      }

      const payload = {
        ...form,
        description: form.description || "",
        purchasePrice: numericPurchasePrice,
        notaryRegistryNumber: form.notaryRegistryNumber?.trim() || null,
        counterpartyName: requiresFinanceInfo ? form.counterpartyName?.trim() ?? null : null,
        dueDate: requiresFinanceInfo ? form.dueDate : null,
        documentNumber: requiresFinanceInfo ? form.documentNumber?.trim() ?? null : null,
        installmentCount: form.paymentMethod === 6 ? form.installmentCount : null,
        installmentIntervalMonths: form.paymentMethod === 6 ? form.installmentIntervalMonths : null,
        tradePlate: hasTradeInfo ? form.tradePlate?.trim().toUpperCase() ?? null : null,
        tradeAmount: hasTradeInfo ? numericTradeAmount || null : null,
        targetSalePrice: numericTargetSalePrice || null,
        year: Number(form.year)
      };

      if (editingVehicleId) {
        await api.updatePurchase(session.token, editingVehicleId, payload);
      } else {
        await api.createPurchase(session.token, payload);
      }

      closeModal();
      await loadPurchases();
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : "Satınalma kaydedilemedi.");
    } finally {
      setSaving(false);
    }
  }

  function handleEdit(purchase: PurchaseRecord) {
    setEditingVehicleId(purchase.vehicleId);
    setForm({
      plate: purchase.plate,
      segmentId: purchase.segmentId ?? "",
      brandId: purchase.brandId ?? "",
      modelId: purchase.modelId ?? "",
      year: purchase.year,
      color: purchase.color,
      engineNumber: purchase.engineNumber,
      chassisNumber: purchase.chassisNumber,
      purchaseDate: purchase.purchasedAt,
      purchasePrice: purchase.purchasePrice,
      paymentMethod: purchase.paymentMethod,
      notaryRegistryNumber: purchase.notaryRegistryNumber,
      counterpartyName: purchase.counterpartyName,
      dueDate: purchase.dueDate,
      documentNumber: purchase.documentNumber,
      installmentCount: purchase.installmentCount,
      installmentIntervalMonths: purchase.installmentIntervalMonths,
      tradePlate: purchase.tradePlate,
      tradeAmount: purchase.tradeAmount,
      targetSalePrice: purchase.targetSalePrice,
      description: purchase.description ?? ""
    });
    setPurchasePriceInput(formatCurrency(purchase.purchasePrice));
    setTradeAmountInput(purchase.tradeAmount ? formatCurrency(purchase.tradeAmount) : "");
    setTargetSalePriceInput(purchase.targetSalePrice ? formatCurrency(purchase.targetSalePrice) : "");
    setError("");
    setIsModalOpen(true);
  }

  function requestDeletePurchase(purchase: PurchaseRecord) {
    setPendingDeletePurchase(purchase);
    setError("");
  }

  async function handleDeletePurchase() {
    if (!session || !pendingDeletePurchase) {
      return;
    }

    try {
      setDeletingVehicleId(pendingDeletePurchase.vehicleId);
      setError("");
      await api.deletePurchase(session.token, pendingDeletePurchase.vehicleId);
      setPendingDeletePurchase(null);
      await loadPurchases();
    } catch (deleteError) {
      setError(deleteError instanceof Error ? deleteError.message : "Satınalma silinemedi.");
    } finally {
      setDeletingVehicleId(null);
    }
  }

  return (
    <section className="page-stack">
      <PageHeader
        eyebrow="Satınalma"
        title="Yeni araç alımlarını kaydedin"
        description="Araç kartını oluşturun, alış maliyetini işleyin ve hedef satış fiyatını belirleyin."
        actions={
          <button type="button" className="primary-button" onClick={openCreateModal}>
            Yeni satınalma
          </button>
        }
      />

      <article className="panel">
        <div className="records-sticky-stack">
          <div className="panel-heading records-heading">
            <h2>Son satınalmalar</h2>
            <p>{loading ? "Liste hazırlanıyor..." : `${filteredPurchases.length} kayıt gösteriliyor.`}</p>
          </div>

          {error && !isModalOpen && !pendingDeletePurchase ? <div className="alert error">{error}</div> : null}

          <div className="filter-bar records-filter-bar">
            <label>
              <span>Yıl</span>
              <select value={selectedYear} onChange={(event) => setSelectedYear(event.target.value)}>
                <option value="">Tüm yıllar</option>
                {yearOptions.map((year) => (
                  <option key={year} value={year}>
                    {year}
                  </option>
                ))}
              </select>
            </label>
            <label>
              <span>Ay</span>
              <select value={selectedMonth} onChange={(event) => setSelectedMonth(event.target.value)}>
                <option value="">Tüm aylar</option>
                {Array.from({ length: 12 }, (_, index) => {
                  const month = (index + 1).toString().padStart(2, "0");
                  return (
                    <option key={month} value={month}>
                      {month}
                    </option>
                  );
                })}
              </select>
            </label>
            <div className="filter-actions">
              <button type="button" className="ghost-button dark" onClick={clearFilters}>
                Filtreyi temizle
              </button>
            </div>
          </div>

          <div className="records-table-head purchase-table-head records-table-head-sticky">
            <span>Araç</span>
            <span>Kimlik</span>
            <span>Alış</span>
            <span>Çek / Senet</span>
            <span>Takas</span>
            <span>Durum</span>
            <span>İşlem</span>
          </div>
        </div>

        <div className="records-table">
          <div className="records-table-body">
            {filteredPurchases.map((purchase) => {
              const isExpanded = expandedIds.includes(purchase.vehicleId);

              return (
                <article key={purchase.vehicleId} className={`records-row purchase-row record-card-shell ${isExpanded ? "expanded" : ""}`}>
                  <button
                    type="button"
                    className={`record-mobile-summary ${isExpanded ? "expanded" : ""}`}
                    onClick={() => toggleExpanded(purchase.vehicleId)}
                    aria-expanded={isExpanded}
                  >
                    <div className="record-mobile-summary-main">
                      <span className="status-badge status-neutral">{getVehicleStatusLabel(purchase.status)}</span>
                      <strong>{purchase.plate}</strong>
                      <span>{[purchase.segment, purchase.brand, purchase.model].filter(Boolean).join(" / ")}</span>
                      <span>
                        {formatDateOnly(purchase.purchasedAt)} · {formatCurrency(purchase.purchasePrice)}
                      </span>
                    </div>
                    <span className="record-mobile-summary-icon" aria-hidden="true">
                      {isExpanded ? "−" : "+"}
                    </span>
                  </button>

                  <div className="records-card-body">
                    <div className="records-cell records-main">
                      <strong>{purchase.plate}</strong>
                      <span>{[purchase.segment, purchase.brand, purchase.model].filter(Boolean).join(" / ")}</span>
                      <small>{`${purchase.year} / ${purchase.color}`}</small>
                      <small>{purchase.description || "Açıklama girilmemiş."}</small>
                    </div>

                    <div className="records-cell records-finance">
                      <div className="finance-stack">
                        <span>Noter yevmiye no</span>
                        <strong>{purchase.notaryRegistryNumber || "-"}</strong>
                        <span>Motor no</span>
                        <strong>{purchase.engineNumber}</strong>
                        <span>Şasi no</span>
                        <strong>{purchase.chassisNumber}</strong>
                      </div>
                    </div>

                    <div className="records-cell records-finance">
                      <div className="finance-stack">
                        <span>Alış tarihi</span>
                        <strong>{formatDateOnly(purchase.purchasedAt)}</strong>
                        <span>Toplam alış tutarı</span>
                        <strong className="finance-strong">{formatCurrency(purchase.purchasePrice)}</strong>
                        <span>Ödeme metodu</span>
                        <strong>{getPaymentMethodLabel(purchase.paymentMethod)}</strong>
                        <span>Hedef satış</span>
                        <strong>{purchase.targetSalePrice ? formatCurrency(purchase.targetSalePrice) : "-"}</strong>
                      </div>
                    </div>

                    <div className="records-cell records-finance">
                      {isPurchaseFinanceMethod(purchase.paymentMethod) ? (
                        <div className="finance-stack">
                          <span>Taraf</span>
                          <strong>{purchase.counterpartyName || "-"}</strong>
                          <span>Belge numarası</span>
                          <strong>{purchase.documentNumber || "-"}</strong>
                          <span>Belge vadesi</span>
                          <strong>{purchase.dueDate ? formatDateOnly(purchase.dueDate) : "-"}</strong>
                        </div>
                      ) : (
                        <div className="finance-stack finance-stack-muted">
                          <span>Bu işlem için çek / senet bilgisi yok</span>
                        </div>
                      )}
                    </div>

                    <div className="records-cell records-finance">
                      {isTradePaymentMethod(purchase.paymentMethod) ? (
                        <div className="finance-stack">
                          <span>Takas plakası</span>
                          <strong>{purchase.tradePlate ?? "-"}</strong>
                          <span>Takas bedeli</span>
                          <strong>{purchase.tradeAmount ? formatCurrency(purchase.tradeAmount) : "-"}</strong>
                        </div>
                      ) : (
                        <div className="finance-stack finance-stack-muted">
                          <span>Bu işlem için takas bilgisi yok</span>
                        </div>
                      )}
                    </div>

                    <div className="records-cell">
                      <div className="status-inline">{getVehicleStatusLabel(purchase.status)}</div>
                    </div>

                    <div className="records-cell records-actions">
                      <button type="button" className="ghost-button dark" onClick={() => handleEdit(purchase)}>
                        Düzenle
                      </button>
                      <button type="button" className="ghost-button danger" onClick={() => requestDeletePurchase(purchase)}>
                        Sil
                      </button>
                    </div>
                  </div>
                </article>
              );
            })}

            {!loading && filteredPurchases.length === 0 ? (
              <div className="empty-panel">Seçilen filtrede satınalma kaydı bulunmuyor.</div>
            ) : null}
          </div>
        </div>
      </article>

      {isModalOpen ? (
        <div className="modal-backdrop">
          <form className="panel modal-panel data-form" onSubmit={handleSubmit}>
            <div className="modal-header">
              <div className="panel-heading">
                <h2>{editingVehicleId ? "Satınalma düzenle" : "Yeni satınalma"}</h2>
              </div>
              <button type="button" className="modal-close" onClick={closeModal} aria-label="Kapat">
                X
              </button>
            </div>

            <div className="form-grid">
              <label>
                <span className="field-label required">Plaka</span>
                <input value={form.plate} onChange={(event) => setForm((current) => ({ ...current, plate: event.target.value }))} required />
              </label>
              <SearchableSelect
                label="Segment"
                value={form.segmentId}
                options={lookups.segments}
                placeholder="Segment seçin"
                onChange={(segmentId) => setForm((current) => ({ ...current, segmentId, modelId: "" }))}
                required
              />
              <SearchableSelect
                label="Marka"
                value={form.brandId}
                options={lookups.brands}
                placeholder="Marka seçin"
                onChange={(brandId) => setForm((current) => ({ ...current, brandId, modelId: "" }))}
                required
              />
              <SearchableSelect
                label="Model"
                value={form.modelId}
                options={filteredModels}
                placeholder="Model seçin"
                onChange={(modelId) => setForm((current) => ({ ...current, modelId }))}
                disabled={!form.brandId}
                required
              />
              <label>
                <span className="field-label required">Yıl</span>
                <input type="number" value={form.year} onChange={(event) => setForm((current) => ({ ...current, year: Number(event.target.value) }))} required />
              </label>
              <label>
                <span className="field-label required">Renk</span>
                <input value={form.color} onChange={(event) => setForm((current) => ({ ...current, color: event.target.value }))} required />
              </label>
              <label>
                <span className="field-label required">Satınalma tarihi</span>
                <input
                  type="datetime-local"
                  value={toDateTimeLocalValue(form.purchaseDate)}
                  onChange={(event) => setForm((current) => ({ ...current, purchaseDate: toIsoFromLocalValue(event.target.value) }))}
                  required
                />
              </label>
              <label>
                <span className="field-label required">Alış fiyatı</span>
                <input inputMode="numeric" value={purchasePriceInput} placeholder="Örn. 850.000 TL" onChange={(event) => handleCurrencyInput(event.target.value, setPurchasePriceInput)} required />
              </label>
              <label>
                <span className="field-label required">Ödeme yöntemi</span>
                <select
                  value={form.paymentMethod}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      paymentMethod: Number(event.target.value) as 1 | 2 | 3 | 4 | 5 | 6 | 7,
                      counterpartyName: Number(event.target.value) >= 5 ? current.counterpartyName : null,
                      dueDate: Number(event.target.value) >= 5 ? current.dueDate : null,
                      documentNumber: Number(event.target.value) >= 5 ? current.documentNumber : null,
                      installmentCount: Number(event.target.value) === 6 ? current.installmentCount : null,
                      installmentIntervalMonths: Number(event.target.value) === 6 ? current.installmentIntervalMonths : null,
                      tradePlate: Number(event.target.value) === 4 ? current.tradePlate : null,
                      tradeAmount: Number(event.target.value) === 4 ? current.tradeAmount : null
                    }))
                  }
                >
                  <option value={1}>Nakit</option>
                  <option value={2}>Havale</option>
                  <option value={3}>Kredi Kartı</option>
                  <option value={4}>Takas</option>
                  <option value={5}>Çek</option>
                  <option value={6}>Senet</option>
                  <option value={7}>Vadeli</option>
                </select>
              </label>
              <label>
                <span>Noter yevmiye no</span>
                <input
                  value={form.notaryRegistryNumber ?? ""}
                  placeholder="Örn. 2026/1458"
                  onChange={(event) => setForm((current) => ({ ...current, notaryRegistryNumber: event.target.value || null }))}
                />
              </label>
              <label>
                <span>Hedef satış</span>
                <input inputMode="numeric" value={targetSalePriceInput} placeholder="Örn. 1.050.000 TL" onChange={(event) => handleCurrencyInput(event.target.value, setTargetSalePriceInput)} />
              </label>

              {form.paymentMethod === 4 ? (
                <>
                  <label>
                    <span className="field-label required">Takas aracının plakası</span>
                    <input value={form.tradePlate ?? ""} placeholder="Örn. 34ABC657" onChange={(event) => setForm((current) => ({ ...current, tradePlate: event.target.value || null }))} />
                  </label>
                  <label>
                    <span className="field-label required">Takas bedeli</span>
                    <input inputMode="numeric" value={tradeAmountInput} placeholder="Örn. 110.000 TL" onChange={(event) => handleCurrencyInput(event.target.value, setTradeAmountInput)} />
                  </label>
                </>
              ) : null}

              {isPurchaseFinanceMethod(form.paymentMethod) ? (
                <>
                  <label>
                    <span className="field-label required">Taraf bilgisi</span>
                    <input value={form.counterpartyName ?? ""} placeholder="Örn. Mehmet Yılmaz / Auto Expert" onChange={(event) => setForm((current) => ({ ...current, counterpartyName: event.target.value || null }))} />
                  </label>
                  <label>
                    <span className="field-label required">{form.paymentMethod === 6 ? "İlk taksit tarihi" : "Vade tarihi"}</span>
                    <input type="date" value={toDateOnlyInputValue(form.dueDate)} onChange={(event) => setForm((current) => ({ ...current, dueDate: toIsoDateValue(event.target.value) }))} />
                  </label>
                  <label>
                    <span>Belge numarası</span>
                    <input value={form.documentNumber ?? ""} placeholder="Örn. CEK-2026-001" onChange={(event) => setForm((current) => ({ ...current, documentNumber: event.target.value || null }))} />
                  </label>
                  {form.paymentMethod === 6 ? (
                    <>
                      <label>
                        <span className="field-label required">Taksit aralığı (ay)</span>
                        <input
                          type="number"
                          min={1}
                          value={form.installmentIntervalMonths ?? ""}
                          placeholder="Örn. 1"
                          onChange={(event) => setForm((current) => ({ ...current, installmentIntervalMonths: event.target.value ? Number(event.target.value) : null }))}
                        />
                      </label>
                      <label>
                        <span className="field-label required">Taksit sayısı</span>
                        <input
                          type="number"
                          min={1}
                          value={form.installmentCount ?? ""}
                          placeholder="Örn. 4"
                          onChange={(event) => setForm((current) => ({ ...current, installmentCount: event.target.value ? Number(event.target.value) : null }))}
                        />
                      </label>
                    </>
                  ) : null}
                </>
              ) : null}

              <label>
                <span className="field-label required">Motor no</span>
                <input value={form.engineNumber} onChange={(event) => setForm((current) => ({ ...current, engineNumber: event.target.value }))} required />
              </label>
              <label>
                <span className="field-label required">Şasi no</span>
                <input value={form.chassisNumber} onChange={(event) => setForm((current) => ({ ...current, chassisNumber: event.target.value }))} required />
              </label>
            </div>

            <label>
              <span>Açıklama</span>
              <textarea value={form.description} onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))} rows={3} />
            </label>

            {error ? <div className="alert error">{error}</div> : null}

            <div className="inline-actions">
              <button type="submit" className="primary-button" disabled={saving}>
                {saving ? "Kaydediliyor..." : editingVehicleId ? "Değişiklikleri kaydet" : "Satınalmayı kaydet"}
              </button>
              <button type="button" className="ghost-button dark" onClick={closeModal}>
                İptal
              </button>
            </div>
          </form>
        </div>
      ) : null}

      {pendingDeletePurchase ? (
        <ConfirmDialog
          title="Satınalma kaydı"
          description={`${pendingDeletePurchase.plate} için silme isteği gönderilecek. Masraf veya satış varsa sistem işlemi engeller.`}
          error={error}
          confirmLabel="Devam et"
          busy={deletingVehicleId === pendingDeletePurchase.vehicleId}
          onCancel={() => {
            if (!deletingVehicleId) {
              setError("");
              setPendingDeletePurchase(null);
            }
          }}
          onConfirm={() => void handleDeletePurchase()}
        />
      ) : null}
    </section>
  );
}
