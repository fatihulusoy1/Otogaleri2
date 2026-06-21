import { useEffect, useState } from "react";
import { Link, NavLink, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../state/AuthContext";

const menuItems = [
  { to: "/", label: "Dashboard", icon: "⌂" },
  { to: "/vehicles", label: "Araçlar", icon: "🚗" }
];

const accountItems = [
  { to: "/subscription", label: "Üyelik", icon: "💳" },
  { to: "/profile", label: "Profil", icon: "👤" }
];

const accountAdminItems = [
  { to: "/tenant-users", label: "Kullanıcılar", icon: "👥" },
  { to: "/tenant-activity", label: "İşlem Geçmişi", icon: "📜" }
];

const vehicleOperationItems = [
  { to: "/purchases", label: "Satınalma", icon: "🛒" },
  { to: "/sales", label: "Satış", icon: "💸" },
  { to: "/vehicle-expenses", label: "Araç Masrafları", icon: "🧰" }
];

const financeItems = [
  { to: "/receivables", label: "Alacaklar", icon: "⤴" },
  { to: "/payables", label: "Borçlar", icon: "⤵" }
];

const consignmentItems = [
  { to: "/consignments/brokered", label: "Konsinye Alım", icon: "🤝" },
  { to: "/consignments/stock", label: "Konsinye Stok", icon: "📦" }
];

const adminBaseDataItems = [
  { to: "/admin/segments", label: "Segmentler", icon: "◫" },
  { to: "/admin/brands", label: "Markalar", icon: "🏷" },
  { to: "/admin/models", label: "Modeller", icon: "◪" },
  { to: "/admin/expenses", label: "Masraf Kategorileri", icon: "🧾" }
];

const adminTenantItems = [
  { to: "/admin/plans", label: "Paketler", icon: "📦" },
  { to: "/admin/tenants", label: "Tenant ve Kullanıcılar", icon: "⚙" }
];

type MobileFolderKey = "vehicle" | "finance" | "consignment" | "account" | "basedata" | "tenantmgmt" | null;

export function AppShell() {
  const { session, logout } = useAuth();
  const location = useLocation();
  const [vehicleOpsOpen, setVehicleOpsOpen] = useState(false);
  const [financeOpen, setFinanceOpen] = useState(false);
  const [consignmentOpen, setConsignmentOpen] = useState(false);
  const [accountOpen, setAccountOpen] = useState(false);
  const [baseDataOpen, setBaseDataOpen] = useState(false);
  const [tenantMgmtOpen, setTenantMgmtOpen] = useState(false);
  const [mobileOpenFolder, setMobileOpenFolder] = useState<MobileFolderKey>(null);

  const accountFolderItems = [...accountItems, ...(session?.isTenantAdmin ? accountAdminItems : [])];

  function closeAllFolders() {
    setVehicleOpsOpen(false);
    setFinanceOpen(false);
    setConsignmentOpen(false);
    setAccountOpen(false);
    setBaseDataOpen(false);
    setTenantMgmtOpen(false);
    setMobileOpenFolder(null);
  }

  useEffect(() => {
    // Sayfalar arası gezinirken masaüstü klasörleri açık kalsın; yalnızca mobil popover kapansın.
    // Klasörler ancak sayfa yenilenince (component yeniden yüklenince) varsayılan kapalı duruma döner.
    setMobileOpenFolder(null);
  }, [location.pathname]);

  function toggleMobileFolder(folder: MobileFolderKey) {
    setMobileOpenFolder((current) => (current === folder ? null : folder));
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <Link to="/" className="brand-block">
          <div className="brand-mark">AG</div>
          <div>
            <strong>AutoGallery</strong>
            <span>SaaS Yönetim Paneli</span>
          </div>
        </Link>

        <nav className="sidebar-nav">
          {menuItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === "/"}
              className={({ isActive }) => `nav-link ${isActive ? "active" : ""}`}
            >
              <span>{item.icon}</span>
              {item.label}
            </NavLink>
          ))}
          <div className={`nav-folder ${vehicleOpsOpen ? "open" : ""}`}>
            <button type="button" className="nav-folder-toggle" onClick={() => setVehicleOpsOpen((current) => !current)}>
              <div className="nav-folder-title">
                <span>🛠</span>
                Araç İşlemleri
              </div>
              <strong className={`nav-folder-indicator ${vehicleOpsOpen ? "open" : ""}`}>{vehicleOpsOpen ? "-" : "+"}</strong>
            </button>
            <div className={`nav-folder-links ${vehicleOpsOpen ? "open" : ""}`}>
              {vehicleOperationItems.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  className={({ isActive }) => `nav-link nav-link-child ${isActive ? "active" : ""}`}
                >
                  <span>{item.icon}</span>
                  {item.label}
                </NavLink>
              ))}
            </div>
          </div>
          <NavLink
            to="/general-expenses"
            className={({ isActive }) => `nav-link ${isActive ? "active" : ""}`}
          >
            <span>🧾</span>
            Diğer Giderler
          </NavLink>
          <div className={`nav-folder ${financeOpen ? "open" : ""}`}>
            <button type="button" className="nav-folder-toggle" onClick={() => setFinanceOpen((current) => !current)}>
              <div className="nav-folder-title">
                <span>₺</span>
                Finans
              </div>
              <strong className={`nav-folder-indicator ${financeOpen ? "open" : ""}`}>{financeOpen ? "-" : "+"}</strong>
            </button>
            <div className={`nav-folder-links ${financeOpen ? "open" : ""}`}>
              {financeItems.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  className={({ isActive }) => `nav-link nav-link-child ${isActive ? "active" : ""}`}
                >
                  <span>{item.icon}</span>
                  {item.label}
                </NavLink>
              ))}
            </div>
          </div>
          <div className={`nav-folder ${consignmentOpen ? "open" : ""}`}>
            <button type="button" className="nav-folder-toggle" onClick={() => setConsignmentOpen((current) => !current)}>
              <div className="nav-folder-title">
                <span>🤝</span>
                Konsinye
              </div>
              <strong className={`nav-folder-indicator ${consignmentOpen ? "open" : ""}`}>{consignmentOpen ? "-" : "+"}</strong>
            </button>
            <div className={`nav-folder-links ${consignmentOpen ? "open" : ""}`}>
              {consignmentItems.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  className={({ isActive }) => `nav-link nav-link-child ${isActive ? "active" : ""}`}
                >
                  <span>{item.icon}</span>
                  {item.label}
                </NavLink>
              ))}
            </div>
          </div>
          {session?.isSuperAdmin ? (
            <>
              <div className="nav-section-title">Super Admin</div>
              <div className={`nav-folder ${baseDataOpen ? "open" : ""}`}>
                <button type="button" className="nav-folder-toggle" onClick={() => setBaseDataOpen((current) => !current)}>
                  <div className="nav-folder-title">
                    <span>🗂</span>
                    Temel Veri
                  </div>
                  <strong className={`nav-folder-indicator ${baseDataOpen ? "open" : ""}`}>{baseDataOpen ? "-" : "+"}</strong>
                </button>
                <div className={`nav-folder-links ${baseDataOpen ? "open" : ""}`}>
                  {adminBaseDataItems.map((item) => (
                    <NavLink
                      key={item.to}
                      to={item.to}
                      className={({ isActive }) => `nav-link nav-link-child ${isActive ? "active" : ""}`}
                    >
                      <span>{item.icon}</span>
                      {item.label}
                    </NavLink>
                  ))}
                </div>
              </div>
              <div className={`nav-folder ${tenantMgmtOpen ? "open" : ""}`}>
                <button type="button" className="nav-folder-toggle" onClick={() => setTenantMgmtOpen((current) => !current)}>
                  <div className="nav-folder-title">
                    <span>🏢</span>
                    Tenant Yönetimi
                  </div>
                  <strong className={`nav-folder-indicator ${tenantMgmtOpen ? "open" : ""}`}>{tenantMgmtOpen ? "-" : "+"}</strong>
                </button>
                <div className={`nav-folder-links ${tenantMgmtOpen ? "open" : ""}`}>
                  {adminTenantItems.map((item) => (
                    <NavLink
                      key={item.to}
                      to={item.to}
                      className={({ isActive }) => `nav-link nav-link-child ${isActive ? "active" : ""}`}
                    >
                      <span>{item.icon}</span>
                      {item.label}
                    </NavLink>
                  ))}
                </div>
              </div>
            </>
          ) : null}
          <div className={`nav-folder nav-folder-bottom ${accountOpen ? "open" : ""}`}>
            <button type="button" className="nav-folder-toggle" onClick={() => setAccountOpen((current) => !current)}>
              <div className="nav-folder-title">
                <span>👤</span>
                Hesap
              </div>
              <strong className={`nav-folder-indicator ${accountOpen ? "open" : ""}`}>{accountOpen ? "-" : "+"}</strong>
            </button>
            <div className={`nav-folder-links ${accountOpen ? "open" : ""}`}>
              {accountFolderItems.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  className={({ isActive }) => `nav-link nav-link-child ${isActive ? "active" : ""}`}
                >
                  <span>{item.icon}</span>
                  {item.label}
                </NavLink>
              ))}
            </div>
          </div>
        </nav>

        <div className="tenant-card">
          <span>Aktif tenant</span>
          <strong>{session?.tenantName}</strong>
          <small>{session?.userFullName}</small>
          <button type="button" className="ghost-button" onClick={logout}>
            Oturumu kapat
          </button>
        </div>
      </aside>

      <div className="main-layout">
        <header className="topbar">
          <div className="topbar-copy">
            <span className="eyebrow">Oto galeri yönetimi</span>
            <strong className="topbar-title">Operasyon, stok ve kârlılık tek ekranda</strong>
          </div>
          <div className="topbar-tenant">
            <span>{session?.tenantName}</span>
            <small>{session?.userFullName}</small>
          </div>
        </header>

        <div className="mobile-nav-shell">
          <div className="mobile-nav-scroll-hint">
            <span>‹</span>
            <small>Kaydır</small>
            <span>›</span>
          </div>

          {mobileOpenFolder === "vehicle" ? (
            <div className="mobile-folder-popover">
              {vehicleOperationItems.map((item) => (
                <NavLink
                  key={`popover-${item.to}`}
                  to={item.to}
                  className={({ isActive }) => `mobile-popover-link ${isActive ? "active" : ""}`}
                  onClick={closeAllFolders}
                >
                  <strong>{item.icon}</strong>
                  <span>{item.label}</span>
                </NavLink>
              ))}
            </div>
          ) : null}

          {mobileOpenFolder === "finance" ? (
            <div className="mobile-folder-popover">
              {financeItems.map((item) => (
                <NavLink
                  key={`popover-${item.to}`}
                  to={item.to}
                  className={({ isActive }) => `mobile-popover-link ${isActive ? "active" : ""}`}
                  onClick={closeAllFolders}
                >
                  <strong>{item.icon}</strong>
                  <span>{item.label}</span>
                </NavLink>
              ))}
            </div>
          ) : null}

          {mobileOpenFolder === "consignment" ? (
            <div className="mobile-folder-popover">
              {consignmentItems.map((item) => (
                <NavLink
                  key={`popover-${item.to}`}
                  to={item.to}
                  className={({ isActive }) => `mobile-popover-link ${isActive ? "active" : ""}`}
                  onClick={closeAllFolders}
                >
                  <strong>{item.icon}</strong>
                  <span>{item.label}</span>
                </NavLink>
              ))}
            </div>
          ) : null}

          {mobileOpenFolder === "account" ? (
            <div className="mobile-folder-popover">
              {accountFolderItems.map((item) => (
                <NavLink
                  key={`popover-${item.to}`}
                  to={item.to}
                  className={({ isActive }) => `mobile-popover-link ${isActive ? "active" : ""}`}
                  onClick={closeAllFolders}
                >
                  <strong>{item.icon}</strong>
                  <span>{item.label}</span>
                </NavLink>
              ))}
            </div>
          ) : null}

          {mobileOpenFolder === "basedata" ? (
            <div className="mobile-folder-popover">
              {adminBaseDataItems.map((item) => (
                <NavLink
                  key={`popover-${item.to}`}
                  to={item.to}
                  className={({ isActive }) => `mobile-popover-link ${isActive ? "active" : ""}`}
                  onClick={closeAllFolders}
                >
                  <strong>{item.icon}</strong>
                  <span>{item.label}</span>
                </NavLink>
              ))}
            </div>
          ) : null}

          {mobileOpenFolder === "tenantmgmt" ? (
            <div className="mobile-folder-popover">
              {adminTenantItems.map((item) => (
                <NavLink
                  key={`popover-${item.to}`}
                  to={item.to}
                  className={({ isActive }) => `mobile-popover-link ${isActive ? "active" : ""}`}
                  onClick={closeAllFolders}
                >
                  <strong>{item.icon}</strong>
                  <span>{item.label}</span>
                </NavLink>
              ))}
            </div>
          ) : null}

          <div className="mobile-nav-grid">
            {menuItems.map((item) => (
              <NavLink
                key={`mobile-${item.to}`}
                to={item.to}
                end={item.to === "/"}
                className={({ isActive }) => `mobile-nav-card ${isActive ? "active" : ""}`}
                onClick={closeAllFolders}
              >
                <strong>{item.icon}</strong>
                <span>{item.label}</span>
              </NavLink>
            ))}
            <button type="button" className={`mobile-folder-toggle ${mobileOpenFolder === "vehicle" ? "open" : ""}`} onClick={() => toggleMobileFolder("vehicle")}>
              <strong>🚗</strong>
              <span>Araç İşl.</span>
              <small>↑</small>
            </button>
            <NavLink
              to="/general-expenses"
              className={({ isActive }) => `mobile-nav-card ${isActive ? "active" : ""}`}
              onClick={closeAllFolders}
            >
              <strong>🧾</strong>
              <span>Gider</span>
            </NavLink>
            <button type="button" className={`mobile-folder-toggle ${mobileOpenFolder === "finance" ? "open" : ""}`} onClick={() => toggleMobileFolder("finance")}>
              <strong>₺</strong>
              <span>Finans</span>
              <small>↑</small>
            </button>
            <button type="button" className={`mobile-folder-toggle ${mobileOpenFolder === "consignment" ? "open" : ""}`} onClick={() => toggleMobileFolder("consignment")}>
              <strong>🤝</strong>
              <span>Konsinye</span>
              <small>↑</small>
            </button>
            {session?.isSuperAdmin ? (
              <>
                <button type="button" className={`mobile-folder-toggle ${mobileOpenFolder === "basedata" ? "open" : ""}`} onClick={() => toggleMobileFolder("basedata")}>
                  <strong>🗂</strong>
                  <span>Temel Veri</span>
                  <small>↑</small>
                </button>
                <button type="button" className={`mobile-folder-toggle ${mobileOpenFolder === "tenantmgmt" ? "open" : ""}`} onClick={() => toggleMobileFolder("tenantmgmt")}>
                  <strong>🏢</strong>
                  <span>Tenant Yön.</span>
                  <small>↑</small>
                </button>
              </>
            ) : null}
            <button type="button" className={`mobile-folder-toggle ${mobileOpenFolder === "account" ? "open" : ""}`} onClick={() => toggleMobileFolder("account")}>
              <strong>👤</strong>
              <span>Hesap</span>
              <small>↑</small>
            </button>
          </div>
        </div>

        <main className="page-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
