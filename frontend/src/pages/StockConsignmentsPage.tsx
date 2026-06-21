import { useEffect, useMemo, useState } from "react";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { PageHeader } from "../components/PageHeader";
import { SearchableSelect } from "../components/SearchableSelect";
import { api } from "../lib/api";
import { formatCurrency, formatDate, formatPercent, toDateTimeLocalValue, toIsoFromLocalValue } from "../lib/format";
import { useAuth } from "../state/AuthContext";
import type {
  CompleteStockConsignmentSaleRequest,
  CreateStockConsignmentRequest,
  StockConsignment,
  VehicleLookups
} from "../types";

const createInitialForm = (): CreateStockConsignmentRequest => ({
  ownerName: "",
  ownerPhone: "",
  plate: "",
  segmentId: "",
  brandId: "",
  modelId: "",
  year: new Date().getFullYear(),
  color: "",
  engineNumber: "",
  chassisNumber: "",
  consignmentDate: new Date().toISOString(),
  basePrice: 0,
  expectedSalePrice: null,
  commissionAmount: null,
  commissionRate: null,
  description: ""
});

const createSaleForm = (): CompleteStockConsignmentSaleRequest => ({
  salePrice: 0,
  saleDate: new Date().toISOString(),
  commissionAmount: null,
  commissionRate: null,
  description: ""
});

