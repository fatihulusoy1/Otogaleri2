import type {
  AdminCatalogLookups,
  AdminSubscriptionPlan,
  AdminTenant,
  AdminUser,
  BrokeredConsignment,
  StockConsignment,
  ReceivablePayable,
  ExpenseCategory,
  SaveSubscriptionPlanRequest,
  Transaction,
  UpdateTenantSubscriptionRequest,
} from "../types";
import type {
  AuthResponse,
  CompleteStockConsignmentSaleRequest,
  CompleteVehicleSaleRequest,
  CreateBrokeredConsignmentRequest,
  CreatePurchaseRequest,
  CreateStockConsignmentRequest,
  CreateVehicleExpenseRequest,
  DashboardSummary,
  LoginRequest,
  VehicleLookups,
  PurchaseRecord,
  RegisterRequest,
  RefreshTokenRequest,
  UpdateBrokeredConsignmentRequest,
  Vehicle,
  VehicleExpense,
  VehicleSale,
  UpdatePurchaseRequest,
  UpdateStockConsignmentRequest,
  UpdateVehicleExpenseRequest,
  UpdateVehicleSaleRequest,
  ExpenseCategoryType,
  TransactionType,
  PaymentMethod
} from "../types";

const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/$/, "") ?? "";
const unauthorizedEventName = "autogallery:unauthorized";

async function readApiError(response: Response): Promise<string> {
  try {
    const payload = await response.json();
    if (typeof payload === "string") {
      return payload;
    }

    return payload.title ?? payload.message ?? payload.detail ?? "Bir hata oluştu.";
  } catch {
    return "Bir hata oluştu.";
  }
}

