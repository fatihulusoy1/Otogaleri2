import { useEffect, useState } from "react";
import { PageHeader } from "../components/PageHeader";
import { api } from "../lib/api";
import { useAuth } from "../state/AuthContext";
import type { LookupOption } from "../types";

export function AdminBrandsPage() {
  const { session } = useAuth();
  const [brands, setBrands] = useState<LookupOption[]>([]);
  const [name, setName] = useState("");
  const [error, setError] = useState("");

  async function loadBrands() {
    if (!session) return;
    try {
      setError("");
      const catalog = await api.adminGetCatalog(session.token);
      setBrands(catalog.brands);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Markalar yuklenemedi.");
    }
  }

  useEffect(() => {
    void loadBrands();
  }, [session]);

  async function createBrand() {
    if (!session || !name.trim()) return;
    try {
      setError("");
      await api.adminCreateBrand(session.token, name);
      setName("");
      await loadBrands();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Marka eklenemedi.");
    }
  }

  async function updateBrand(brand: LookupOption) {
    if (!session) return;
    const nextName = window.prompt("Yeni marka adi", brand.name)?.trim();
    if (!nextName) return;
    try {
      setError("");
      await api.adminUpdateBrand(session.token, brand.id, nextName);
      await loadBrands();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Marka guncellenemedi.");
    }
  }

  async function deleteBrand(brandId: string) {
    if (!session) return;
    try {
      setError("");
      await api.adminDeleteBrand(session.token, brandId);
      await loadBrands();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Marka silinemedi.");
    }
  }

  return (
    <section className="page-stack">
      <PageHeader eyebrow="Super Admin" title="Marka yonetimi" description="Tum kullanicilarin gorecegi ortak marka listesini buradan yonetin." />
      <article className="panel data-form">
        <input value={name} onChange={(event) => setName(event.target.value)} placeholder="Ornek: Peugeot" />
        <button type="button" className="primary-button" onClick={() => void createBrand()}>Marka ekle</button>
        {error ? <div className="alert error">{error}</div> : null}
      </article>
      <article className="panel">
        <div className="mini-list">
          {brands.map((brand) => <div key={brand.id} className="mini-list-item"><strong>{brand.name}</strong><div className="inline-actions"><button type="button" className="ghost-button dark" onClick={() => void updateBrand(brand)}>Duzenle</button><button type="button" className="ghost-button danger" onClick={() => void deleteBrand(brand.id)}>Sil</button></div></div>)}
        </div>
      </article>
    </section>
  );
}
