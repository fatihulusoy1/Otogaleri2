import { useEffect, useMemo, useRef, useState } from "react";
import { PageHeader } from "../components/PageHeader";
import { PurchaseFormModal } from "../components/PurchaseFormModal";
import { StockConsignmentFormModal } from "../components/StockConsignmentFormModal";
import { BrokeredConsignmentFormModal } from "../components/BrokeredConsignmentFormModal";
import { VehicleSaleFormModal } from "../components/VehicleSaleFormModal";
import { StockConsignmentSaleFormModal } from "../components/StockConsignmentSaleFormModal";
import { VehicleExpenseFormModal } from "../components/VehicleExpenseFormModal";
import { VehiclePhotoModal } from "../components/VehiclePhotoModal";
import { VehiclePhotoThumb } from "../components/VehiclePhotoThumb";
import { PhotoLightbox } from "../components/PhotoLightbox";
import { api } from "../lib/api";
import { formatCurrency, formatDate, formatDateOnly, formatPercent, getPaymentMethodLabel, getVehicleStatusLabel } from "../lib/format";
import { useAuth } from "../state/AuthContext";
import type { ExpenseCategory, StockConsignment, Vehicle, VehicleExpense, VehicleLookups, VehiclePhoto } from "../types";

type NewRecordModal = "purchase" | "stockConsignment" | "brokeredConsignment";

