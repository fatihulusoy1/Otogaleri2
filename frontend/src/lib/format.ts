import type { FinancialDocumentType, PaymentMethod, ReceivablePayableStatus, VehicleStatus } from "../types";

export const currency = new Intl.NumberFormat("tr-TR", {
  style: "currency",
  currency: "TRY",
  maximumFractionDigits: 0
});

export const dateTime = new Intl.DateTimeFormat("tr-TR", {
  dateStyle: "medium",
  timeStyle: "short"
});

export const dateOnly = new Intl.DateTimeFormat("tr-TR", {
  dateStyle: "medium"
});

export function formatCurrency(value: number): string {
  return currency.format(value ?? 0);
}

export function formatDate(value: string): string {
  return dateTime.format(new Date(value));
}

export function formatDateOnly(value: string): string {
  return dateOnly.format(new Date(value));
}

export function toDateTimeLocalValue(value: string): string {
  const date = new Date(value);
  const pad = (part: number) => part.toString().padStart(2, "0");

  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

export function toIsoFromLocalValue(value: string): string {
  return new Date(value).toISOString();
}

export function formatPercent(value: number): string {
  return `%${(value ?? 0).toFixed(1)}`;
}

export function getVehicleStatusLabel(status: VehicleStatus): string {
  switch (status) {
    case 1:
      return "Stokta";
    case 2:
      return "Satıldı";
    case 3:
      return "Rezerve";
    case 4:
      return "Serviste";
    default:
      return "Bilinmiyor";
  }
}

export function getPaymentMethodLabel(method: PaymentMethod): string {
  switch (method) {
    case 1:
      return "Nakit";
    case 2:
      return "Havale";
    case 3:
      return "Kredi Kartı";
    case 4:
      return "Takas";
    case 5:
      return "Çek";
    case 6:
      return "Senet";
    case 7:
      return "Vadeli";
    default:
      return "Bilinmiyor";
  }
}

export function getStatusTone(status: VehicleStatus): string {
  switch (status) {
    case 1:
      return "success";
    case 2:
      return "neutral";
    case 3:
      return "warning";
    case 4:
      return "danger";
    default:
      return "neutral";
  }
}

export function getFinancialDocumentTypeLabel(type: FinancialDocumentType): string {
  switch (type) {
    case 1:
      return "Açık hesap";
    case 2:
      return "Çek";
    case 3:
      return "Senet";
    default:
      return "Bilinmiyor";
  }
}

export function getReceivablePayableStatusLabel(status: ReceivablePayableStatus): string {
  switch (status) {
    case 1:
      return "Açık";
    case 2:
      return "Kısmen ödendi";
    case 3:
      return "Kapandı";
    case 4:
      return "Gecikmiş";
    default:
      return "Bilinmiyor";
  }
}