async function request<T>(path: string, init?: RequestInit, token?: string): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    cache: "no-store",
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(init?.headers ?? {})
    }
  });

  if (!response.ok) {
    if (response.status === 401 && typeof window !== "undefined") {
      window.dispatchEvent(new CustomEvent(unauthorizedEventName));
    }

    throw new Error(await readApiError(response));
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export const api = {
  login(payload: LoginRequest) {
    return request<AuthResponse>("/api/Auth/login", {
      method: "POST",
      body: JSON.stringify(payload)
    });
  },
  register(payload: RegisterRequest) {
    return request<AuthResponse>("/api/Auth/register", {
      method: "POST",
      body: JSON.stringify(payload)
    });
  },
  refreshToken(payload: RefreshTokenRequest) {
    return request<AuthResponse>("/api/Auth/refresh-token", {
      method: "POST",
      body: JSON.stringify(payload)
    });
  },
  getDashboard(token: string) {
    return request<DashboardSummary>("/api/Dashboard/summary", undefined, token);
  },
  getVehicles(token: string) {
    return request<Vehicle[]>("/api/Vehicles", undefined, token);
  },
  getVehicleLookups(token: string) {
    return request<VehicleLookups>("/api/Vehicles/lookups", undefined, token);
  },
  getPurchases(token: string) {
    return request<PurchaseRecord[]>("/api/Vehicles/purchases", undefined, token);
  },
  createPurchase(token: string, payload: CreatePurchaseRequest) {
    return request<PurchaseRecord>(
      "/api/Vehicles/purchases",
      {
        method: "POST",
        body: JSON.stringify(payload)
      },
      token
    );
  },
  updatePurchase(token: string, vehicleId: string, payload: UpdatePurchaseRequest) {
    return request<PurchaseRecord>(
      `/api/Vehicles/purchases/${vehicleId}`,
      {
        method: "PUT",
        body: JSON.stringify(payload)
      },
      token
    );
  },
  deletePurchase(token: string, vehicleId: string) {
    return request<void>(`/api/Vehicles/purchases/${vehicleId}`, { method: "DELETE" }, token);
  },
  getExpenses(token: string, vehicleId?: string) {
    const suffix = vehicleId ? `?vehicleId=${vehicleId}` : "";
    return request<VehicleExpense[]>(`/api/Vehicles/expenses${suffix}`, undefined, token);
  },
  createExpense(token: string, vehicleId: string, payload: CreateVehicleExpenseRequest) {
    return request<VehicleExpense>(
      `/api/Vehicles/${vehicleId}/expenses`,
      {
        method: "POST",
        body: JSON.stringify(payload)
      },
      token
    );
  },
  updateExpense(token: string, expenseId: string, payload: UpdateVehicleExpenseRequest) {
    return request<VehicleExpense>(
      `/api/Vehicles/expenses/${expenseId}`,
      {
        method: "PUT",
        body: JSON.stringify(payload)
      },
      token
    );
  },
  deleteExpense(token: string, expenseId: string) {
    return request<void>(`/api/Vehicles/expenses/${expenseId}`, { method: "DELETE" }, token);
  },
  getSales(token: string) {
    return request<VehicleSale[]>("/api/Vehicles/sales", undefined, token);
  },
  getExpenseCategories(token: string, categoryType?: ExpenseCategoryType) {
    const suffix = categoryType ? `?categoryType=${categoryType}` : "";
    return request<ExpenseCategory[]>(`/api/Finance/expense-categories${suffix}`, undefined, token);
  },
  getTransactions(token: string) {
    return request<Transaction[]>("/api/Finance/transactions", undefined, token);
  },
  createTransaction(
    token: string,
    payload: {
      type: TransactionType;
      amount: number;
      transactionDate: string;
      description: string;
      paymentMethod: PaymentMethod;
      categoryId: string | null;
      relatedEntityId: string | null;
      relatedEntityType: string | null;
    }
  ) {
    return request<Transaction>("/api/Finance/transactions", {
      method: "POST",
      body: JSON.stringify(payload)
    }, token);
  },
  updateTransaction(
    token: string,
    transactionId: string,
    payload: {
      type: TransactionType;
      amount: number;
      transactionDate: string;
      description: string;
      paymentMethod: PaymentMethod;
      categoryId: string | null;
      relatedEntityId: string | null;
      relatedEntityType: string | null;
    }
  ) {
    return request<Transaction>(`/api/Finance/transactions/${transactionId}`, {
      method: "PUT",
      body: JSON.stringify(payload)
    }, token);
  },
  deleteTransaction(token: string, transactionId: string) {
    return request<void>(`/api/Finance/transactions/${transactionId}`, { method: "DELETE" }, token);
  },
  completeSale(token: string, vehicleId: string, payload: CompleteVehicleSaleRequest) {
    return request<VehicleSale>(
      `/api/Vehicles/${vehicleId}/sales`,
      {
        method: "POST",
        body: JSON.stringify(payload)
      },
      token
    );
  },
  updateSale(token: string, vehicleId: string, payload: UpdateVehicleSaleRequest) {
    return request<VehicleSale>(
      `/api/Vehicles/${vehicleId}/sales`,
      {
        method: "PUT",
        body: JSON.stringify(payload)
      },
      token
    );
  },
  deleteSale(token: string, vehicleId: string) {
    return request<void>(`/api/Vehicles/${vehicleId}/sales`, { method: "DELETE" }, token);
  },
  adminGetTenants(token: string) {
    return request<AdminTenant[]>("/api/admin/tenants", undefined, token);
  },
  adminGetUsers(token: string, tenantId?: string) {
    const suffix = tenantId ? `?tenantId=${tenantId}` : "";
    return request<AdminUser[]>(`/api/admin/users${suffix}`, undefined, token);
  },
  adminUpdateTenantStatus(token: string, tenantId: string, isActive: boolean) {
    return request<void>(`/api/admin/tenants/${tenantId}/status`, {
      method: "PATCH",
      body: JSON.stringify({ isActive })
    }, token);
  },
  adminUpdateUserStatus(token: string, userId: string, isActive: boolean) {
    return request<void>(`/api/admin/users/${userId}/status`, {
      method: "PATCH",
      body: JSON.stringify({ isActive })
    }, token);
  },
  adminUpdateTenantSubscription(token: string, tenantId: string, payload: UpdateTenantSubscriptionRequest) {
    return request<void>(`/api/admin/tenants/${tenantId}/subscription`, {
      method: "PUT",
      body: JSON.stringify(payload)
    }, token);
  },
  adminDeleteTenant(token: string, tenantId: string) {
    return request<void>(`/api/admin/tenants/${tenantId}`, { method: "DELETE" }, token);
  },
  adminGetPlans(token: string) {
    return request<AdminSubscriptionPlan[]>("/api/admin/plans", undefined, token);
  },
  adminCreatePlan(token: string, payload: SaveSubscriptionPlanRequest) {
    return request<AdminSubscriptionPlan>("/api/admin/plans", {
      method: "POST",
      body: JSON.stringify(payload)
    }, token);
  },
  adminUpdatePlan(token: string, id: string, payload: SaveSubscriptionPlanRequest) {
    return request<AdminSubscriptionPlan>(`/api/admin/plans/${id}`, {
      method: "PUT",
      body: JSON.stringify(payload)
    }, token);
  },
  adminGetCatalog(token: string) {
    return request<AdminCatalogLookups>("/api/admin/catalog", undefined, token);
  },
  adminCreateSegment(token: string, name: string) {
    return request<{ id: string; name: string }>("/api/admin/catalog/segments", {
      method: "POST",
      body: JSON.stringify({ name })
    }, token);
  },
  adminUpdateSegment(token: string, id: string, name: string) {
    return request<{ id: string; name: string }>(`/api/admin/catalog/segments/${id}`, {
      method: "PUT",
      body: JSON.stringify({ name })
    }, token);
  },
  adminDeleteSegment(token: string, id: string) {
    return request<void>(`/api/admin/catalog/segments/${id}`, { method: "DELETE" }, token);
  },
  adminCreateBrand(token: string, name: string) {
    return request<{ id: string; name: string }>("/api/admin/catalog/brands", {
      method: "POST",
      body: JSON.stringify({ name })
    }, token);
  },
  adminUpdateBrand(token: string, id: string, name: string) {
    return request<{ id: string; name: string }>(`/api/admin/catalog/brands/${id}`, {
      method: "PUT",
      body: JSON.stringify({ name })
    }, token);
  },
  adminDeleteBrand(token: string, id: string) {
    return request<void>(`/api/admin/catalog/brands/${id}`, { method: "DELETE" }, token);
  },
  adminCreateModel(token: string, payload: { brandId: string; segmentId: string | null; name: string; }) {
    return request<{ id: string; name: string; brandId: string; segmentId: string | null }>("/api/admin/catalog/models", {
      method: "POST",
      body: JSON.stringify(payload)
    }, token);
  },
  adminUpdateModel(token: string, id: string, payload: { brandId: string; segmentId: string | null; name: string; }) {
    return request<{ id: string; name: string; brandId: string; segmentId: string | null }>(`/api/admin/catalog/models/${id}`, {
      method: "PUT",
      body: JSON.stringify(payload)
    }, token);
  },
  adminDeleteModel(token: string, id: string) {
    return request<void>(`/api/admin/catalog/models/${id}`, { method: "DELETE" }, token);
  },
  adminGetExpenseCategories(token: string, categoryType?: ExpenseCategoryType) {
    const suffix = categoryType ? `?categoryType=${categoryType}` : "";
    return request<ExpenseCategory[]>(`/api/admin/expense-categories${suffix}`, undefined, token);
  },
  adminCreateExpenseCategory(token: string, name: string, categoryType: ExpenseCategoryType) {
    return request<ExpenseCategory>("/api/admin/expense-categories", {
      method: "POST",
      body: JSON.stringify({ name, categoryType })
    }, token);
  },
  adminUpdateExpenseCategory(token: string, id: string, name: string) {
    return request<ExpenseCategory>(`/api/admin/expense-categories/${id}`, {
      method: "PUT",
      body: JSON.stringify({ name })
    }, token);
  },
  adminDeleteExpenseCategory(token: string, id: string) {
    return request<void>(`/api/admin/expense-categories/${id}`, { method: "DELETE" }, token);
  },
  getReceivablePayables(token: string, type: 1 | 2) {
    return request<ReceivablePayable[]>(`/api/Finance/receivables-payables?type=${type}`, undefined, token);
  },
  settleReceivablePayable(token: string, id: string, settlementDate: string) {
    return request<ReceivablePayable>(`/api/Finance/receivables-payables/${id}/settle`, {
      method: "POST",
      body: JSON.stringify({ settlementDate })
    }, token);
  },
  partialSettleReceivablePayable(token: string, id: string, amount: number, settlementDate: string) {
    return request<ReceivablePayable>(`/api/Finance/receivables-payables/${id}/partial-settlement`, {
      method: "POST",
      body: JSON.stringify({ amount, settlementDate })
    }, token);
  },
  markReceivablePayableOverdue(token: string, id: string) {
    return request<ReceivablePayable>(`/api/Finance/receivables-payables/${id}/mark-overdue`, { method: "POST" }, token);
  },
  reopenReceivablePayable(token: string, id: string) {
    return request<ReceivablePayable>(`/api/Finance/receivables-payables/${id}/reopen`, { method: "POST" }, token);
  },
  getBrokeredConsignments(token: string) {
    return request<BrokeredConsignment[]>("/api/Consignments/brokered", undefined, token);
  },
  createBrokeredConsignment(token: string, payload: CreateBrokeredConsignmentRequest) {
    return request<BrokeredConsignment>("/api/Consignments/brokered", {
      method: "POST",
      body: JSON.stringify(payload)
    }, token);
  },
  updateBrokeredConsignment(token: string, id: string, payload: UpdateBrokeredConsignmentRequest) {
    return request<BrokeredConsignment>(`/api/Consignments/brokered/${id}`, {
      method: "PUT",
      body: JSON.stringify(payload)
    }, token);
  },
  deleteBrokeredConsignment(token: string, id: string) {
    return request<void>(`/api/Consignments/brokered/${id}`, { method: "DELETE" }, token);
  },
  getStockConsignments(token: string) {
    return request<StockConsignment[]>("/api/Consignments/stock", undefined, token);
  },
  createStockConsignment(token: string, payload: CreateStockConsignmentRequest) {
    return request<StockConsignment>("/api/Consignments/stock", {
      method: "POST",
      body: JSON.stringify(payload)
    }, token);
  },
  updateStockConsignment(token: string, id: string, payload: UpdateStockConsignmentRequest) {
    return request<StockConsignment>(`/api/Consignments/stock/${id}`, {
      method: "PUT",
      body: JSON.stringify(payload)
    }, token);
  },
  deleteStockConsignment(token: string, id: string) {
    return request<void>(`/api/Consignments/stock/${id}`, { method: "DELETE" }, token);
  },
  completeStockConsignmentSale(token: string, id: string, payload: CompleteStockConsignmentSaleRequest) {
    return request<StockConsignment>(`/api/Consignments/stock/${id}/sale`, {
      method: "POST",
      body: JSON.stringify(payload)
    }, token);
  },
  updateStockConsignmentSale(token: string, id: string, payload: CompleteStockConsignmentSaleRequest) {
    return request<StockConsignment>(`/api/Consignments/stock/${id}/sale`, {
      method: "PUT",
      body: JSON.stringify(payload)
    }, token);
  },
  deleteStockConsignmentSale(token: string, id: string) {
    return request<void>(`/api/Consignments/stock/${id}/sale`, { method: "DELETE" }, token);
  }
};

export { unauthorizedEventName };
