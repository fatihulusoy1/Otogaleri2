import type {
  AdminCatalogLookups,
  AdminSubscriptionPlan,
  AdminTenant,
  AdminUser,
  CreateTenantRequest,
  CreateUserRequest,
  UpdateTenantRequest,
  UpdateUserRequest,
  BrokeredConsignment,
  StockConsignment,
  ReceivablePayable,
  ExpenseCategory,
  Transaction,
  SubscriptionPlanPublic,
  SubscriptionStatus,
  BillingCycle,
  Profile,
  UpdateProfileRequest,
  ChangePasswordRequest,
  TenantUser,
  CreateTenantUserRequest,
  UpdateTenantUserRequest,
  PlanRequest,
  TenantActivity,
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
  UpdateBrokeredConsignmentRequest,
  Vehicle,
  VehiclePhoto,
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

// Veritabanindaki FileUrl ya mutlak (R2) ya da goreli ("/uploads/..") gelir.
// Mutlaksa oldugu gibi kullan, goreli ise API tabani ile birlestir (dev'de vite proxy /uploads'i yonlendirir).
export function resolveFileUrl(fileUrl: string): string {
  if (/^https?:\/\//i.test(fileUrl)) {
    return fileUrl;
  }
  return `${apiBaseUrl}${fileUrl}`;
}
const unauthorizedEventName = "autogallery:unauthorized";
const limitExceededEventName = "autogallery:limit-exceeded";

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

    let payload: unknown = null;
    try {
      payload = await response.json();
    } catch {
      payload = null;
    }

    const data = payload as { code?: string; title?: string; message?: string; detail?: string } | string | null;
    const message =
      typeof data === "string"
        ? data
        : data?.title ?? data?.message ?? data?.detail ?? "Bir hata oluştu.";

    if (data && typeof data === "object" && data.code === "limit_exceeded" && typeof window !== "undefined") {
      window.dispatchEvent(new CustomEvent(limitExceededEventName, { detail: message }));
    }

    throw new Error(message);
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
  verifyTwoFactor(email: string, code: string) {
    return request<AuthResponse>("/api/Auth/verify-2fa", {
      method: "POST",
      body: JSON.stringify({ email, code })
    });
  },
  getEmailEnabled() {
    return request<{ enabled: boolean }>("/api/Auth/email-enabled");
  },
  forgotPassword(email: string) {
    return request<void>("/api/Auth/forgot-password", {
      method: "POST",
      body: JSON.stringify({ email })
    });
  },
  resetPassword(token: string, newPassword: string) {
    return request<void>("/api/Auth/reset-password", {
      method: "POST",
      body: JSON.stringify({ token, newPassword })
    });
  },
  setTwoFactor(token: string, enabled: boolean) {
    return request<Profile>("/api/Auth/two-factor", {
      method: "POST",
      body: JSON.stringify({ enabled })
    }, token);
  },
  getProfile(token: string) {
    return request<Profile>("/api/Auth/me", undefined, token);
  },
  updateProfile(token: string, payload: UpdateProfileRequest) {
    return request<Profile>("/api/Auth/me", {
      method: "PUT",
      body: JSON.stringify(payload)
    }, token);
  },
  changePassword(token: string, payload: ChangePasswordRequest) {
    return request<void>("/api/Auth/change-password", {
      method: "POST",
      body: JSON.stringify(payload)
    }, token);
  },
  tenantGetUsers(token: string) {
    return request<TenantUser[]>("/api/tenant/users", undefined, token);
  },
  tenantCreateUser(token: string, payload: CreateTenantUserRequest) {
    return request<TenantUser>("/api/tenant/users", {
      method: "POST",
      body: JSON.stringify(payload)
    }, token);
  },
  tenantUpdateUser(token: string, userId: string, payload: UpdateTenantUserRequest) {
    return request<TenantUser>(`/api/tenant/users/${userId}`, {
      method: "PUT",
      body: JSON.stringify(payload)
    }, token);
  },
  tenantSetUserStatus(token: string, userId: string, isActive: boolean) {
    return request<void>(`/api/tenant/users/${userId}/status`, {
      method: "PATCH",
      body: JSON.stringify({ isActive })
    }, token);
  },
  tenantSetUserPassword(token: string, userId: string, password: string) {
    return request<void>(`/api/tenant/users/${userId}/password`, {
      method: "POST",
      body: JSON.stringify({ password })
    }, token);
  },
  tenantDeleteUser(token: string, userId: string) {
    return request<void>(`/api/tenant/users/${userId}`, { method: "DELETE" }, token);
  },
  getPublicPlans() {
    return request<SubscriptionPlanPublic[]>("/api/subscription/plans");
  },
  getSubscription(token: string) {
    return request<SubscriptionStatus>("/api/subscription", undefined, token);
  },
  checkoutSubscription(token: string, planId: string, billingCycle: BillingCycle) {
    return request<SubscriptionStatus>("/api/subscription/checkout", {
      method: "POST",
      body: JSON.stringify({ planId, billingCycle })
    }, token);
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
  getVehiclePhotos(token: string, vehicleId: string) {
    return request<VehiclePhoto[]>(`/api/Vehicles/${vehicleId}/photos`, undefined, token);
  },
  async uploadVehiclePhotos(token: string, vehicleId: string, files: File[]) {
    const formData = new FormData();
    files.forEach((file) => formData.append("files", file, file.name));

    // FormData icin Content-Type'i tarayici boundary ile kendisi ayarlamali; request() JSON zorladigi icin burada elle cagiriyoruz.
    const response = await fetch(`${apiBaseUrl}/api/Vehicles/${vehicleId}/photos`, {
      method: "POST",
      cache: "no-store",
      headers: { Authorization: `Bearer ${token}` },
      body: formData
    });

    if (!response.ok) {
      if (response.status === 401 && typeof window !== "undefined") {
        window.dispatchEvent(new CustomEvent(unauthorizedEventName));
      }
      let message = "Fotograflar yuklenemedi.";
      try {
        const payload = (await response.json()) as { title?: string; message?: string; detail?: string };
        message = payload?.title ?? payload?.message ?? payload?.detail ?? message;
      } catch {
        // ignore
      }
      throw new Error(message);
    }

    return (await response.json()) as VehiclePhoto[];
  },
  deleteVehiclePhoto(token: string, photoId: string) {
    return request<void>(`/api/Vehicles/photos/${photoId}`, { method: "DELETE" }, token);
  },
  setVehiclePhotoCover(token: string, photoId: string) {
    return request<VehiclePhoto[]>(`/api/Vehicles/photos/${photoId}/cover`, { method: "PUT" }, token);
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
  adminGetSubscriptionPlans(token: string) {
    return request<AdminSubscriptionPlan[]>("/api/admin/subscription-plans", undefined, token);
  },
  adminCreatePlan(token: string, payload: PlanRequest) {
    return request<AdminSubscriptionPlan>("/api/admin/subscription-plans", {
      method: "POST",
      body: JSON.stringify(payload)
    }, token);
  },
  adminUpdatePlan(token: string, planId: string, payload: PlanRequest) {
    return request<AdminSubscriptionPlan>(`/api/admin/subscription-plans/${planId}`, {
      method: "PUT",
      body: JSON.stringify(payload)
    }, token);
  },
  adminDeletePlan(token: string, planId: string) {
    return request<void>(`/api/admin/subscription-plans/${planId}`, { method: "DELETE" }, token);
  },
  tenantGetActivities(token: string) {
    return request<TenantActivity[]>("/api/tenant/activities", undefined, token);
  },
  adminGetUsers(token: string, tenantId?: string) {
    const suffix = tenantId ? `?tenantId=${tenantId}` : "";
    return request<AdminUser[]>(`/api/admin/users${suffix}`, undefined, token);
  },
  adminCreateTenant(token: string, payload: CreateTenantRequest) {
    return request<AdminTenant>("/api/admin/tenants", {
      method: "POST",
      body: JSON.stringify(payload)
    }, token);
  },
  adminUpdateTenant(token: string, tenantId: string, payload: UpdateTenantRequest) {
    return request<AdminTenant>(`/api/admin/tenants/${tenantId}`, {
      method: "PUT",
      body: JSON.stringify(payload)
    }, token);
  },
  adminDeleteTenant(token: string, tenantId: string) {
    return request<void>(`/api/admin/tenants/${tenantId}`, { method: "DELETE" }, token);
  },
  adminGetTenantActivities(token: string, tenantId: string) {
    return request<TenantActivity[]>(`/api/admin/tenants/${tenantId}/activities`, undefined, token);
  },
  adminCreateUser(token: string, payload: CreateUserRequest) {
    return request<AdminUser>("/api/admin/users", {
      method: "POST",
      body: JSON.stringify(payload)
    }, token);
  },
  adminUpdateUser(token: string, userId: string, payload: UpdateUserRequest) {
    return request<AdminUser>(`/api/admin/users/${userId}`, {
      method: "PUT",
      body: JSON.stringify(payload)
    }, token);
  },
  adminDeleteUser(token: string, userId: string) {
    return request<void>(`/api/admin/users/${userId}`, { method: "DELETE" }, token);
  },
  adminSetUserPassword(token: string, userId: string, password: string) {
    return request<void>(`/api/admin/users/${userId}/password`, {
      method: "POST",
      body: JSON.stringify({ password })
    }, token);
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

export { unauthorizedEventName, limitExceededEventName };
