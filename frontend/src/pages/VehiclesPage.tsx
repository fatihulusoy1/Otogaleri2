import { useEffect, useMemo, useState } from "react";
import { PageHeader } from "../components/PageHeader";
import { api } from "../lib/api";
import { formatCurrency, formatDate, formatDateOnly, formatPercent, getPaymentMethodLabel, getVehicleStatusLabel } from "../lib/format";
import { useAuth } from "../state/AuthContext";
import type { Vehicle, VehicleExpense } from "../types";

function getDateParts(value: string) {
  const date = new Date(value);
  return {
    year: date.getFullYear().toString(),
    month: (date.getMonth() + 1).toString().padStart(2, "0")
  };
}

export function VehiclesPage() {
  const { session } = useAuth();
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [expenses, setExpenses] = useState<VehicleExpense[]>([]);
  const [showConsignment, setShowConsignment] = useState(false);
  const [selectedPurchaseYear, setSelectedPurchaseYear] = useState("");
  const [selectedPurchaseMonth, setSelectedPurchaseMonth] = useState("");
  const [selectedSaleYear, setSelectedSaleYear] = useState("");
  const [selectedSaleMonth, setSelectedSaleMonth] = useState("");
  const [selectedStatus, setSelectedStatus] = useState("");
  const [activeVehicle, setActiveVehicle] = useState<Vehicle | null>(null);
  const [expandedIds, setExpandedIds] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!session) {
      return;
    }

    setLoading(true);
    Promise.all([api.getVehicles(session.token), api.getExpenses(session.token)])
      .then(([vehicleData, expenseData]) => {
        setVehicles(vehicleData);
        setExpenses(expenseData);
      })
      .catch((requestError) =>
        setError(requestError instanceof Error ? requestError.message : "Araç listesi alınamadı.")
      )
      .finally(() => setLoading(false));
  }, [session]);

  const purchaseYearOptions = useMemo(
    () =>
      [...new Set(vehicles.map((vehicle) => getDateParts(vehicle.purchaseDate).year))]
        .sort((left, right) => Number(right) - Number(left)),
    [vehicles]
  );

  const saleYearOptions = useMemo(
    () =>
      [...new Set(
        vehicles
          .filter((vehicle) => Boolean(vehicle.saleDate))
          .map((vehicle) => getDateParts(vehicle.saleDate as string).year)
      )].sort((left, right) => Number(right) - Number(left)),
    [vehicles]
  );

  const filteredVehicles = useMemo(
    () =>
      vehicles
        .filter((vehicle) => {
          if (!showConsignment && vehicle.ownershipType === 2) {
            return false;
          }

          if (selectedStatus === "stock" && vehicle.status !== 1) {
            return false;
          }

          if (selectedStatus === "sold" && vehicle.status !== 2) {
            return false;
          }

          if (selectedPurchaseYear || selectedPurchaseMonth) {
            const purchaseDate = getDateParts(vehicle.purchaseDate);
            if (selectedPurchaseYear && purchaseDate.year !== selectedPurchaseYear) {
              return false;
            }
            if (selectedPurchaseMonth && purchaseDate.month !== selectedPurchaseMonth) {
              return false;
            }
          }

          if (selectedSaleYear || selectedSaleMonth) {
            if (!vehicle.saleDate) {
              return false;
            }

            const saleDate = getDateParts(vehicle.saleDate);
            if (selectedSaleYear && saleDate.year !== selectedSaleYear) {
              return false;
            }
            if (selectedSaleMonth && saleDate.month !== selectedSaleMonth) {
              return false;
            }
          }

          return true;
        })
        .sort((left, right) => {
          const leftIsStock = left.status === 1;
          const rightIsStock = right.status === 1;

          if (leftIsStock && !rightIsStock) {
            return -1;
          }

          if (!leftIsStock && rightIsStock) {
            return 1;
          }

          const leftDate = left.saleDate ? new Date(left.saleDate).getTime() : 0;
          const rightDate = right.saleDate ? new Date(right.saleDate).getTime() : 0;
          return rightDate - leftDate;
        }),
    [selectedPurchaseMonth, selectedPurchaseYear, selectedSaleMonth, selectedSaleYear, selectedStatus, showConsignment, vehicles]
  );

  const activeVehicleExpenses = useMemo(
    () => expenses.filter((expense) => expense.vehicleId === activeVehicle?.id),
    [activeVehicle?.id, expenses]
  );

  function clearFilters() {
    setShowConsignment(false);
    setSelectedPurchaseYear("");
    setSelectedPurchaseMonth("");
    setSelectedSaleYear("");
    setSelectedSaleMonth("");
    setSelectedStatus("");
  }

  function toggleExpanded(id: string) {
    setExpandedIds((current) => (current.includes(id) ? current.filter((item) => item !== id) : [...current, id]));
  }

  return (
    <section className="page-stack">
      <PageHeader
        eyebrow="Araçlar"
        title="Tüm araç portföyünüz"
        description="Satıştaki, stoktaki ve servis sürecindeki araçları aynı yapı içinde yönetin."
      />

      <article className="panel">
        <div className="vehicles-sticky-stack">
          <div className="panel-heading records-heading">
            <h2>Araç listesi</h2>
            <p>{loading ? "Araçlar yükleniyor..." : `${filteredVehicles.length} araç görüntüleniyor.`}</p>
          </div>

          <div className="filter-section records-filter-shell">
            <strong>Tarih filtreleri</strong>
            <div className="filter-bar two-groups vehicles-filter-bar">
              <label>
                <span>Durum</span>
                <select value={selectedStatus} onChange={(event) => setSelectedStatus(event.target.value)}>
                  <option value="">Tümü</option>
                  <option value="stock">Stokta</option>
                  <option value="sold">Satıldı</option>
                </select>
              </label>
              <label>
                <span>Alış yılı</span>
                <select value={selectedPurchaseYear} onChange={(event) => setSelectedPurchaseYear(event.target.value)}>
                  <option value="">Tüm yıllar</option>
                  {purchaseYearOptions.map((year) => (
                    <option key={year} value={year}>
                      {year}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                <span>Alış ayı</span>
                <select value={selectedPurchaseMonth} onChange={(event) => setSelectedPurchaseMonth(event.target.value)}>
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
              <label>
                <span>Satış yılı</span>
                <select value={selectedSaleYear} onChange={(event) => setSelectedSaleYear(event.target.value)}>
                  <option value="">Tüm yıllar</option>
                  {saleYearOptions.map((year) => (
                    <option key={year} value={year}>
                      {year}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                <span>Satış ayı</span>
                <select value={selectedSaleMonth} onChange={(event) => setSelectedSaleMonth(event.target.value)}>
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
              <label className="checkbox-field">
                <span>Konsinye</span>
                <input
                  type="checkbox"
                  checked={showConsignment}
                  onChange={(event) => setShowConsignment(event.target.checked)}
                />
                <small>Konsinye göster</small>
              </label>
            </div>
          </div>

          <div className="vehicles-table-head vehicles-table-head-sticky">
            <span>Kaynak</span>
            <span>Araç</span>
            <span>Tarih</span>
            <span>Tutarlar</span>
            <span>Durum</span>
            <span>İşlem</span>
          </div>
        </div>

        <div className="vehicles-table">
          <div className="vehicles-table-body">
            {filteredVehicles.map((vehicle) => {
              const isExpanded = expandedIds.includes(vehicle.id);

              return (
                <article key={vehicle.id} className={`vehicles-row vehicles-card-shell ${isExpanded ? "expanded" : ""}`}>
                  <button
                    type="button"
                    className={`record-mobile-summary ${isExpanded ? "expanded" : ""}`}
                    onClick={() => toggleExpanded(vehicle.id)}
                    aria-expanded={isExpanded}
                  >
                    <div className="record-mobile-summary-main">
                      <span className={`status-badge ${vehicle.status === 1 ? "status-danger" : vehicle.status === 2 ? "status-success" : "status-neutral"}`}>
                        {getVehicleStatusLabel(vehicle.status)}
                      </span>
                      <strong>{vehicle.plate}</strong>
                      <span>{[vehicle.segment, vehicle.brand, vehicle.model].filter(Boolean).join(" / ")}</span>
                      <span>
                        {formatDateOnly(vehicle.purchaseDate)}
                        {vehicle.saleDate ? ` · ${formatDateOnly(vehicle.saleDate)}` : ""}
                      </span>
                    </div>
                    <span className="record-mobile-summary-icon" aria-hidden="true">
                      {isExpanded ? "−" : "+"}
                    </span>
                  </button>

                  <div className="vehicles-card-body">
                    <div className="vehicles-cell vehicles-origin">
                      <span className={`status-badge ${vehicle.ownershipType === 2 ? "status-warning" : "status-neutral"}`}>
                        {vehicle.ownershipType === 2 ? "Konsinye" : "Galeri"}
                      </span>
                    </div>

                    <div className="vehicles-cell vehicles-main">
                      <strong>{vehicle.plate}</strong>
                      <span>{[vehicle.segment, vehicle.brand, vehicle.model].filter(Boolean).join(" / ")}</span>
                      <small>{vehicle.description || "Açıklama girilmemiş."}</small>
                    </div>

                    <div className="vehicles-cell vehicles-dates">
                      <div>
                        <span>Alış</span>
                        <strong>{formatDateOnly(vehicle.purchaseDate)}</strong>
                      </div>
                      <div>
                        <span>Satış</span>
                        <strong>{vehicle.saleDate ? formatDateOnly(vehicle.saleDate) : "-"}</strong>
                      </div>
                    </div>

                    <div className="vehicles-cell vehicles-finance">
                      <div className="finance-stack">
                        <span>Alış</span>
                        <strong>{formatCurrency(vehicle.purchasePrice)}</strong>
                        <span>Masraf</span>
                        <strong>{formatCurrency(vehicle.totalExpenseCost)}</strong>
                      </div>
                      <div className="finance-stack">
                        <span>Toplam</span>
                        <strong className="finance-strong">
                          {vehicle.ownershipType === 2
                            ? formatCurrency(vehicle.totalExpenseCost)
                            : formatCurrency(vehicle.totalCost)}
                        </strong>
                        <span>Satış</span>
                        <strong className="finance-strong">
                          {vehicle.actualSalePrice ? formatCurrency(vehicle.actualSalePrice) : "-"}
                        </strong>
                      </div>
                      <div className="finance-stack">
                        <span>Kâr</span>
                        <strong className="finance-strong">
                          {vehicle.ownershipType === 2
                            ? vehicle.consignmentCommissionAmount
                              ? formatCurrency(vehicle.consignmentCommissionAmount)
                              : "-"
                            : vehicle.actualSalePrice
                              ? formatCurrency(vehicle.actualSalePrice - vehicle.totalCost)
                              : "-"}
                        </strong>
                        <span>Kâr oranı</span>
                        {vehicle.ownershipType === 2 ? (
                          <strong>-</strong>
                        ) : (
                          <strong
                            className={`finance-rate ${
                              vehicle.actualSalePrice
                                ? vehicle.actualSalePrice - vehicle.totalCost >= 0
                                  ? "positive"
                                  : "negative"
                                : ""
                            }`}
                          >
                            {vehicle.actualSalePrice
                              ? formatPercent(((vehicle.actualSalePrice - vehicle.totalCost) / vehicle.totalCost) * 100)
                              : "-"}
                          </strong>
                        )}
                      </div>
                    </div>

                    <div className="vehicles-cell">
                      <span
                        className={`status-badge ${
                          vehicle.status === 1 ? "status-danger" : vehicle.status === 2 ? "status-success" : "status-neutral"
                        }`}
                      >
                        {getVehicleStatusLabel(vehicle.status)}
                      </span>
                    </div>

                    <div className="vehicles-cell vehicles-actions">
                      <button type="button" className="ghost-button dark" onClick={() => setActiveVehicle(vehicle)}>
                        Masrafları göster
                      </button>
                    </div>
                  </div>
                </article>
              );
            })}

            {!loading && filteredVehicles.length === 0 ? (
              <div className="empty-panel">Seçilen filtrede araç kaydı bulunmuyor.</div>
            ) : null}
            {error ? <div className="alert error">{error}</div> : null}
          </div>
        </div>
      </article>

      {activeVehicle ? (
        <div className="modal-backdrop" onClick={() => setActiveVehicle(null)}>
          <article className="panel modal-panel" onClick={(event) => event.stopPropagation()}>
            <div className="panel-heading">
              <h2>{activeVehicle.plate} masrafları</h2>
              <p>{[activeVehicle.segment, activeVehicle.brand, activeVehicle.model].filter(Boolean).join(" / ")}</p>
            </div>

            <div className="records-list">
              {activeVehicleExpenses.map((expense) => (
                <article key={expense.id} className="record-card">
                  <div className="record-main">
                    <strong>{expense.description}</strong>
                    <span>{expense.categoryName ?? "Kategorisiz"}</span>
                  </div>
                  <div className="record-meta">
                    <span>{formatCurrency(expense.amount)}</span>
                    <small>
                      {formatDate(expense.expenseDate)} / {getPaymentMethodLabel(expense.paymentMethod)}
                    </small>
                  </div>
                </article>
              ))}

              {activeVehicleExpenses.length === 0 ? (
                <div className="empty-panel">Bu araca ait masraf kaydı bulunmuyor.</div>
              ) : null}
            </div>

            <div className="inline-actions">
              <button type="button" className="ghost-button dark" onClick={() => setActiveVehicle(null)}>
                Kapat
              </button>
            </div>
          </article>
        </div>
      ) : null}
    </section>
  );
}
