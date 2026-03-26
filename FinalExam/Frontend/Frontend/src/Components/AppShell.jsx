import React from "react";
import { useNavigate } from "react-router-dom";

const navItems = [
  { key: "dashboard", label: "Dashboard", path: "/dashboard" },
  { key: "account", label: "Account", path: "/account" },
  { key: "customer", label: "Customer", path: "/customer" },
  { key: "item", label: "Item", path: "/item" },
  { key: "invoice", label: "Invoice", path: "/invoices" },
];

const AppShell = ({ activeKey, children }) => {
  const navigate = useNavigate();

  return (
    <>
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;700&family=Instrument+Serif:ital@0;1&display=swap');

        .app-shell-root {
          min-height: 100vh;
          background:
            radial-gradient(circle at top left, rgba(44, 107, 237, 0.08), transparent 28%),
            linear-gradient(180deg, #f7f3eb 0%, #f2eee7 100%);
          font-family: 'DM Sans', sans-serif;
          color: #1f1a17;
        }

        .app-shell-layout {
          display: grid;
          grid-template-columns: 280px minmax(0, 1fr);
          min-height: 100vh;
        }

        .app-shell-sidebar {
          padding: 28px 22px;
          border-right: 1px solid rgba(31, 26, 23, 0.08);
          background: rgba(255, 255, 255, 0.64);
          backdrop-filter: blur(14px);
          position: sticky;
          top: 0;
          height: 100vh;
          overflow-y: auto;
        }

        .app-shell-brand {
          background: #171412;
          color: #fffaf4;
          border-radius: 24px;
          padding: 24px 22px;
          box-shadow: 0 18px 40px rgba(23, 20, 18, 0.16);
        }

        .app-shell-brand h1 {
          font-family: 'Instrument Serif', serif;
          font-size: 34px;
          line-height: 1;
          margin: 0;
          font-weight: 400;
        }

        .app-shell-nav {
          display: grid;
          gap: 10px;
          margin-top: 28px;
        }

        .app-shell-nav button {
          border: none;
          border-radius: 14px;
          padding: 14px 16px;
          text-align: left;
          background: #ffffff;
          color: #1f1a17;
          font-weight: 600;
          box-shadow: 0 10px 24px rgba(31, 26, 23, 0.06);
        }

        .app-shell-nav button.active {
          background: #2c6bed;
          color: #ffffff;
        }

        .app-shell-content {
          padding: 28px;
        }

        @media (max-width: 1080px) {
          .app-shell-layout {
            grid-template-columns: 1fr;
          }

          .app-shell-sidebar {
            border-right: none;
            border-bottom: 1px solid rgba(31, 26, 23, 0.08);
          }
        }

        @media (max-width: 720px) {
          .app-shell-sidebar,
          .app-shell-content {
            padding: 18px;
          }
        }
      `}</style>

      <div className="app-shell-root">
        <div className="app-shell-layout">
          <aside className="app-shell-sidebar">
            <div className="app-shell-brand">
              <h1>QuickBooks</h1>
            </div>

            <div className="app-shell-nav">
              {navItems.map((item) => (
                <button
                  key={item.key}
                  type="button"
                  className={activeKey === item.key ? "active" : ""}
                  onClick={() => navigate(item.path)}
                >
                  {item.label}
                </button>
              ))}
            </div>
          </aside>

          <main className="app-shell-content">{children}</main>
        </div>
      </div>
    </>
  );
};

export default AppShell;
