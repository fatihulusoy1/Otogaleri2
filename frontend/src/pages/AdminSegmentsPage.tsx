import { useEffect, useState } from "react";
import { PageHeader } from "../components/PageHeader";
import { api } from "../lib/api";
import { useAuth } from "../state/AuthContext";
import type { LookupOption } from "../types";

export function AdminSegmentsPage() {
  const { session } = useAuth();
  const [segments, setSegments] = useState<LookupOption[]>([]);
  const [name, setName] = useState("");
  const [error, setError] = useState("");

  async function loadSegments() {
    if (!session) return;
    try {
      setError("");
      const catalog = await api.adminGetCatalog(session.token);
      setSegments(catalog.segments);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Segmentler yuklenemedi.");
    }
  }

  useEffect(() => {
    void loadSegments();
  }, [session]);

  async function createSegment() {
    if (!session || !name.trim()) return;
    try {
      setError("");
      await api.adminCreateSegment(session.token, name);
      setName("");
      await loadSegments();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Segment eklenemedi.");
    }
  }

  async function updateSegment(segment: LookupOption) {
    if (!session) return;
    const nextName = window.prompt("Yeni segment adi", segment.name)?.trim();
    if (!nextName) return;
    try {
      setError("");
      await api.adminUpdateSegment(session.token, segment.id, nextName);
      await loadSegments();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Segment guncellenemedi.");
    }
  }

  async function deleteSegment(segmentId: string) {
    if (!session) return;
    try {
      setError("");
      await api.adminDeleteSegment(session.token, segmentId);
      await loadSegments();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Segment silinemedi.");
    }
  }

  return (
    <section className="page-stack">
      <PageHeader eyebrow="Super Admin" title="Segment yonetimi" description="Tum kullanicilarin gorecegi ortak segment listesini buradan yonetin." />
      <article className="panel data-form">
        <input value={name} onChange={(event) => setName(event.target.value)} placeholder="Ornek: Crossover" />
        <button type="button" className="primary-button" onClick={() => void createSegment()}>Segment ekle</button>
        {error ? <div className="alert error">{error}</div> : null}
      </article>
      <article className="panel">
        <div className="mini-list">
          {segments.map((segment) => <div key={segment.id} className="mini-list-item"><strong>{segment.name}</strong><div className="inline-actions"><button type="button" className="ghost-button dark" onClick={() => void updateSegment(segment)}>Duzenle</button><button type="button" className="ghost-button danger" onClick={() => void deleteSegment(segment.id)}>Sil</button></div></div>)}
        </div>
      </article>
    </section>
  );
}
