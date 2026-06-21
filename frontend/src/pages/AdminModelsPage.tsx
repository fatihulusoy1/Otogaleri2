import { useEffect, useMemo, useState } from "react";
import { PageHeader } from "../components/PageHeader";
import { SearchableSelect } from "../components/SearchableSelect";
import { api } from "../lib/api";
import { useAuth } from "../state/AuthContext";
import type { AdminCatalogLookups } from "../types";

export function AdminModelsPage() {
  const { session } = useAuth();
  const [catalog, setCatalog] = useState<AdminCatalogLookups>({ segments: [], brands: [], models: [] });
  const [name, setName] = useState("");
  const [brandId, setBrandId] = useState("");
  const [segmentId, setSegmentId] = useState("");
  const [error, setError] = useState("");

  const visibleModels = useMemo(
    () => catalog.models.filter((model) => !brandId || model.brandId === brandId),
    [catalog.models, brandId]
  );

  async function loadModels() {
    if (!session) return;
    try {
      setError("");
      setCatalog(await api.adminGetCatalog(session.token));
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Modeller yuklenemedi.");
    }
  }

  useEffect(() => {
    void loadModels();
  }, [session]);

  async function createModel() {
    if (!session || !brandId || !name.trim()) return;
    try {
      setError("");
      await api.adminCreateModel(session.token, { brandId, segmentId: segmentId || null, name });
      setName("");
      await loadModels();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Model eklenemedi.");
    }
  }

  async function updateModel(modelId: string, currentName: string, currentBrandId: string, currentSegmentId: string | null) {
    if (!session) return;
    const nextName = window.prompt("Yeni model adi", currentName)?.trim();
    if (!nextName) return;
    try {
      setError("");
      await api.adminUpdateModel(session.token, modelId, { brandId: currentBrandId, segmentId: currentSegmentId, name: nextName });
      await loadModels();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Model guncellenemedi.");
    }
  }

  async function deleteModel(modelId: string) {
    if (!session) return;
    try {
      setError("");
      await api.adminDeleteModel(session.token, modelId);
      await loadModels();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Model silinemedi.");
    }
  }

  return (
    <section className="page-stack">
      <PageHeader eyebrow="Super Admin" title="Model yonetimi" description="Tum kullanicilarin gorecegi ortak model listesini buradan yonetin." />
      <article className="panel data-form">
        <SearchableSelect label="Marka" value={brandId} options={catalog.brands} placeholder="Marka secin" onChange={setBrandId} />
        <SearchableSelect label="Segment" value={segmentId} options={catalog.segments} placeholder="Istege bagli segment" onChange={setSegmentId} />
        <input value={name} onChange={(event) => setName(event.target.value)} placeholder="Ornek: 3008" />
        <button type="button" className="primary-button" onClick={() => void createModel()}>Model ekle</button>
        {error ? <div className="alert error">{error}</div> : null}
      </article>
      <article className="panel">
        <div className="mini-list">
          {visibleModels.map((model) => <div key={model.id} className="mini-list-item"><strong>{model.name}</strong><div className="inline-actions"><button type="button" className="ghost-button dark" onClick={() => void updateModel(model.id, model.name, model.brandId, model.segmentId)}>Duzenle</button><button type="button" className="ghost-button danger" onClick={() => void deleteModel(model.id)}>Sil</button></div></div>)}
        </div>
      </article>
    </section>
  );
}
