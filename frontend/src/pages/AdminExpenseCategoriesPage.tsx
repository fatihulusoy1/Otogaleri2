import { useEffect, useState } from "react";
import { PageHeader } from "../components/PageHeader";
import { api } from "../lib/api";
import { useAuth } from "../state/AuthContext";
import type { ExpenseCategory, ExpenseCategoryType } from "../types";

function CategorySection({
  title,
  description,
  categories,
  draftName,
  onDraftNameChange,
  onCreate,
  onRename,
  onRemove
}: {
  title: string;
  description: string;
  categories: ExpenseCategory[];
  draftName: string;
  onDraftNameChange: (value: string) => void;
  onCreate: () => void;
  onRename: (id: string, name: string) => void;
  onRemove: (id: string) => void;
}) {
  return (
    <article className="panel page-stack">
      <div className="panel-heading">
        <h2>{title}</h2>
        <p>{description}</p>
      </div>

      <div className="inline-form">
        <input value={draftName} onChange={(event) => onDraftNameChange(event.target.value)} placeholder="Yeni kategori adi" />
        <button type="button" className="primary-button" onClick={onCreate}>
          Kategori ekle
        </button>
      </div>

      <div className="mini-list">
        {categories.map((item) => (
          <div key={item.id} className="mini-list-item">
            <strong>{item.name}</strong>
            <div className="inline-actions">
              <button type="button" className="ghost-button dark" onClick={() => onRename(item.id, item.name)}>
                Duzenle
              </button>
              <button type="button" className="ghost-button danger" onClick={() => onRemove(item.id)}>
                Sil
              </button>
            </div>
          </div>
        ))}

        {categories.length === 0 ? <div className="empty-panel">Bu grupta henuz kategori yok.</div> : null}
      </div>
    </article>
  );
}

export function AdminExpenseCategoriesPage() {
  const { session } = useAuth();
  const [vehicleCategories, setVehicleCategories] = useState<ExpenseCategory[]>([]);
  const [generalCategories, setGeneralCategories] = useState<ExpenseCategory[]>([]);
  const [vehicleName, setVehicleName] = useState("");
  const [generalName, setGeneralName] = useState("");
  const [error, setError] = useState("");

  async function loadCategories() {
    if (!session) {
      return;
    }

    try {
      const [vehicleData, generalData] = await Promise.all([
        api.adminGetExpenseCategories(session.token, 1),
        api.adminGetExpenseCategories(session.token, 2)
      ]);
      setVehicleCategories(vehicleData);
      setGeneralCategories(generalData);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Kategoriler yuklenemedi.");
    }
  }

  useEffect(() => {
    void loadCategories();
  }, [session]);

  async function createCategory(name: string, categoryType: ExpenseCategoryType, clear: () => void) {
    if (!session || !name.trim()) {
      return;
    }

    try {
      setError("");
      await api.adminCreateExpenseCategory(session.token, name.trim(), categoryType);
      clear();
      await loadCategories();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Kategori eklenemedi.");
    }
  }

  async function renameCategory(id: string, currentName: string) {
    if (!session) {
      return;
    }

    const name = window.prompt("Yeni kategori adi", currentName)?.trim();
    if (!name) {
      return;
    }

    try {
      setError("");
      await api.adminUpdateExpenseCategory(session.token, id, name);
      await loadCategories();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Kategori guncellenemedi.");
    }
  }

  async function removeCategory(id: string) {
    if (!session) {
      return;
    }

    try {
      setError("");
      await api.adminDeleteExpenseCategory(session.token, id);
      await loadCategories();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Kategori silinemedi.");
    }
  }

  return (
    <section className="page-stack">
      <PageHeader
        eyebrow="Super Admin"
        title="Masraf kategori yonetimi"
        description="Arac masraflari ve genel giderlerde kullanilan ortak kategori havuzlarini buradan yonetin."
      />

      {error ? <div className="alert error">{error}</div> : null}

      <div className="content-grid two-columns">
        <CategorySection
          title="Arac masraf kategorileri"
          description="Ekspertiz, bakim, tamir gibi araca bagli tum giderler bu listeden secilir."
          categories={vehicleCategories}
          draftName={vehicleName}
          onDraftNameChange={setVehicleName}
          onCreate={() => void createCategory(vehicleName, 1, () => setVehicleName(""))}
          onRename={(id, name) => void renameCategory(id, name)}
          onRemove={(id) => void removeCategory(id)}
        />

        <CategorySection
          title="Genel gider kategorileri"
          description="Kira, aidat, guvenlik ve benzeri operasyon giderleri bu listeden secilir."
          categories={generalCategories}
          draftName={generalName}
          onDraftNameChange={setGeneralName}
          onCreate={() => void createCategory(generalName, 2, () => setGeneralName(""))}
          onRename={(id, name) => void renameCategory(id, name)}
          onRemove={(id) => void removeCategory(id)}
        />
      </div>
    </section>
  );
}
