import { useEffect, useMemo, useState } from "react";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { PageHeader } from "../components/PageHeader";
import { SearchableSelect } from "../components/SearchableSelect";
import { api } from "../lib/api";
import { formatCurrency, formatDate, formatPercent, toDateTimeLocalValue, toIsoFromLocalValue } from "../lib/format";
import { useAuth } from "../state/AuthContext";
import type { BrokeredConsignment, CreateBrokeredConsignmentRequest, VehicleLookups } from "../types";

const createInitialForm = (): CreateBrokeredConsignmentRequest => ({
  ownerName: "",
  ownerPhone: "",
  customerName: "",
  customerPhone: "",
  plate: "",
  segmentId: "",
  brandId: "",
  modelId: "",
  year: new Date().getFullYear(),
  color: "",
  engineNumber: "",
  chassisNumber: "",
  consignmentDate: new Date().toISOString(),
  purchasePrice: 0,
  commissionAmount: null,
  commissionRate: null,
  description: ""
});

export function BrokeredConsignmentsPage() {
  const { session } = useAuth();
  const [records, setRecords] = useState<BrokeredConsignment[]>([]);
  const [lookups, setLookups] = useState<VehicleLookups>({ segments: [], brands: [], models: [] });
  const [form, setForm] = useState<CreateBrokeredConsignmentRequest>(createInitialForm);
  const [purchasePriceInput, setPurchasePriceInput] = useState("");
  const [commissionAmountInput, setCommissionAmountInput] = useState("");
  const [commissionRateInput, setCommissionRateInput] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [pendingDelete, setPendingDelete] = useState<BrokeredConsignment | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!session) {
      return;
    }

    setLoading(true);
    Promise.all([api.getBrokeredConsignments(session.token), api.getVehicleLookups(session.token)])
      .then(([data, lookupData]) => {
        setRecords(data);
        setLookups(lookupData);
      })
      .catch((requestError) =>
        setError(requestError instanceof Error ? requestError.message : "Konsinye aracilik kayitlari alinamadi.")
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

  function resetForm() {
    setForm(createInitialForm());
    setPurchasePriceInput("");
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

  function handleCurrencyInput(value: string, setter: (value: string) => void) {
    const digits = value.replace(/\D/g, "");
    setter(digits ? formatCurrency(Number(digits)) : "");
  }

  async function reload() {
    if (!session) {
      return;
    }

    const data = await api.getBrokeredConsignments(session.token);
    setRecords(data);
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!session) {
      return;
    }

    const purchasePrice = Number.parseInt(purchasePriceInput.replace(/\D/g, ""), 10);
    const commissionAmount = Number.parseInt(commissionAmountInput.replace(/\D/g, ""), 10);
    const commissionRate = Number.parseFloat(commissionRateInput.replace(",", "."));

    if (!purchasePrice) {
      setError("Alim tutari girmeniz gerekiyor.");
      return;
    }

    setSaving(true);
    setError("");

    try {
      const payload = {
        ...form,
        purchasePrice,
        commissionAmount: commissionAmount || null,
        commissionRate: Number.isFinite(commissionRate) ? commissionRate : null
      };

      if (editingId) {
        await api.updateBrokeredConsignment(session.token, editingId, payload);
      } else {
        await api.createBrokeredConsignment(session.token, payload);
      }

      closeModal();
      await reload();
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : "Konsinye kaydi kaydedilemedi.");
    } finally {
      setSaving(false);
    }
  }

  function handleEdit(record: BrokeredConsignment) {
    setEditingId(record.id);
    setForm({
      ownerName: record.ownerName,
      ownerPhone: record.ownerPhone,
      customerName: record.customerName,
      customerPhone: record.customerPhone,
      plate: record.plate,
      segmentId: record.segmentId ?? "",
      brandId: record.brandId ?? "",
      modelId: record.modelId ?? "",
      year: record.year,
      color: record.color,
      engineNumber: record.engineNumber,
      chassisNumber: record.chassisNumber,
      consignmentDate: record.consignmentDate,
      purchasePrice: record.purchasePrice,
      commissionAmount: record.commissionAmount,
      commissionRate: record.commissionRate,
      description: record.description ?? ""
    });
    setPurchasePriceInput(formatCurrency(record.purchasePrice));
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
      await api.deleteBrokeredConsignment(session.token, pendingDelete.id);
      setPendingDelete(null);
      await reload();
    } catch (deleteError) {
      setError(deleteError instanceof Error ? deleteError.message : "Konsinye kaydi silinemedi.");
    } finally {
      setDeletingId(null);
    }
  }

  return (
    <section className="page-stack">
      <PageHeader
        eyebrow="Konsinye Aracilik"
        title="Baska biri adina alim islemleri"
        description="Galeri adina degil, musteri adina yapilan alimlari ve komisyon gelirlerini yonetin."
        actions={
          <button type="button" className="primary-button" onClick={openCreateModal}>
            Yeni aracilik kaydi
          </button>
        }
      />

      {error && !isModalOpen ? <div className="alert error">{error}</div> : null}

      <article className="panel">
        <div className="panel-heading">
          <h2>Aracilik kayitlari</h2>
          <p>{loading ? "Liste hazirlaniyor..." : `${records.length} kayit var.`}</p>
        </div>

        <div className="records-table">
          <div className="records-table-head brokered-table-head">
            <span>Kisi ve arac</span>
            <span>Tarih</span>
            <span>Tutarlar</span>
            <span>Durum</span>
            <span>Islem</span>
          </div>

          <div className="records-table-body">
            {records.map((record) => (
              <article key={record.id} className="records-row brokered-row">
                <div className="records-cell records-main">
                  <strong>{record.plate}</strong>
                  <span>{[record.segment, record.brand, record.model].filter(Boolean).join(" / ")}</span>
                  <small>Sahip: {record.ownerName}</small>
                  <small>Alici: {record.customerName || "-"}</small>
                </div>

                <div className="records-cell records-dates">
                  <div>
                    <span>Islem</span>
                    <strong>{formatDate(record.consignmentDate)}</strong>
                  </div>
                </div>

                <div className="records-cell records-finance">
                  <div className="finance-stack">
                    <span>Alim</span>
                    <strong>{formatCurrency(record.purchasePrice)}</strong>
                    <span>Komisyon</span>
                    <strong className="finance-strong">
                      {record.commissionAmount ? formatCurrency(record.commissionAmount) : "-"}
                    </strong>
                  </div>
                  <div className="finance-stack">
                    <span>Komisyon orani</span>
                    <strong className="finance-rate positive">
                      {record.commissionRate ? formatPercent(record.commissionRate) : "-"}
                    </strong>
                    <span>Telefon</span>
                    <strong>{record.ownerPhone || "-"}</strong>
                  </div>
                </div>

                <div className="records-cell">
                  <span className="status-badge status-success">Tamamlandi</span>
                </div>

                <div className="records-cell records-actions">
                  <button type="button" className="ghost-button dark" onClick={() => handleEdit(record)}>
                    Duzenle
                  </button>
                  <button type="button" className="ghost-button danger" onClick={() => setPendingDelete(record)}>
                    Sil
                  </button>
                </div>
              </article>
            ))}

            {!loading && records.length === 0 ? <div className="empty-panel">Henuz aracilik kaydi yok.</div> : null}
          </div>
        </div>
      </article>

      {isModalOpen ? (
        <div className="modal-backdrop">
          <form className="panel modal-panel data-form" onSubmit={handleSubmit}>
            <div className="modal-header">
              <div className="panel-heading">
                <h2>{editingId ? "Aracilik kaydi duzenle" : "Yeni aracilik kaydi"}</h2>
              </div>
              <button type="button" className="modal-close" onClick={closeModal} aria-label="Kapat">
                X
              </button>
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
                <span>Alici adi</span>
                <input value={form.customerName ?? ""} onChange={(event) => setForm((current) => ({ ...current, customerName: event.target.value }))} />
              </label>
              <label>
                <span>Alici telefonu</span>
                <input value={form.customerPhone ?? ""} onChange={(event) => setForm((current) => ({ ...current, customerPhone: event.target.value }))} />
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
                <span>Islem tarihi</span>
                <input type="datetime-local" value={toDateTimeLocalValue(form.consignmentDate)} onChange={(event) => setForm((current) => ({ ...current, consignmentDate: toIsoFromLocalValue(event.target.value) }))} required />
              </label>
              <label>
                <span>Alim tutari</span>
                <input inputMode="numeric" value={purchasePriceInput} onChange={(event) => handleCurrencyInput(event.target.value, setPurchasePriceInput)} required />
              </label>
              <label>
                <span>Komisyon tutari</span>
                <input inputMode="numeric" value={commissionAmountInput} onChange={(event) => handleCurrencyInput(event.target.value, setCommissionAmountInput)} />
              </label>
              <label>
                <span>Komisyon orani</span>
                <input value={commissionRateInput} onChange={(event) => setCommissionRateInput(event.target.value)} placeholder="Orn. 4.5" />
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
              <button type="button" className="ghost-button dark" onClick={closeModal}>
                Iptal
              </button>
            </div>
          </form>
        </div>
      ) : null}

      {pendingDelete ? (
        <ConfirmDialog
          title="Aracilik kaydini sil"
          description={`${pendingDelete.plate} icin olusturulan konsinye aracilik kaydi silinecek.`}
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
    </section>
  );
}
