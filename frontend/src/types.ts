export interface AuthResponse {
  token: string;
  refreshToken: string;
  expiry: string;
  tenantId: string;
  tenantName: string;
  userFullName: string;
  isSuperAdmin: boolean;
  subscriptionEndDate: string;
  subscriptionExpired: boolean;
  isTenantAdmin: boolean;
  requiresTwoFactor: boolean;
}

export interface TenantUser {
  id: string;
  fullName: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  isTenantAdmin: boolean;
  isSuperAdmin: boolean;
  lastLoginAt: string | null;
}

export interface CreateTenantUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  isTenantAdmin: boolean;
}

export interface UpdateTenantUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  isTenantAdmin: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export type BillingCycle = 1 | 2;

export interface RegisterRequest extends LoginRequest {
  firstName: string;
  lastName: string;
  tenantName: string;
  subscriptionPlanId: string;
  billingCycle: BillingCycle;
}

export interface SubscriptionPlanPublic {
  id: string;
  name: string;
  description: string;
  monthlyPrice: number;
  yearlyPrice: number;
  maxUsers: number;
  maxVehicles: number;
}

export interface Profile {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  tenantId: string;
  tenantName: string;
  isSuperAdmin: boolean;
  roleLabel: string;
  createdAt: string;
  subscriptionEndDate: string;
  subscriptionExpired: boolean;
  lastLoginAt: string | null;
  twoFactorEnabled: boolean;
}

