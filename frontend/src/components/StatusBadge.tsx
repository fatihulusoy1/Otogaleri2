import { getStatusTone, getVehicleStatusLabel } from "../lib/format";
import type { VehicleStatus } from "../types";

export function StatusBadge({ status }: { status: VehicleStatus }) {
  return (
    <span className={`status-badge status-${getStatusTone(status)}`}>
      {getVehicleStatusLabel(status)}
    </span>
  );
}
