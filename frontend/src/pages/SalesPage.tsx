import { useEffect, useMemo, useState } from "react";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { PageHeader } from "../components/PageHeader";
import { api } from "../lib/api";
import { formatCurrency, formatDate, formatPercent, getPaymentMethodLabel, toDateTimeLocalValue, toIsoFromLocalValue } from "../lib/format";
import { useAuth } from "../state/AuthContext";
import type { CompleteVehicleSaleRequest, Vehicle, VehicleSale } from "../types";

const createInitialForm = (): CompleteVehicleSaleRequest => ({
  salePrice: 0,
  saleDate: new Date().toISOString(),
  paymentMethod: 1,
  counterpartyName: null,
  dueDate: null,
  documentNumber: null,
  installmentCount: null,
  installmentIntervalMonths: null,
  tradePlate: null,
  tradeAmount: null,
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

export function SalesPage() {
  const { session } = useAuth();
  const [sales, setSales] = useState<VehicleSale[]>([]);
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [selectedVehicleId, setSelectedVehicleId] = useState("");
  const [form, setForm] = useState<CompleteVehicleSaleRequest>(createInitialForm);
  const [salePriceInput, setSalePriceInput] = useState("");
  const [tradeAmountInput, setTradeAmountInput] = useState("");
  const [editingVehicleId, setEditingVehicleId] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedYear, setSelectedYear] = useState("");
  const [selectedMonth, setSelectedMonth] = useState("");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [deletingVehicleId, setDeletingVehicleId] = useState<string | null>(null);
  const [pendingDeleteSale, setPendingDeleteSale] = useState<VehicleSale | null>(null);
  const [error, setError] = useState("");
  const [expandedIds, setExpandedIds] = useState<string[]>([]);

  const availableVehicles = useMemo(
    () =>
      vehicles.filter((vehicle) => {
        if (vehicle.ownershipType !== 1) {
          return editingVehicleId === vehicle.id;
        }

        if (vehicle.status === 1) {
          return true;
        }

        return editingVehicleId === vehicle.id;
      }),
    [editingVehicleId, vehicles]
  );

  const yearOptions = useMemo(
    () =>
      [...new Set(sales.map((sale) => getDateParts(sale.soldAt).year))]
        .sort((left, right) => Number(right) - Number(left)),
    [sales]
  );

  const filteredSales = useMemo(
    () =>
      sales.filter((sale) => {
        const { year, month } = getDateParts(sale.soldAt);
        if (selectedYear && year !== selectedYear) {
          return false;
        }
        if (selectedMonth && month !== selectedMonth) {
          return false;
        }
        return true;
      }),
    [sales, selectedMonth, selectedYear]
  );

  const numericSalePrice = Number.parseInt(salePriceInput.replace(/\D/g, ""), 10) || 0;
  const numericTradeAmount = Number.parseInt(tradeAmountInput.replace(/\D/g, ""), 10) || 0;

  async function loadSales() {
    if (!session) {
      return;
    }

    setLoading(true);
    try {
      const [salesData, vehicleData] = await Promise.all([
        api.getSales(session.token),
        api.getVehicles(session.token)
      ]);
      setSales(salesData);
      setVehicles(vehicleData);
      setSelectedVehicleId(
        (current) => current || vehicleData.find((vehicle) => vehicle.status === 1 && vehicle.ownershipType === 1)?.id || ""
      );
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Satis ekrani yuklenemedi.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadSales();
  }, [session]);

  function resetForm() {
    setForm(createInitialForm());
    setSalePriceInput("");
    setTradeAmountInput("");
    setEditingVehicleId(null);
    setSelectedVehicleId(vehicles.find((vehicle) => vehicle.status === 1 && vehicle.ownershipType === 1)?.id || "");
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
    if (!session || !selectedVehicleId) {
      return;
    }

    setSaving(true);
    setError("");

    try {
      const parsedSalePrice = Number.parseInt(salePriceInput.replace(/\D/g, ""), 10);
      const parsedTradeAmount = Number.parseInt(tradeAmountInput.replace(/\D/g, ""), 10);
      const requiresFinanceInfo = form.paymentMethod === 5 || form.paymentMethod === 6 || form.paymentMethod === 7;

      if (!parsedSalePrice) {
        setError("Satis fiyati girmeniz gerekiyor.");
        setSaving(false);
        return;
      }

      const hasTradeInfo = form.paymentMethod === 4;
      if (hasTradeInfo && (!form.tradePlate?.trim() || !parsedTradeAmount)) {
        setError("Takas bilgisi giriyorsaniz plaka ve takas bedelini birlikte doldurun.");
        setSaving(false);
        return;
      }

      if (requiresFinanceInfo && (!form.counterpartyName?.trim() || !form.dueDate)) {
        setError("Cek, senet veya vadeli satista taraf ve vade tarihi zorunludur.");
        setSaving(false);
        return;
      }

      if (form.paymentMethod === 6 && (!form.installmentCount || !form.installmentIntervalMonths)) {
        setError("Senet satisinda taksit sayisi ve taksit araligi zorunludur.");
        setSaving(false);
        return;
      }

      const payload = {
        ...form,
        salePrice: parsedSalePrice,
        counterpartyName: requiresFinanceInfo ? form.counterpartyName?.trim() ?? null : null,
        dueDate: requiresFinanceInfo ? form.dueDate : null,
        documentNumber: requiresFinanceInfo ? form.documentNumber?.trim() ?? null : null,
        installmentCount: form.paymentMethod === 6 ? form.installmentCount : null,
        installmentIntervalMonths: form.paymentMethod === 6 ? form.installmentIntervalMonths : null,
        tradePlate: hasTradeInfo ? form.tradePlate?.trim().toUpperCase() ?? null : null,
        tradeAmount: hasTradeInfo ? parsedTradeAmount || null : null
      };

      if (editingVehicleId) {
        await api.updateSale(session.token, editingVehicleId, payload);
      } else {
        await api.completeSale(session.token, selectedVehicleId, payload);
      }

      closeModal();
      await loadSales();
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : "Satis kaydi tamamlanamadi.");
    } finally {
      setSaving(false);
    }
  }

  function handleEdit(sale: VehicleSale) {
    setEditingVehicleId(sale.vehicleId);
    setSelectedVehicleId(sale.vehicleId);
    setForm({
      salePrice: sale.salePrice,
      saleDate: sale.soldAt,
      paymentMethod: sale.paymentMethod,
      counterpartyName: sale.counterpartyName,
      dueDate: sale.dueDate,
      documentNumber: sale.documentNumber,
      installmentCount: sale.installmentCount,
      installmentIntervalMonths: sale.installmentIntervalMonths,
      tradePlate: sale.tradePlate,
      tradeAmount: sale.tradeAmount,
      description: ""
    });
    setSalePriceInput(formatCurrency(sale.salePrice));
    setTradeAmountInput(sale.tradeAmount ? formatCurrency(sale.tradeAmount) : "");
    setError("");
    setIsModalOpen(true);
  }

  function requestDeleteSale(sale: VehicleSale) {
    setPendingDeleteSale(sale);
    setError("");
  }

  async function handleDeleteSale() {
    if (!session || !pendingDeleteSale) {
      return;
    }

    try {
      setDeletingVehicleId(pendingDeleteSale.vehicleId);
      setError("");
      await api.deleteSale(session.token, pendingDeleteSale.vehicleId);
      setPendingDeleteSale(null);
      await loadSales();
    } catch (deleteError) {
      setError(deleteError instanceof Error ? deleteError.message : "Satis kaydi silinemedi.");
    } finally {
      setDeletingVehicleId(null);
    }
  }

  return (
    <section className="page-stack">
      <PageHeader
        eyebrow="Satış"
        title="Araç satışlarını kapatın"
        description="Stoktaki araçları satışa dönüştürün, gelir ve kâr oranlarını anında izleyin."
        actions={
          <button type="button" className="primary-button" onClick={openCreateModal}>
            Yeni satış
          </button>
        }
      />

      <article className="panel">
        <div className="records-sticky-stack">
          <div className="panel-heading records-heading">
            <h2>Satış geçmişi</h2>
            <p>{loading ? "Liste hazırlanıyor..." : `${filteredSales.length} satış kaydı var.`}</p>
          </div>

          {error && !isModalOpen && !pendingDeleteSale ? <div className="alert error">{error}</div> : null}

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

          <div className="records-table-head sales-table-head records-table-head-sticky">
            <span>Araç</span>
            <span>Tarih</span>
            <span>Tutarlar</span>
            <span>Sonuç</span>
            <span>İşlem</span>
          </div>
        </div>

        <div className="records-table">
          <div className="records-table-body">
            {filteredSales.map((sale) => {
              const isExpanded = expandedIds.includes(sale.vehicleId);

              return (
                <article key={sale.vehicleId} className={`records-row sales-row record-card-shell ${isExpanded ? "expanded" : ""}`}>
                  <button
                    type="button"
                    className={`record-mobile-summary ${isExpanded ? "expanded" : ""}`}
                    onClick={() => toggleExpanded(sale.vehicleId)}
                    aria-expanded={isExpanded}
                  >
                    <div className="record-mobile-summary-main">
                      <span className="status-badge status-success">Satıldı</span>
                      <strong>{sale.plate}</strong>
                      <span>{sale.vehicleDisplayName}</span>
                      <span>
                        {formatDate(sale.soldAt)} · {formatCurrency(sale.salePrice)}
                      </span>
                    </div>
                    <span className="record-mobile-summary-icon" aria-hidden="true">
                      {isExpanded ? "−" : "+"}
                    </span>
                  </button>

                  <div className="records-card-body">
                    <div className="records-cell records-main">
                      <strong>{sale.plate}</strong>
                      <span>{sale.vehicleDisplayName}</span>
                    </div>

                    <div className="records-cell records-dates">
                      <div>
                        <span>Satış</span>
                        <strong>{formatDate(sale.soldAt)}</strong>
                      </div>
                    </div>

                    <div className="records-cell records-finance">
                      <div className="finance-stack">
                        <span>Alış</span>
                        <strong>{formatCurrency(sale.purchasePrice)}</strong>
                        <span>Ödeme</span>
                        <strong>{getPaymentMethodLabel(sale.paymentMethod)}</strong>
                      </div>
                      <div className="finance-stack">
                        <span>Masraf</span>
                        <strong>{formatCurrency(sale.totalExpenseCost)}</strong>
                        <span>Toplam</span>
                        <strong className="finance-strong">{formatCurrency(sale.totalCost)}</strong>
                      </div>
                      <div className="finance-stack">
                        <span>Satış</span>
                        <strong className="finance-strong">{formatCurrency(sale.salePrice)}</strong>
                      </div>
                      <div className="finance-stack">
                        <span>Taraf</span>
                        <strong>{sale.counterpartyName || "-"}</strong>
                        <span>Belge</span>
                        <strong>{sale.documentNumber || "-"}</strong>
                      </div>
                      <div className="finance-stack">
                        <span>Senet planı</span>
                        <strong>
                          {sale.installmentCount && sale.installmentIntervalMonths
                            ? `${sale.installmentCount} taksit / ${sale.installmentIntervalMonths} ay`
                            : "-"}
                        </strong>
                        <span>Takas bedeli</span>
                        <strong>{sale.tradeAmount ? formatCurrency(sale.tradeAmount) : "-"}</strong>
                        <span>Takas plakası</span>
                        <strong>{sale.tradePlate ?? "-"}</strong>
                      </div>
                    </div>

                    <div className="records-cell records-finance">
                      <div className="finance-stack">
                        <span>Kâr</span>
                        <strong className="finance-strong">{formatCurrency(sale.profit)}</strong>
                        <span>Kâr oranı</span>
                        <strong className={`finance-rate ${sale.profit >= 0 ? "positive" : "negative"}`}>
                          {formatPercent(sale.profitMargin)}
                        </strong>
                      </div>
                    </div>

                    <div className="records-cell records-actions">
                      <button type="button" className="ghost-button dark" onClick={() => handleEdit(sale)}>
                        Düzenle
                      </button>
                      <button type="button" className="ghost-button danger" onClick={() => requestDeleteSale(sale)}>
                        Sil
                      </button>
                    </div>
                  </div>
                </article>
              );
            })}

            {!loading && filteredSales.length === 0 ? (
              <div className="empty-panel">Seçilen filtrede satış kaydı bulunmuyor.</div>
            ) : null}
          </div>
        </div>
      </article>

      {isModalOpen ? (
        <div className="modal-backdrop">
          <form className="panel modal-panel data-form" onSubmit={handleSubmit}>
            <div className="modal-header">
              <div className="panel-heading">
                <h2>{editingVehicleId ? "Satis duzenle" : "Yeni satis islemi"}</h2>
              </div>
              <button type="button" className="modal-close" onClick={closeModal} aria-label="Kapat">
                X
              </button>
            </div>

            <label>
              <span>Arac</span>
              <select value={selectedVehicleId} onChange={(event) => setSelectedVehicleId(event.target.value)} required disabled={Boolean(editingVehicleId)}>
                <option value="">Arac secin</option>
                {availableVehicles.map((vehicle) => (
                  <option key={vehicle.id} value={vehicle.id}>
                    {vehicle.plate} - {[vehicle.segment, vehicle.brand, vehicle.model].filter(Boolean).join(" / ")}
                  </option>
                ))}
              </select>
            </label>

            <div className="form-grid">
              <label>
                <span>Satis fiyati</span>
                <input
                  inputMode="numeric"
                  value={salePriceInput}
                  placeholder="Orn. 1.250.000 TL"
                  onChange={(event) => handleCurrencyInput(event.target.value, setSalePriceInput)}
                  required
                />
              </label>
              <label>
                <span>Odeme yontemi</span>
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
                  <option value={3}>Kredi Karti</option>
                  <option value={4}>Takas</option>
                  <option value={5}>Cek</option>
                  <option value={6}>Senet</option>
                  <option value={7}>Vadeli</option>
                </select>
              </label>
              {form.paymentMethod === 4 ? (
                <>
                  <label>
                    <span>Takas aracinin plakasi</span>
                    <input
                      value={form.tradePlate ?? ""}
                      placeholder="Orn. 34XYZ657"
                      onChange={(event) => setForm((current) => ({ ...current, tradePlate: event.target.value || null }))}
                    />
                  </label>
                  <label>
                    <span>Takas bedeli</span>
                    <input
                      inputMode="numeric"
                      value={tradeAmountInput}
                      placeholder="Orn. 50.000 TL"
                      onChange={(event) => handleCurrencyInput(event.target.value, setTradeAmountInput)}
                    />
                  </label>
                </>
              ) : null}
              {form.paymentMethod === 5 || form.paymentMethod === 6 || form.paymentMethod === 7 ? (
                <>
                  <label>
                    <span>Taraf bilgisi</span>
                    <input
                      value={form.counterpartyName ?? ""}
                      placeholder="Orn. Ali Demir / XYZ Ticaret"
                      onChange={(event) => setForm((current) => ({ ...current, counterpartyName: event.target.value || null }))}
                    />
                  </label>
                  <label>
                    <span>{form.paymentMethod === 6 ? "Ilk taksit tarihi" : "Vade tarihi"}</span>
                    <input
                      type="date"
                      value={toDateOnlyInputValue(form.dueDate)}
                      onChange={(event) => setForm((current) => ({ ...current, dueDate: toIsoDateValue(event.target.value) }))}
                    />
                  </label>
                  <label>
                    <span>Belge numarasi</span>
                    <input
                      value={form.documentNumber ?? ""}
                      placeholder="Orn. SNT-2026-014"
                      onChange={(event) => setForm((current) => ({ ...current, documentNumber: event.target.value || null }))}
                    />
                  </label>
                  {form.paymentMethod === 6 ? (
                    <>
                      <label>
                        <span>Taksit araligi (ay)</span>
                        <input
                          type="number"
                          min={1}
                          value={form.installmentIntervalMonths ?? ""}
                          placeholder="Orn. 1"
                          onChange={(event) =>
                            setForm((current) => ({
                              ...current,
                              installmentIntervalMonths: event.target.value ? Number(event.target.value) : null
                            }))
                          }
                        />
                      </label>
                      <label>
                        <span>Taksit sayisi</span>
                        <input
                          type="number"
                          min={1}
                          value={form.installmentCount ?? ""}
                          placeholder="Orn. 4"
                          onChange={(event) =>
                            setForm((current) => ({
                              ...current,
                              installmentCount: event.target.value ? Number(event.target.value) : null
                            }))
                          }
                        />
                      </label>
                    </>
                  ) : null}
                </>
              ) : null}
              <label>
                <span>Satis tarihi</span>
                <input
                  type="datetime-local"
                  value={toDateTimeLocalValue(form.saleDate)}
                  onChange={(event) => setForm((current) => ({ ...current, saleDate: toIsoFromLocalValue(event.target.value) }))}
                  required
                />
              </label>
            </div>

            <label>
              <span>Aciklama</span>
              <textarea
                rows={3}
                value={form.description}
                onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
              />
            </label>

            {error ? <div className="alert error">{error}</div> : null}

            <div className="inline-actions">
              <button type="submit" className="primary-button" disabled={saving || !selectedVehicleId}>
                {saving ? "Isleniyor..." : editingVehicleId ? "Degisiklikleri kaydet" : "Satisi tamamla"}
              </button>
              <button type="button" className="ghost-button dark" onClick={closeModal}>
                Iptal
              </button>
            </div>
          </form>
        </div>
      ) : null}

      {pendingDeleteSale ? (
        <ConfirmDialog
          title="Satis kaydini sil"
          description={`${pendingDeleteSale.plate} aracina ait satis kaydi silinecek. Arac tekrar stok durumuna donecek.`}
          error={error}
          confirmLabel="Satisi sil"
          busy={deletingVehicleId === pendingDeleteSale.vehicleId}
          onCancel={() => {
            if (!deletingVehicleId) {
              setPendingDeleteSale(null);
              setError("");
            }
          }}
          onConfirm={() => void handleDeleteSale()}
        />
      ) : null}
    </section>
  );
}