export interface UpdateProfileRequest {
  firstName: string;
  lastName: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface SubscriptionStatus {
  tenantId: string;
  tenantName: string;
  planId: string;
  planName: string;
  monthlyPrice: number;
  yearlyPrice: number;
  maxUsers: number;
  maxVehicles: number;
  currentUsers: number;
  currentVehicles: number;
  subscriptionEndDate: string;
  daysRemaining: number;
  isExpired: boolean;
}

export interface DashboardSummary {
  totalVehicles: number;
  inStockVehicles: number;
  soldVehicles: number;
  totalPurchaseCost: number;
  totalVehicleExpenseCost: number;
  totalCostWithExpenses: number;
  totalSalesRevenue: number;
  grossProfit: number;
  grossProfitMargin: number;
  currentStockPurchaseCost: number;
  currentStockExpenseCost: number;
  currentStockTotalCost: number;
  currentMonthPurchaseCost: number;
  currentMonthExpenseCost: number;
  currentMonthSalesRevenue: number;
}

export type VehicleStatus = 1 | 2 | 3 | 4;
export type VehicleOwnershipType = 1 | 2;
export type PaymentMethod = 1 | 2 | 3 | 4 | 5 | 6 | 7;
export type TransactionType = 1 | 2;
export type ExpenseCategoryType = 1 | 2;
export type ConsignmentStatus = 1 | 2 | 3 | 4;
export type ReceivablePayableType = 1 | 2;
export type ReceivablePayableSourceType = 1 | 2;
export type FinancialDocumentType = 1 | 2 | 3;
export type ReceivablePayableStatus = 1 | 2 | 3 | 4;

export interface Vehicle {
  id: string;
  plate: string;
  ownershipType: VehicleOwnershipType;
  consignmentId: string | null;
  segmentId: string | null;
  segment: string | null;
  brandId: string | null;
  brand: string;
  modelId: string | null;
  model: string;
  year: number;
  color: string;
  engineNumber: string;
  chassisNumber: string;
  purchaseDate: string;
  purchasePrice: number;
  totalExpenseCost: number;
  totalCost: number;
  targetSalePrice: number | null;
  actualSalePrice: number | null;
  saleDate: string | null;
  consignmentCommissionAmount: number | null;
  estimatedProfit: number | null;
  status: VehicleStatus;
  description: string | null;
  photos: VehiclePhoto[];
}

export interface VehiclePhoto {
  id: string;
  vehicleId: string;
  fileName: string;
  fileUrl: string;
  fileSize: number;
  contentType: string;
  sortOrder: number;
}

export interface PurchaseRecord {
  vehicleId: string;
  plate: string;
  ownershipType: VehicleOwnershipType;
  consignmentId: string | null;
  segmentId: string | null;
  segment: string | null;
  brandId: string | null;
  brand: string;
  modelId: string | null;
  model: string;
  year: number;
  color: string;
  engineNumber: string;
  chassisNumber: string;
  purchasePrice: number;
  paymentMethod: PaymentMethod;
  notaryRegistryNumber: string | null;
  counterpartyName: string | null;
  dueDate: string | null;
  documentNumber: string | null;
  installmentAmount: number | null;
  installmentCount: number | null;
  installmentIntervalMonths: number | null;
  tradePlate: string | null;
  tradeAmount: number | null;
  targetSalePrice: number | null;
  purchasedAt: string;
  description: string | null;
  status: VehicleStatus;
}

export interface LookupOption {
  id: string;
  name: string;
}

export interface VehicleModelLookup {
  id: string;
  name: string;
  brandId: string;
  segmentId: string | null;
}

export interface VehicleLookups {
  segments: LookupOption[];
  brands: LookupOption[];
  models: VehicleModelLookup[];
}

export interface VehicleSale {
  vehicleId: string;
  plate: string;
  vehicleDisplayName: string;
  purchasePrice: number;
  totalExpenseCost: number;
  totalCost: number;
  salePrice: number;
  profit: number;
  profitMargin: number;
  soldAt: string;
  paymentMethod: PaymentMethod;
  counterpartyName: string | null;
  dueDate: string | null;
  documentNumber: string | null;
  installmentAmount: number | null;
  installmentCount: number | null;
  installmentIntervalMonths: number | null;
  tradePlate: string | null;
  tradeAmount: number | null;
  notaryRegistryNumber: string | null;
}

export interface VehicleExpense {
  id: string;
  vehicleId: string;
  vehiclePlate: string;
  description: string;
  amount: number;
  expenseDate: string;
  categoryId: string | null;
  categoryName: string | null;
  paymentMethod: PaymentMethod;
}

export interface CreatePurchaseRequest {
  plate: string;
  segmentId: string;
  brandId: string;
  modelId: string;
  year: number;
  color: string;
  engineNumber: string;
  chassisNumber: string;
  purchaseDate: string;
  purchasePrice: number;
  paymentMethod: PaymentMethod;
  notaryRegistryNumber: string | null;
  counterpartyName: string | null;
  dueDate: string | null;
  documentNumber: string | null;
  installmentCount: number | null;
  installmentIntervalMonths: number | null;
  tradePlate: string | null;
  tradeAmount: number | null;
  targetSalePrice: number | null;
  description: string;
}

export interface UpdatePurchaseRequest extends CreatePurchaseRequest {}

export interface CreateVehicleExpenseRequest {
  description: string;
  amount: number;
  expenseDate: string;
  categoryId: string | null;
  paymentMethod: PaymentMethod;
}

export interface UpdateVehicleExpenseRequest extends CreateVehicleExpenseRequest {}

export interface CompleteVehicleSaleRequest {
  salePrice: number;
  saleDate: string;
  paymentMethod: PaymentMethod;
  notaryRegistryNumber: string | null;
  counterpartyName: string | null;
  dueDate: string | null;
  documentNumber: string | null;
  installmentCount: number | null;
  installmentIntervalMonths: number | null;
  tradePlate: string | null;
  tradeAmount: number | null;
  description: string;
}

export interface UpdateVehicleSaleRequest extends CompleteVehicleSaleRequest {}

export interface ExpenseCategory {
  id: string;
  name: string;
  categoryType: ExpenseCategoryType;
}

export interface Transaction {
  id: string;
  type: TransactionType;
  amount: number;
  transactionDate: string;
  description: string;
  paymentMethod: PaymentMethod;
  categoryId: string | null;
  categoryName: string | null;
  relatedEntityId: string | null;
  relatedEntityType: string | null;
}

export interface ReceivablePayable {
  id: string;
  type: ReceivablePayableType;
  sourceType: ReceivablePayableSourceType;
  sourceId: string;
  plate: string | null;
  counterpartyName: string;
  paymentMethod: PaymentMethod;
  documentType: FinancialDocumentType;
  documentNumber: string | null;
  issueDate: string;
  dueDate: string;
  originalAmount: number;
  remainingAmount: number;
  lastSettlementDate: string | null;
  status: ReceivablePayableStatus;
  description: string | null;
}

export interface AdminTenant {
  id: string;
  name: string;
  identifier: string | null;
  isActive: boolean;
  userCount: number;
  subscriptionPlanId: string;
  subscriptionPlanName: string;
  subscriptionEndDate: string;
}

export interface AdminSubscriptionPlan {
  id: string;
  name: string;
  description: string;
  monthlyPrice: number;
  yearlyPrice: number;
  maxUsers: number;
  maxVehicles: number;
  isActive: boolean;
}

export interface PlanRequest {
  name: string;
  description: string;
  monthlyPrice: number;
  yearlyPrice: number;
  maxUsers: number;
  maxVehicles: number;
  isActive: boolean;
}

export interface TenantActivity {
  id: string;
  type: number;
  typeLabel: string;
  description: string;
  amount: number | null;
  performedBy: string | null;
  createdAt: string;
}

export interface AdminUser {
  id: string;
  tenantId: string;
  tenantName: string;
  fullName: string;
  email: string;
  isActive: boolean;
  isSuperAdmin: boolean;
  lastLoginAt: string | null;
}

export interface CreateTenantRequest {
  name: string;
  identifier: string | null;
  subscriptionPlanId: string;
  subscriptionEndDate: string;
  adminFirstName: string;
  adminLastName: string;
  adminEmail: string;
  adminPassword: string;
}

export interface UpdateTenantRequest {
  name: string;
  identifier: string | null;
  subscriptionPlanId: string;
  subscriptionEndDate: string;
  isActive: boolean;
}

export interface CreateUserRequest {
  tenantId: string;
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  isSuperAdmin: boolean;
}

export interface UpdateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  isSuperAdmin: boolean;
}