export function StockConsignmentsPage() {
  const { session } = useAuth();
  const [records, setRecords] = useState<StockConsignment[]>([]);
  const [lookups, setLookups] = useState<VehicleLookups>({ segments: [], brands: [], models: [] });
  const [form, setForm] = useState<CreateStockConsignmentRequest>(createInitialForm);
  const [saleForm, setSaleForm] = useState<CompleteStockConsignmentSaleRequest>(createSaleForm);
  const [basePriceInput, setBasePriceInput] = useState("");
  const [expectedSaleInput, setExpectedSaleInput] = useState("");
  const [commissionAmountInput, setCommissionAmountInput] = useState("");
  const [commissionRateInput, setCommissionRateInput] = useState("");
  const [salePriceInput, setSalePriceInput] = useState("");
  const [saleCommissionAmountInput, setSaleCommissionAmountInput] = useState("");
  const [saleCommissionRateInput, setSaleCommissionRateInput] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [sellingRecord, setSellingRecord] = useState<StockConsignment | null>(null);
  const [pendingDelete, setPendingDelete] = useState<StockConsignment | null>(null);
  const [pendingDeleteSale, setPendingDeleteSale] = useState<StockConsignment | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isSaleModalOpen, setIsSaleModalOpen] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!session) {
      return;
    }

    setLoading(true);
    Promise.all([api.getStockConsignments(session.token), api.getVehicleLookups(session.token)])
      .then(([data, lookupData]) => {
        setRecords(data);
        setLookups(lookupData);
      })
      .catch((requestError) =>
        setError(requestError instanceof Error ? requestError.message : "Konsinye stok kayitlari alinamadi.")
      )
      .finally(() => setLoading(false));
  }, [session]);

  const filteredModels = useMemo(
    () =>
      lookups.models
        .filter((model) => (!form.brandId || model.brandId === form.brandId))
        .filter((model) => (!form.segmentId || !model.segmentId || model.segmentId === form.segmentId))
        .map((model) => ({ id: model.id, name: model.name })),
    [form.brandId, form.segmentId, lookups.models]
  );

  async function reload() {
    if (!session) {
      return;
    }

    const data = await api.getStockConsignments(session.token);
    setRecords(data);
  }

  function resetForm() {
    setForm(createInitialForm());
    setBasePriceInput("");
    setExpectedSaleInput("");
    setCommissionAmountInput("");
    setCommissionRateInput("");
    setEditingId(null);
    setError("");
  }

  function closeModal() {
    setIsModalOpen(false);
    resetForm();
  }

  function openCreateModal() {
    resetForm();
    setIsModalOpen(true);
  }

  function openSaleModal(record: StockConsignment) {
    setSellingRecord(record);
    setSaleForm({
      salePrice: record.salePrice ?? 0,
      saleDate: record.saleDate ?? new Date().toISOString(),
      commissionAmount: record.commissionAmount,
      commissionRate: record.commissionRate,
      description: record.description ?? ""
    });
    setSalePriceInput(record.salePrice ? formatCurrency(record.salePrice) : "");
    setSaleCommissionAmountInput(record.commissionAmount ? formatCurrency(record.commissionAmount) : "");
    setSaleCommissionRateInput(record.commissionRate?.toString() ?? "");
    setError("");
    setIsSaleModalOpen(true);
  }

  function closeSaleModal() {
    setSellingRecord(null);
    setSaleForm(createSaleForm());
    setSalePriceInput("");
    setSaleCommissionAmountInput("");
    setSaleCommissionRateInput("");
    setError("");
    setIsSaleModalOpen(false);
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

    const basePrice = Number.parseInt(basePriceInput.replace(/\D/g, ""), 10);
    const expectedSalePrice = Number.parseInt(expectedSaleInput.replace(/\D/g, ""), 10);
    const commissionAmount = Number.parseInt(commissionAmountInput.replace(/\D/g, ""), 10);
    const commissionRate = Number.parseFloat(commissionRateInput.replace(",", "."));

    if (!basePrice) {
      setError("Baz tutar girmeniz gerekiyor.");
      return;
    }

    setSaving(true);
    setError("");

    try {
      const payload = {
        ...form,
        basePrice,
        expectedSalePrice: expectedSalePrice || null,
        commissionAmount: commissionAmount || null,
        commissionRate: Number.isFinite(commissionRate) ? commissionRate : null
      };

      if (editingId) {
        await api.updateStockConsignment(session.token, editingId, payload);
      } else {
        await api.createStockConsignment(session.token, payload);
      }

      closeModal();
      await reload();
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : "Konsinye stok kaydedilemedi.");
    } finally {
      setSaving(false);
    }
  }

  async function handleSaleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!session || !sellingRecord) {
      return;
    }

    const salePrice = Number.parseInt(salePriceInput.replace(/\D/g, ""), 10);
    const commissionAmount = Number.parseInt(saleCommissionAmountInput.replace(/\D/g, ""), 10);
    const commissionRate = Number.parseFloat(saleCommissionRateInput.replace(",", "."));

    if (!salePrice) {
      setError("Satis tutari girmeniz gerekiyor.");
      return;
    }

    setSaving(true);
    setError("");

    try {
      const payload = {
        ...saleForm,
        salePrice,
        commissionAmount: commissionAmount || null,
        commissionRate: Number.isFinite(commissionRate) ? commissionRate : null
      };

      if (sellingRecord.status === 3) {
        await api.updateStockConsignmentSale(session.token, sellingRecord.id, payload);
      } else {
        await api.completeStockConsignmentSale(session.token, sellingRecord.id, payload);
      }

      closeSaleModal();
      await reload();
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : "Konsinye satisi kaydedilemedi.");
    } finally {
      setSaving(false);
    }
  }

  function handleEdit(record: StockConsignment) {
    setEditingId(record.id);
    setForm({
      ownerName: record.ownerName,
      ownerPhone: record.ownerPhone,
      plate: record.plate,
      segmentId: record.segmentId ?? "",
      brandId: record.brandId ?? "",
      modelId: record.modelId ?? "",
      year: record.year,
      color: record.color,
      engineNumber: record.engineNumber,
      chassisNumber: record.chassisNumber,
      consignmentDate: record.consignmentDate,
      basePrice: record.basePrice,
      expectedSalePrice: record.expectedSalePrice,
      commissionAmount: record.commissionAmount,
      commissionRate: record.commissionRate,
      description: record.description ?? ""
    });
    setBasePriceInput(formatCurrency(record.basePrice));
    setExpectedSaleInput(record.expectedSalePrice ? formatCurrency(record.expectedSalePrice) : "");
    setCommissionAmountInput(record.commissionAmount ? formatCurrency(record.commissionAmount) : "");
    setCommissionRateInput(record.commissionRate?.toString() ?? "");
    setError("");
    setIsModalOpen(true);
  }

  async function handleDelete() {
    if (!session || !pendingDelete) {
      return;
    }

    try {
      setDeletingId(pendingDelete.id);
      setError("");
      await api.deleteStockConsignment(session.token, pendingDelete.id);
      setPendingDelete(null);
      await reload();
    } catch (deleteError) {
      setError(deleteError instanceof Error ? deleteError.message : "Konsinye stok silinemedi.");
    } finally {
      setDeletingId(null);
    }
  }

  async function handleDeleteSale() {
    if (!session || !pendingDeleteSale) {
      return;
    }

    try {
      setDeletingId(pendingDeleteSale.id);
      setError("");
      await api.deleteStockConsignmentSale(session.token, pendingDeleteSale.id);
      setPendingDeleteSale(null);
      await reload();
    } catch (deleteError) {
      setError(deleteError instanceof Error ? deleteError.message : "Konsinye satisi silinemedi.");
    } finally {
      setDeletingId(null);
    }
  }

  return (
    <section className="page-stack">
      <PageHeader
        eyebrow="Konsinye Stok"
        title="Stoktaki konsinye araclari yonetin"
        description="Stoğa emanet giren araclari, satislarini ve komisyon dagilimini bu ekrandan takip edin."
        actions={
          <button type="button" className="primary-button" onClick={openCreateModal}>
            Yeni konsinye stok
          </button>
        }
      />

      {error && !isModalOpen && !isSaleModalOpen ? <div className="alert error">{error}</div> : null}

      <article className="panel">
        <div className="panel-heading">
          <h2>Konsinye stok listesi</h2>
          <p>{loading ? "Liste hazirlaniyor..." : `${records.length} kayit var.`}</p>
        </div>

        <div className="records-table">
          <div className="records-table-head stock-consignment-table-head">
            <span>Arac</span>
            <span>Sahip ve tarih</span>
            <span>Tutarlar</span>
            <span>Durum</span>
            <span>Islem</span>
          </div>

          <div className="records-table-body">
            {records.map((record) => (
              <article key={record.id} className="records-row stock-consignment-row">
                <div className="records-cell records-main">
                  <strong>{record.plate}</strong>
                  <span>{[record.segment, record.brand, record.model].filter(Boolean).join(" / ")}</span>
                  <small>{record.description || "Aciklama girilmemis."}</small>
                </div>

                <div className="records-cell records-dates">
                  <div>
                    <span>Sahip</span>
                    <strong>{record.ownerName}</strong>
                  </div>
                  <div>
                    <span>Giris</span>
                    <strong>{formatDate(record.consignmentDate)}</strong>
                  </div>
                </div>

                <div className="records-cell records-finance">
                  <div className="finance-stack">
                    <span>Baz</span>
                    <strong>{formatCurrency(record.basePrice)}</strong>
                    <span>Hedef</span>
                    <strong>{record.expectedSalePrice ? formatCurrency(record.expectedSalePrice) : "-"}</strong>
                  </div>
                  <div className="finance-stack">
                    <span>Satis</span>
                    <strong className="finance-strong">{record.salePrice ? formatCurrency(record.salePrice) : "-"}</strong>
                    <span>Komisyon</span>
                    <strong className="finance-strong">
                      {record.commissionAmount ? formatCurrency(record.commissionAmount) : record.commissionRate ? formatPercent(record.commissionRate) : "-"}
                    </strong>
                  </div>
                  <div className="finance-stack">
                    <span>Sahibe net</span>
                    <strong className="finance-strong">{record.netAmountToOwner ? formatCurrency(record.netAmountToOwner) : "-"}</strong>
                    <span>Satis tarihi</span>
                    <strong>{record.saleDate ? formatDate(record.saleDate) : "-"}</strong>
                  </div>
                </div>

                <div className="records-cell">
                  <span className={`status-badge ${record.status === 3 ? "status-success" : "status-warning"}`}>
                    {record.status === 3 ? "Satildi" : "Stokta"}
                  </span>
                </div>

                <div className="records-cell records-actions">
                  <button type="button" className="ghost-button dark" onClick={() => handleEdit(record)}>
                    Duzenle
                  </button>
                  <button type="button" className="ghost-button dark" onClick={() => openSaleModal(record)}>
                    {record.status === 3 ? "Satisi duzenle" : "Satis yap"}
                  </button>
                  {record.status === 3 ? (
                    <button type="button" className="ghost-button danger" onClick={() => setPendingDeleteSale(record)}>
                      Satisi sil
                    </button>
                  ) : (
                    <button type="button" className="ghost-button danger" onClick={() => setPendingDelete(record)}>
                      Sil
                    </button>
                  )}
                </div>
              </article>
            ))}

            {!loading && records.length === 0 ? <div className="empty-panel">Henuz konsinye stok kaydi yok.</div> : null}
          </div>
        </div>
      </article>

      {isModalOpen ? (
        <div className="modal-backdrop">
          <form className="panel modal-panel data-form" onSubmit={handleSubmit}>
            <div className="modal-header">
              <div className="panel-heading">
                <h2>{editingId ? "Konsinye stok duzenle" : "Yeni konsinye stok"}</h2>
              </div>
              <button type="button" className="modal-close" onClick={closeModal} aria-label="Kapat">X</button>
            </div>

            <div className="form-grid">
              <label>
                <span>Arac sahibi</span>
                <input value={form.ownerName} onChange={(event) => setForm((current) => ({ ...current, ownerName: event.target.value }))} required />
              </label>
              <label>
                <span>Sahip telefonu</span>
                <input value={form.ownerPhone ?? ""} onChange={(event) => setForm((current) => ({ ...current, ownerPhone: event.target.value }))} />
              </label>
              <label>
                <span>Plaka</span>
                <input value={form.plate} onChange={(event) => setForm((current) => ({ ...current, plate: event.target.value }))} required />
              </label>
              <SearchableSelect label="Segment" value={form.segmentId} options={lookups.segments} placeholder="Segment secin" onChange={(segmentId) => setForm((current) => ({ ...current, segmentId, modelId: "" }))} />
              <SearchableSelect label="Marka" value={form.brandId} options={lookups.brands} placeholder="Marka secin" onChange={(brandId) => setForm((current) => ({ ...current, brandId, modelId: "" }))} />
              <SearchableSelect label="Model" value={form.modelId} options={filteredModels} placeholder="Model secin" onChange={(modelId) => setForm((current) => ({ ...current, modelId }))} disabled={!form.brandId} />
              <label>
                <span>Yil</span>
                <input type="number" value={form.year} onChange={(event) => setForm((current) => ({ ...current, year: Number(event.target.value) }))} required />
              </label>
              <label>
                <span>Renk</span>
                <input value={form.color} onChange={(event) => setForm((current) => ({ ...current, color: event.target.value }))} required />
              </label>
              <label>
                <span>Motor no</span>
                <input value={form.engineNumber} onChange={(event) => setForm((current) => ({ ...current, engineNumber: event.target.value }))} required />
              </label>
              <label>
                <span>Sasi no</span>
                <input value={form.chassisNumber} onChange={(event) => setForm((current) => ({ ...current, chassisNumber: event.target.value }))} required />
              </label>
              <label>
                <span>Giris tarihi</span>
                <input type="datetime-local" value={toDateTimeLocalValue(form.consignmentDate)} onChange={(event) => setForm((current) => ({ ...current, consignmentDate: toIsoFromLocalValue(event.target.value) }))} required />
              </label>
              <label>
                <span>Baz tutar</span>
                <input inputMode="numeric" value={basePriceInput} onChange={(event) => handleCurrencyInput(event.target.value, setBasePriceInput)} required />
              </label>
              <label>
                <span>Hedef satis</span>
                <input inputMode="numeric" value={expectedSaleInput} onChange={(event) => handleCurrencyInput(event.target.value, setExpectedSaleInput)} />
              </label>
              <label>
                <span>Komisyon tutari</span>
                <input inputMode="numeric" value={commissionAmountInput} onChange={(event) => handleCurrencyInput(event.target.value, setCommissionAmountInput)} />
              </label>
              <label>
                <span>Komisyon orani</span>
                <input value={commissionRateInput} onChange={(event) => setCommissionRateInput(event.target.value)} placeholder="Orn. 5" />
              </label>
            </div>

            <label>
              <span>Aciklama</span>
              <textarea rows={3} value={form.description} onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))} />
            </label>

            {error ? <div className="alert error">{error}</div> : null}

            <div className="inline-actions">
              <button type="submit" className="primary-button" disabled={saving}>
                {saving ? "Kaydediliyor..." : editingId ? "Degisiklikleri kaydet" : "Kaydi kaydet"}
              </button>
              <button type="button" className="ghost-button dark" onClick={closeModal}>Iptal</button>
            </div>
          </form>
        </div>
      ) : null}

      {isSaleModalOpen && sellingRecord ? (
        <div className="modal-backdrop">
          <form className="panel modal-panel data-form" onSubmit={handleSaleSubmit}>
            <div className="modal-header">
              <div className="panel-heading">
                <h2>{sellingRecord.status === 3 ? "Konsinye satis duzenle" : "Konsinye satis yap"}</h2>
              </div>
              <button type="button" className="modal-close" onClick={closeSaleModal} aria-label="Kapat">X</button>
            </div>

            <div className="panel-heading">
              <h2>{sellingRecord.plate}</h2>
              <p>{[sellingRecord.segment, sellingRecord.brand, sellingRecord.model].filter(Boolean).join(" / ")}</p>
            </div>

            <div className="form-grid">
              <label>
                <span>Satis tutari</span>
                <input inputMode="numeric" value={salePriceInput} onChange={(event) => handleCurrencyInput(event.target.value, setSalePriceInput)} required />
              </label>
              <label>
                <span>Satis tarihi</span>
                <input type="datetime-local" value={toDateTimeLocalValue(saleForm.saleDate)} onChange={(event) => setSaleForm((current) => ({ ...current, saleDate: toIsoFromLocalValue(event.target.value) }))} required />
              </label>
              <label>
                <span>Komisyon tutari</span>
                <input inputMode="numeric" value={saleCommissionAmountInput} onChange={(event) => handleCurrencyInput(event.target.value, setSaleCommissionAmountInput)} />
              </label>
              <label>
                <span>Komisyon orani</span>
                <input value={saleCommissionRateInput} onChange={(event) => setSaleCommissionRateInput(event.target.value)} placeholder="Orn. 5" />
              </label>
            </div>

            <label>
              <span>Aciklama</span>
              <textarea rows={3} value={saleForm.description} onChange={(event) => setSaleForm((current) => ({ ...current, description: event.target.value }))} />
            </label>

            {error ? <div className="alert error">{error}</div> : null}

            <div className="inline-actions">
              <button type="submit" className="primary-button" disabled={saving}>
                {saving ? "Kaydediliyor..." : sellingRecord.status === 3 ? "Satisi guncelle" : "Satisi tamamla"}
              </button>
              <button type="button" className="ghost-button dark" onClick={closeSaleModal}>Iptal</button>
            </div>
          </form>
        </div>
      ) : null}

      {pendingDelete ? (
        <ConfirmDialog
          title="Konsinye stok kaydini sil"
          description={`${pendingDelete.plate} icin olusturulan konsinye stok kaydi silinecek.`}
          error={error}
          confirmLabel="Kaydi sil"
          busy={deletingId === pendingDelete.id}
          onCancel={() => {
            if (!deletingId) {
              setError("");
              setPendingDelete(null);
            }
          }}
          onConfirm={() => void handleDelete()}
        />
      ) : null}

      {pendingDeleteSale ? (
        <ConfirmDialog
          title="Konsinye satisini sil"
          description={`${pendingDeleteSale.plate} icin kaydedilen konsinye satisi geri alinacak ve arac tekrar stoga donecek.`}
          error={error}
          confirmLabel="Satisi sil"
          busy={deletingId === pendingDeleteSale.id}
          onCancel={() => {
            if (!deletingId) {
              setError("");
              setPendingDeleteSale(null);
            }
          }}
          onConfirm={() => void handleDeleteSale()}
        />
      ) : null}
    </section>
  );
}
