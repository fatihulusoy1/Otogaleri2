interface StatCardProps {
  label: string;
  value: string;
  meta: string;
  tone?: "primary" | "success" | "danger" | "neutral";
}

export function StatCard({ label, value, meta, tone = "neutral" }: StatCardProps) {
  return (
    <article className={`stat-card stat-card-${tone}`}>
      <span>{label}</span>
      <strong>{value}</strong>
      <small>{meta}</small>
    </article>
  );
}
