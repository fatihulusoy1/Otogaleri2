import { useEffect, useState } from "react";
import { Navigate, Route, Routes, useLocation, useNavigate } from "react-router-dom";
import { AppShell } from "./components/AppShell";
import { ResetPasswordPage } from "./pages/ResetPasswordPage";
import { ConfirmDialog } from "./components/ConfirmDialog";
import { AuthPage } from "./pages/AuthPage";
import { AdminBrandsPage } from "./pages/AdminBrandsPage";
import { AdminPlansPage } from "./pages/AdminPlansPage";
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
import { ProfilePage } from "./pages/ProfilePage";
import { StockConsignmentsPage } from "./pages/StockConsignmentsPage";
import { SubscriptionPage } from "./pages/SubscriptionPage";
import { TenantActivityPage } from "./pages/TenantActivityPage";
import { TenantUsersPage } from "./pages/TenantUsersPage";
import { VehicleExpensesPage } from "./pages/VehicleExpensesPage";
import { VehiclesPage } from "./pages/VehiclesPage";
import { limitExceededEventName } from "./lib/api";
import { useAuth } from "./state/AuthContext";

function ProtectedRoutes() {
  const { session } = useAuth();
  const navigate = useNavigate();
  const [limitMessage, setLimitMessage] = useState<string | null>(null);

  useEffect(() => {
    function handleLimitExceeded(event: Event) {
      const detail = (event as CustomEvent<string>).detail;
      setLimitMessage(detail || "Paket limitiniz dolu.");
    }

    window.addEventListener(limitExceededEventName, handleLimitExceeded);
    return () => window.removeEventListener(limitExceededEventName, handleLimitExceeded);
  }, []);

  return (
    <>
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
        <Route path="/subscription" element={<SubscriptionPage />} />
        <Route path="/profile" element={<ProfilePage />} />
        {session?.isTenantAdmin ? <Route path="/tenant-users" element={<TenantUsersPage />} /> : null}
        {session?.isTenantAdmin ? <Route path="/tenant-activity" element={<TenantActivityPage />} /> : null}
        <Route path="/expenses" element={<Navigate to="/vehicle-expenses" replace />} />
        <Route path="/stock" element={<Navigate to="/vehicle-expenses" replace />} />
        {session?.isSuperAdmin ? (
          <>
            <Route path="/admin/segments" element={<AdminSegmentsPage />} />
            <Route path="/admin/brands" element={<AdminBrandsPage />} />
            <Route path="/admin/models" element={<AdminModelsPage />} />
            <Route path="/admin/expenses" element={<AdminExpenseCategoriesPage />} />
            <Route path="/admin/tenants" element={<AdminTenantsPage />} />
            <Route path="/admin/plans" element={<AdminPlansPage />} />
          </>
        ) : null}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
      {limitMessage ? (
        <ConfirmDialog
          title="Limit doldu"
          description={`${limitMessage} Üyelik ekranına giderek paketinizi yükseltmek ister misiniz?`}
          confirmLabel="Üyelik ekranına git"
          cancelLabel="Vazgeç"
          onConfirm={() => {
            setLimitMessage(null);
            navigate("/subscription");
          }}
          onCancel={() => setLimitMessage(null)}
        />
      ) : null}
    </>
  );
}

export default function App() {
  const { isAuthenticated, session } = useAuth();
  const location = useLocation();

  // Şifre sıfırlama bağlantısı kimlik doğrulamadan bağımsız çalışır.
  if (location.pathname === "/reset-password") {
    return <ResetPasswordPage />;
  }

  if (!isAuthenticated) {
    return <AuthPage />;
  }

  if (session?.subscriptionExpired && !session.isSuperAdmin) {
    return (
      <div className="expired-gate-shell">
        <SubscriptionPage expiredGate />
      </div>
    );
  }

  return <ProtectedRoutes />;
}