function getVehicleTitle(vehicle: Vehicle) {
  return [vehicle.plate, vehicle.segment, vehicle.brand, vehicle.model].filter(Boolean).join(" / ");
}

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
  const [lookups, setLookups] = useState<VehicleLookups>({ segments: [], brands: [], models: [] });
  const [stockConsignments, setStockConsignments] = useState<StockConsignment[]>([]);
  const [expenseCategories, setExpenseCategories] = useState<ExpenseCategory[]>([]);
  const [isNewMenuOpen, setIsNewMenuOpen] = useState(false);
  const [activeNewModal, setActiveNewModal] = useState<NewRecordModal | null>(null);
  const [saleVehicle, setSaleVehicle] = useState<Vehicle | null>(null);
  const [saleConsignment, setSaleConsignment] = useState<StockConsignment | null>(null);
  const [expenseVehicleId, setExpenseVehicleId] = useState<string | null>(null);
  const [openActionsId, setOpenActionsId] = useState<string | null>(null);
  const [filtersOpen, setFiltersOpen] = useState(false);
  const newMenuRef = useRef<HTMLDivElement | null>(null);
  const [showConsignment, setShowConsignment] = useState(false);
  const [selectedPurchaseYear, setSelectedPurchaseYear] = useState("");
  const [selectedPurchaseMonth, setSelectedPurchaseMonth] = useState("");
  const [selectedSaleYear, setSelectedSaleYear] = useState("");
  const [selectedSaleMonth, setSelectedSaleMonth] = useState("");
  const [selectedStatus, setSelectedStatus] = useState("");
  const [activeVehicle, setActiveVehicle] = useState<Vehicle | null>(null);
  const [photoVehicle, setPhotoVehicle] = useState<Vehicle | null>(null);
  const [lightboxVehicle, setLightboxVehicle] = useState<Vehicle | null>(null);
  const [expandedIds, setExpandedIds] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!session) {
      return;
    }

    setLoading(true);
    Promise.all([
      api.getVehicles(session.token),
      api.getExpenses(session.token),
      api.getVehicleLookups(session.token),
      api.getStockConsignments(session.token),
      api.getExpenseCategories(session.token, 1)
    ])
      .then(([vehicleData, expenseData, lookupData, consignmentData, categoryData]) => {
        setVehicles(vehicleData);
        setExpenses(expenseData);
        setLookups(lookupData);
        setStockConsignments(consignmentData);
        setExpenseCategories(categoryData);
      })
      .catch((requestError) =>
        setError(requestError instanceof Error ? requestError.message : "Araç listesi alınamadı.")
      )
      .finally(() => setLoading(false));
  }, [session]);

  useEffect(() => {
    if (!isNewMenuOpen) {
      return;
    }

    function handleClickOutside(event: MouseEvent) {
      if (newMenuRef.current && !newMenuRef.current.contains(event.target as Node)) {
        setIsNewMenuOpen(false);
      }
    }

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [isNewMenuOpen]);

  useEffect(() => {
    if (!openActionsId) {
      return;
    }

    function handleClickOutside(event: MouseEvent) {
      if (!(event.target as HTMLElement).closest(".row-actions-dropdown")) {
        setOpenActionsId(null);
      }
    }

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [openActionsId]);

  async function reloadVehicles() {
    if (!session) {
      return;
    }

    const [vehicleData, expenseData, consignmentData] = await Promise.all([
      api.getVehicles(session.token),
      api.getExpenses(session.token),
      api.getStockConsignments(session.token)
    ]);
    setVehicles(vehicleData);
    setExpenses(expenseData);
    setStockConsignments(consignmentData);
  }

  function openNewModal(modal: NewRecordModal) {
    setIsNewMenuOpen(false);
    setActiveNewModal(modal);
  }

  async function handleNewModalSaved() {
    setActiveNewModal(null);
    await reloadVehicles();
  }

  function openSaleModal(vehicle: Vehicle) {
    setError("");

    if (vehicle.ownershipType === 2) {
      const consignment = stockConsignments.find((record) => record.id === vehicle.consignmentId);
      if (!consignment) {
        setError("Bu konsinye araca ait stok kaydı bulunamadı.");
        return;
      }
      setSaleConsignment(consignment);
      return;
    }

    setSaleVehicle(vehicle);
  }

  async function handleSaleSaved() {
    setSaleVehicle(null);
    setSaleConsignment(null);
    await reloadVehicles();
  }

  async function handleExpenseSaved() {
    setExpenseVehicleId(null);
    await reloadVehicles();
  }

  function handlePhotosChanged(vehicleId: string, photos: VehiclePhoto[]) {
    setVehicles((current) =>
      current.map((vehicle) => (vehicle.id === vehicleId ? { ...vehicle, photos } : vehicle))
    );
    setPhotoVehicle((current) => (current && current.id === vehicleId ? { ...current, photos } : current));
    setLightboxVehicle((current) => (current && current.id === vehicleId ? { ...current, photos } : current));
  }

  function handlePhotosUploaded(vehicleId: string, created: VehiclePhoto[]) {
    setVehicles((current) =>
      current.map((vehicle) =>
        vehicle.id === vehicleId ? { ...vehicle, photos: [...vehicle.photos, ...created] } : vehicle
      )
    );
  }

  async function handleSetCover(vehicleId: string, photoId: string) {
    if (!session) {
      return;
    }

    try {
      const photos = await api.setVehiclePhotoCover(session.token, photoId);
      handlePhotosChanged(vehicleId, photos);
    } catch (coverError) {
      setError(coverError instanceof Error ? coverError.message : "Ana ekran resmi ayarlanamadı.");
    }
  }

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
        actions={
          <div className={`new-actions-dropdown ${isNewMenuOpen ? "open" : ""}`} ref={newMenuRef}>
            <button
              type="button"
              className="primary-button"
              onClick={() => setIsNewMenuOpen((current) => !current)}
              aria-haspopup="menu"
              aria-expanded={isNewMenuOpen}
            >
              Yeni
              <span className="new-actions-caret" aria-hidden="true">
                ▾
              </span>
            </button>
            {isNewMenuOpen ? (
              <div className="new-actions-menu" role="menu">
                <button type="button" role="menuitem" onClick={() => openNewModal("purchase")}>
                  Yeni araç al
                </button>
                <button type="button" role="menuitem" onClick={() => openNewModal("brokeredConsignment")}>
                  Yeni konsinye alım aracılığı
                </button>
                <button type="button" role="menuitem" onClick={() => openNewModal("stockConsignment")}>
                  Yeni konsinye stok ekle
                </button>
              </div>
            ) : null}
          </div>
        }
      />

      <article className="panel">
        <div className="vehicles-sticky-stack">
          <div className="panel-heading records-heading">
            <h2>Araç listesi</h2>
            <p>{loading ? "Araçlar yükleniyor..." : `${filteredVehicles.length} araç görüntüleniyor.`}</p>
          </div>

          <div className={`filter-section records-filter-shell ${filtersOpen ? "filters-open" : ""}`}>
            <button
              type="button"
              className="filter-toggle ghost-button dark"
              onClick={() => setFiltersOpen((current) => !current)}
              aria-expanded={filtersOpen}
            >
              Filtreler
              <span className="new-actions-caret" aria-hidden="true">
                ▾
              </span>
            </button>
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
                  <div className={`record-mobile-summary ${isExpanded ? "expanded" : ""}`}>
                    <VehiclePhotoThumb
                      vehicle={vehicle}
                      token={session?.token ?? ""}
                      small
                      onView={() => setLightboxVehicle(vehicle)}
                      onManage={() => setPhotoVehicle(vehicle)}
                      onUploaded={(created) => handlePhotosUploaded(vehicle.id, created)}
                      onError={setError}
                    />
                    <button
                      type="button"
                      className="record-mobile-summary-toggle"
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
                  </div>

                  <div className="vehicles-card-body">
                    <div className="vehicles-cell vehicles-origin">
                      <span className={`status-badge ${vehicle.ownershipType === 2 ? "status-warning" : "status-neutral"}`}>
                        {vehicle.ownershipType === 2 ? "Konsinye" : "Galeri"}
                      </span>
                    </div>

                    <div className="vehicles-cell vehicles-main">
                      <VehiclePhotoThumb
                        vehicle={vehicle}
                        token={session?.token ?? ""}
                        onView={() => setLightboxVehicle(vehicle)}
                        onManage={() => setPhotoVehicle(vehicle)}
                        onUploaded={(created) => handlePhotosUploaded(vehicle.id, created)}
                        onError={setError}
                      />
                      <div className="vehicles-main-text">
                        <strong>{vehicle.plate}</strong>
                        <span>{[vehicle.segment, vehicle.brand, vehicle.model].filter(Boolean).join(" / ")}</span>
                        <small>{vehicle.description || "Açıklama girilmemiş."}</small>
                      </div>
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
                  </div>

                  <div className="vehicles-cell vehicles-actions">
                    <div className={`row-actions-dropdown ${openActionsId === vehicle.id ? "open" : ""}`}>
                      <button
                        type="button"
                        className="ghost-button dark"
                        onClick={() => setOpenActionsId((current) => (current === vehicle.id ? null : vehicle.id))}
                        aria-haspopup="menu"
                        aria-expanded={openActionsId === vehicle.id}
                      >
                        İşlemler
                        <span className="new-actions-caret" aria-hidden="true">
                          ▾
                        </span>
                      </button>
                      {openActionsId === vehicle.id ? (
                        <div className="row-actions-menu" role="menu">
                          {vehicle.status === 1 ? (
                            <button
                              type="button"
                              role="menuitem"
                              onClick={() => {
                                setOpenActionsId(null);
                                openSaleModal(vehicle);
                              }}
                            >
                              Araç sat
                            </button>
                          ) : null}
                          <button
                            type="button"
                            role="menuitem"
                            onClick={() => {
                              setOpenActionsId(null);
                              setExpenseVehicleId(vehicle.id);
                            }}
                          >
                            Masraf gir
                          </button>
                          <button
                            type="button"
                            role="menuitem"
                            onClick={() => {
                              setOpenActionsId(null);
                              setActiveVehicle(vehicle);
                            }}
                          >
                            Masrafları göster
                          </button>
                          <button
                            type="button"
                            role="menuitem"
                            onClick={() => {
                              setOpenActionsId(null);
                              setPhotoVehicle(vehicle);
                            }}
                          >
                            Fotoğraflar
                          </button>
                        </div>
                      ) : null}
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

      {session && activeNewModal === "purchase" ? (
        <PurchaseFormModal
          token={session.token}
          lookups={lookups}
          onClose={() => setActiveNewModal(null)}
          onSaved={() => void handleNewModalSaved()}
        />
      ) : null}

      {session && activeNewModal === "stockConsignment" ? (
        <StockConsignmentFormModal
          token={session.token}
          lookups={lookups}
          onClose={() => setActiveNewModal(null)}
          onSaved={() => void handleNewModalSaved()}
        />
      ) : null}

      {session && activeNewModal === "brokeredConsignment" ? (
        <BrokeredConsignmentFormModal
          token={session.token}
          lookups={lookups}
          onClose={() => setActiveNewModal(null)}
          onSaved={() => void handleNewModalSaved()}
        />
      ) : null}

      {session && saleVehicle ? (
        <VehicleSaleFormModal
          token={session.token}
          vehicle={saleVehicle}
          onClose={() => setSaleVehicle(null)}
          onSaved={() => void handleSaleSaved()}
        />
      ) : null}

      {session && saleConsignment ? (
        <StockConsignmentSaleFormModal
          token={session.token}
          record={saleConsignment}
          onClose={() => setSaleConsignment(null)}
          onSaved={() => void handleSaleSaved()}
        />
      ) : null}

      {session && expenseVehicleId ? (
        <VehicleExpenseFormModal
          token={session.token}
          vehicles={vehicles}
          categories={expenseCategories}
          initialVehicleId={expenseVehicleId}
          onClose={() => setExpenseVehicleId(null)}
          onSaved={() => void handleExpenseSaved()}
        />
      ) : null}

      {session && photoVehicle ? (
        <VehiclePhotoModal
          token={session.token}
          vehicleId={photoVehicle.id}
          vehicleTitle={getVehicleTitle(photoVehicle)}
          initialPhotos={photoVehicle.photos}
          onClose={() => setPhotoVehicle(null)}
          onChanged={(photos) => handlePhotosChanged(photoVehicle.id, photos)}
        />
      ) : null}

      {lightboxVehicle && lightboxVehicle.photos.length > 0 ? (
        <PhotoLightbox
          photos={lightboxVehicle.photos}
          title={getVehicleTitle(lightboxVehicle)}
          onSetCover={(photoId) => handleSetCover(lightboxVehicle.id, photoId)}
          onClose={() => setLightboxVehicle(null)}
        />
      ) : null}

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
