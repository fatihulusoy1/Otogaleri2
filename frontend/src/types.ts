export interface AuthResponse {
  token: string;
  refreshToken: string;
  expiry: string;
  tenantId: string;
  tenantName: string;
  userFullName: string;
  isSuperAdmin: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest extends LoginRequest {
  firstName: string;
  lastName: string;
  tenantName: string;
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
}

export interface AdminUser {
  id: string;
  tenantId: string;
  tenantName: string;
  fullName: string;
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
