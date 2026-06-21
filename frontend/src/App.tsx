import { Navigate, Route, Routes } from "react-router-dom";
import { AppShell } from "./components/AppShell";
import { AuthPage } from "./pages/AuthPage";
import { AdminBrandsPage } from "./pages/AdminBrandsPage";
import { AdminExpenseCategoriesPage } from "./pages/AdminExpenseCategoriesPage";
import { AdminModelsPage } from "./pages/AdminModelsPage";
import { AdminSegmentsPage } from "./pages/AdminSegmentsPage";
import { AdminTenantsPage } from "./pages/AdminTenantsPage";
import { BrokeredConsignmentsPage } from "./pages/BrokeredConsignmentsPage";
import { DashboardPage } from "./pages/DashboardPage";
import { GeneralExpensesPage } from "./pages/GeneralExpensesPage";
import { PayablesPage } from "./pages/PayablesPage";
import { PurchasesPage } from "./pages/PurchasesPage";
import { ReceivablesPage } from "./pages/ReceivablesPage";
import { SalesPage } from "./pages/SalesPage";
import { StockConsignmentsPage } from "./pages/StockConsignmentsPage";
import { VehicleExpensesPage } from "./pages/VehicleExpensesPage";
import { VehiclesPage } from "./pages/VehiclesPage";
import { useAuth } from "./state/AuthContext";

function ProtectedRoutes() {
  const { session } = useAuth();

  return (
    <Routes>
      <Route element={<AppShell />}>
        <Route path="/" element={<DashboardPage />} />
        <Route path="/purchases" element={<PurchasesPage />} />
        <Route path="/sales" element={<SalesPage />} />
        <Route path="/consignments/brokered" element={<BrokeredConsignmentsPage />} />
        <Route path="/consignments/stock" element={<StockConsignmentsPage />} />
        <Route path="/vehicle-expenses" element={<VehicleExpensesPage />} />
        <Route path="/general-expenses" element={<GeneralExpensesPage />} />
        <Route path="/receivables" element={<ReceivablesPage />} />
        <Route path="/payables" element={<PayablesPage />} />
        <Route path="/vehicles" element={<VehiclesPage />} />
        <Route path="/expenses" element={<Navigate to="/vehicle-expenses" replace />} />
        <Route path="/stock" element={<Navigate to="/vehicle-expenses" replace />} />
        {session?.isSuperAdmin ? (
          <>
            <Route path="/admin/segments" element={<AdminSegmentsPage />} />
            <Route path="/admin/brands" element={<AdminBrandsPage />} />
            <Route path="/admin/models" element={<AdminModelsPage />} />
            <Route path="/admin/expenses" element={<AdminExpenseCategoriesPage />} />
            <Route path="/admin/tenants" element={<AdminTenantsPage />} />
          </>
        ) : null}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}

export default function App() {
  const { isAuthenticated } = useAuth();

  return isAuthenticated ? <ProtectedRoutes /> : <AuthPage />;
}