export interface AdminCatalogLookups {
  segments: LookupOption[];
  brands: LookupOption[];
  models: VehicleModelLookup[];
}

export interface BrokeredConsignment {
  id: string;
  ownerName: string;
  ownerPhone: string | null;
  customerName: string | null;
  customerPhone: string | null;
  plate: string;
  segmentId: string | null;
  segment: string | null;
  brandId: string | null;
  brand: string;
  modelId: string | null;
  model: string;
  year: number;
  color: string;
  engineNumber: string;
  chassisNumber: string;
  consignmentDate: string;
  purchasePrice: number;
  commissionAmount: number | null;
  commissionRate: number | null;
  status: ConsignmentStatus;
  description: string | null;
}

export interface StockConsignment {
  id: string;
  vehicleId: string | null;
  ownerName: string;
  ownerPhone: string | null;
  plate: string;
  segmentId: string | null;
  segment: string | null;
  brandId: string | null;
  brand: string;
  modelId: string | null;
  model: string;
  year: number;
  color: string;
  engineNumber: string;
  chassisNumber: string;
  consignmentDate: string;
  basePrice: number;
  expectedSalePrice: number | null;
  commissionAmount: number | null;
  commissionRate: number | null;
  salePrice: number | null;
  netAmountToOwner: number | null;
  saleDate: string | null;
  status: ConsignmentStatus;
  description: string | null;
}

export interface CreateBrokeredConsignmentRequest {
  ownerName: string;
  ownerPhone: string | null;
  customerName: string | null;
  customerPhone: string | null;
  plate: string;
  segmentId: string;
  brandId: string;
  modelId: string;
  year: number;
  color: string;
  engineNumber: string;
  chassisNumber: string;
  consignmentDate: string;
  purchasePrice: number;
  commissionAmount: number | null;
  commissionRate: number | null;
  description: string;
}

export interface UpdateBrokeredConsignmentRequest extends CreateBrokeredConsignmentRequest {}

export interface CreateStockConsignmentRequest {
  ownerName: string;
  ownerPhone: string | null;
  plate: string;
  segmentId: string;
  brandId: string;
  modelId: string;
  year: number;
  color: string;
  engineNumber: string;
  chassisNumber: string;
  consignmentDate: string;
  basePrice: number;
  expectedSalePrice: number | null;
  commissionAmount: number | null;
  commissionRate: number | null;
  description: string;
}

export interface UpdateStockConsignmentRequest extends CreateStockConsignmentRequest {}

export interface CompleteStockConsignmentSaleRequest {
  salePrice: number;
  saleDate: string;
  commissionAmount: number | null;
  commissionRate: number | null;
  description: string;
}
