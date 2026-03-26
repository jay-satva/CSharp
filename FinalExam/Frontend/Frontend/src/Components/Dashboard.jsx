import React, { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { authorizedFetch } from "../auth/api";
import { clearAuth, getAuth } from "../auth/authStorage";

const Dashboard = () => {
  const location = useLocation();
  const navigate = useNavigate();

  const [user, setUser] = useState(null);
  const [companies, setCompanies] = useState([]);
  const [selectedRealmId, setSelectedRealmId] = useState("");
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState(false);
  const [status, setStatus] = useState("");
  const [error, setError] = useState("");

  const activeCompanies = companies.filter((company) => company.isActive ?? company.IsActive);
  const storedAuth = getAuth();
  const displayName = user?.name || user?.Name || storedAuth?.name || "Intuit User";
  const displayEmail =
    user?.email || user?.Email || storedAuth?.email || user?.intuitSub || user?.IntuitSub || storedAuth?.intuitSub || "No email available";

  useEffect(() => {
    const params = new URLSearchParams(location.search);
    const nextStatus = params.get("status");
    const nextError = params.get("error");

    if (nextStatus === "success") {
      setStatus("Signed in with Intuit successfully.");
    } else if (nextStatus === "connected") {
      setStatus("QuickBooks company connected successfully.");
    }

    if (nextError) {
      setError(nextError);
    }
  }, [location.search]);

  const loadCompanies = async () => {
    const res = await authorizedFetch(`/qb/companies`, {
      method: "GET",
    });

    if (res.status === 401) {
      navigate("/login");
      return [];
    }

    const data = await res.json().catch(() => ({}));
    if (!res.ok) {
      throw new Error(data.message || "Failed to load companies.");
    }

    return Array.isArray(data) ? data : [];
  };

  useEffect(() => {
    const initializeDashboard = async () => {
      setLoading(true);
      setError("");

      try {
        const userRes = await authorizedFetch(`/auth/user`, {
          method: "GET",
        });

        if (userRes.status === 401) {
          navigate("/login");
          return;
        }

        const userData = await userRes.json().catch(() => ({}));
        if (!userRes.ok) {
          throw new Error(userData.message || "Failed to load session.");
        }

        setUser(userData);

        const companyData = await loadCompanies();
        setCompanies(companyData);
      } catch (e) {
        setError(e.message || "Failed to load dashboard.");
      } finally {
        setLoading(false);
      }
    };

    initializeDashboard();
  }, [navigate]);

  useEffect(() => {
    if (!selectedRealmId && activeCompanies.length > 0) {
      setSelectedRealmId(activeCompanies[0].realmId || activeCompanies[0].RealmId);
    }

    if (activeCompanies.length === 0) {
      setSelectedRealmId("");
    }
  }, [activeCompanies, selectedRealmId]);

  const handleConnectQuickBooks = () => {
    const startConnect = async () => {
      setActionLoading(true);
      setError("");

      try {
        const res = await authorizedFetch(`/auth/qb/connect-url`, { method: "GET" });
        const data = await res.json().catch(() => ({}));
        if (!res.ok) {
          throw new Error(data.message || "Failed to start QuickBooks connection.");
        }

        window.location.href = data.url;
      } catch (e) {
        setError(e.message || "Failed to start QuickBooks connection.");
      } finally {
        setActionLoading(false);
      }
    };

    startConnect();
  };

  const handleToggleCompany = async (realmId, isActive) => {
    if (!realmId) return;

    if (!isActive) {
      handleConnectQuickBooks();
      return;
    }

    setActionLoading(true);
    setError("");
    setStatus("");

    try {
      const endpoint = `/qb/companies/${encodeURIComponent(realmId)}/disconnect`;

      const res = await authorizedFetch(`${endpoint}`, {
        method: "POST",
      });

      const data = await res.json().catch(() => ({}));
      if (!res.ok) {
        throw new Error(data.message || "Failed to update company status.");
      }

      const refreshedCompanies = await loadCompanies();
      setCompanies(refreshedCompanies);
      setStatus(data.message || "Company status updated.");
    } catch (e) {
      setError(e.message || "Failed to update company status.");
    } finally {
      setActionLoading(false);
    }
  };

  const handleLogout = async () => {
    clearAuth();
    navigate("/login");
  };

  return (
    <>
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;700&family=Instrument+Serif:ital@0;1&display=swap');

        .dashboard-shell {
          min-height: 100vh;
          background:
            radial-gradient(circle at top left, rgba(44, 107, 237, 0.08), transparent 28%),
            linear-gradient(180deg, #f7f3eb 0%, #f2eee7 100%);
          font-family: 'DM Sans', sans-serif;
          color: #1f1a17;
        }

        .dashboard-layout {
          display: grid;
          grid-template-columns: 280px minmax(0, 1fr);
          min-height: 100vh;
        }

        .dashboard-sidebar {
          padding: 28px 22px;
          border-right: 1px solid rgba(31, 26, 23, 0.08);
          background: rgba(255, 255, 255, 0.64);
          backdrop-filter: blur(14px);
        }

        .dashboard-brand {
          background: #171412;
          color: #fffaf4;
          border-radius: 24px;
          padding: 24px 22px;
          box-shadow: 0 18px 40px rgba(23, 20, 18, 0.16);
        }

        .dashboard-brand h1 {
          font-family: 'Instrument Serif', serif;
          font-size: 34px;
          line-height: 1;
          margin: 0 0 8px;
          font-weight: 400;
        }

        .dashboard-brand p {
          margin: 0;
          color: rgba(255, 250, 244, 0.72);
          font-size: 14px;
        }

        .dashboard-nav {
          display: grid;
          gap: 10px;
          margin-top: 28px;
        }

        .dashboard-nav button {
          border: none;
          border-radius: 14px;
          padding: 14px 16px;
          text-align: left;
          background: #ffffff;
          color: #1f1a17;
          font-weight: 600;
          box-shadow: 0 10px 24px rgba(31, 26, 23, 0.06);
        }

        .dashboard-nav button.active {
          background: #2c6bed;
          color: #ffffff;
        }

        .dashboard-nav button:disabled {
          opacity: 0.8;
          cursor: not-allowed;
        }

        .sidebar-note {
          margin-top: 28px;
          padding: 18px;
          border-radius: 18px;
          background: rgba(255, 255, 255, 0.8);
          color: #5f5954;
          font-size: 14px;
          line-height: 1.5;
        }

        .dashboard-main {
          padding: 28px;
        }

        .dashboard-topbar {
          display: flex;
          justify-content: space-between;
          align-items: center;
          gap: 18px;
          background: rgba(255, 255, 255, 0.8);
          border: 1px solid rgba(31, 26, 23, 0.06);
          border-radius: 24px;
          padding: 22px 24px;
          box-shadow: 0 16px 36px rgba(31, 26, 23, 0.08);
        }

        .topbar-title {
          font-family: 'Instrument Serif', serif;
          font-size: 34px;
          line-height: 1;
          margin: 0 0 8px;
          font-weight: 400;
        }

        .topbar-subtitle {
          margin: 0;
          color: #6f6761;
          font-size: 15px;
        }

        .topbar-user {
          text-align: right;
        }

        .topbar-user strong {
          display: block;
          font-size: 15px;
          color: #1f1a17;
        }

        .topbar-user span {
          color: #7a716a;
          font-size: 13px;
        }

        .topbar-actions {
          display: flex;
          align-items: center;
          gap: 12px;
        }

        .btn-solid {
          border: none;
          border-radius: 14px;
          padding: 12px 18px;
          background: #2c6bed;
          color: #ffffff;
          font-weight: 700;
          box-shadow: 0 14px 28px rgba(44, 107, 237, 0.18);
        }

        .btn-ghost {
          border: 1px solid rgba(31, 26, 23, 0.12);
          border-radius: 14px;
          padding: 12px 18px;
          background: #ffffff;
          color: #1f1a17;
          font-weight: 700;
        }

        .btn-solid:disabled,
        .btn-ghost:disabled {
          opacity: 0.7;
          cursor: not-allowed;
        }

        .dashboard-grid {
          display: grid;
          
          gap: 22px;
          margin-top: 22px;
        }

        .dashboard-card {
          background: rgba(255, 255, 255, 0.84);
          border: 1px solid rgba(31, 26, 23, 0.06);
          border-radius: 24px;
          padding: 24px;
          box-shadow: 0 18px 34px rgba(31, 26, 23, 0.08);
        }

        .dashboard-card h2 {
          margin: 0 0 10px;
          font-size: 22px;
        }

        .dashboard-card p {
          color: #6a625c;
          margin: 0;
        }

        .message {
          border-radius: 16px;
          padding: 14px 16px;
          margin-top: 18px;
          font-weight: 500;
        }

        .message.success {
          background: #edf8ef;
          color: #21653a;
          border: 1px solid #cfe8d4;
        }

        .message.error {
          background: #fff1f0;
          color: #b23a33;
          border: 1px solid #f2cac7;
        }

        .stat-row {
          display: grid;
          grid-template-columns: repeat(3, minmax(0, 1fr));
          gap: 14px;
          margin-top: 22px;
        }

        .stat-box {
          border-radius: 20px;
          padding: 18px;
          background: #f8f5ef;
          border: 1px solid rgba(31, 26, 23, 0.05);
        }

        .stat-box span {
          display: block;
          color: #766e67;
          font-size: 13px;
          margin-bottom: 8px;
        }

        .stat-box strong {
          font-size: 28px;
          line-height: 1;
          color: #1f1a17;
        }

        .company-picker {
          margin-top: 22px;
        }

        .company-picker label {
          display: block;
          margin-bottom: 8px;
          font-size: 13px;
          font-weight: 700;
          text-transform: uppercase;
          letter-spacing: 0.05em;
          color: #756d67;
        }

        .company-picker select {
          width: 100%;
          border-radius: 14px;
          border: 1px solid rgba(31, 26, 23, 0.12);
          background: #ffffff;
          padding: 13px 14px;
          font-size: 15px;
          color: #1f1a17;
        }

        .empty-state {
          margin-top: 22px;
          border-radius: 20px;
          padding: 20px;
          background: #f8f5ef;
          border: 1px dashed rgba(31, 26, 23, 0.14);
          color: #665f58;
          line-height: 1.6;
        }

        .company-list {
          width: 100%;
          border-collapse: collapse;
          margin-top: 20px;
        }

        .company-list th,
        .company-list td {
          text-align: left;
          padding: 14px 8px;
          border-bottom: 1px solid rgba(31, 26, 23, 0.08);
          vertical-align: middle;
        }

        .company-list th {
          color: #7a716a;
          font-size: 12px;
          text-transform: uppercase;
          letter-spacing: 0.05em;
        }

        .company-name {
          font-weight: 700;
          color: #1f1a17;
        }

        .company-meta {
          font-size: 13px;
          color: #766e67;
          margin-top: 3px;
        }

        .status-pill {
          display: inline-flex;
          align-items: center;
          border-radius: 999px;
          padding: 7px 12px;
          font-size: 12px;
          font-weight: 700;
        }

        .status-pill.active {
          background: #edf8ef;
          color: #21653a;
        }

        .status-pill.inactive {
          background: #f4efe8;
          color: #8a7462;
        }

        .loading-shell {
          min-height: 100vh;
          display: grid;
          place-items: center;
          background: linear-gradient(180deg, #f7f3eb 0%, #f2eee7 100%);
          font-family: 'DM Sans', sans-serif;
          color: #5d5650;
        }

        @media (max-width: 1080px) {
          .dashboard-layout {
            grid-template-columns: 1fr;
          }

          .dashboard-sidebar {
            border-right: none;
            border-bottom: 1px solid rgba(31, 26, 23, 0.08);
          }

          .dashboard-grid {
            grid-template-columns: 1fr;
          }

          .dashboard-topbar {
            flex-direction: column;
            align-items: flex-start;
          }

          .topbar-user {
            text-align: left;
          }
        }

        @media (max-width: 720px) {
          .dashboard-main,
          .dashboard-sidebar {
            padding: 18px;
          }

          .stat-row {
            grid-template-columns: 1fr;
          }

          .topbar-actions {
            width: 100%;
            flex-direction: column;
            align-items: stretch;
          }

          .btn-solid,
          .btn-ghost {
            width: 100%;
          }
        }
      `}</style>

      {loading ? (
        <div className="loading-shell">Loading dashboard...</div>
      ) : (
        <div className="dashboard-shell">
          <div className="dashboard-layout">
            <aside className="dashboard-sidebar">
              <div className="dashboard-brand">
                <h1>QuickBooks</h1>
              </div>

              <div className="dashboard-nav">
                <button type="button" className="active">
                  Dashboard
                </button>
                <button type="button" onClick={() => navigate("/account")}>
                  Account
                </button>
                <button type="button" onClick={() => navigate("/customer")}>
                  Customer
                </button>
                <button type="button" onClick={() => navigate("/item")}>
                  Item
                </button>
                <button type="button" onClick={() => navigate("/invoices")}>
                  Invoice
                </button>
              </div>

              {/* <div className="sidebar-note">
                Only active companies should appear in selection dropdowns later. Disconnected companies stay in Mongo with
                <strong> IsActive = false</strong>.
              </div> */}
            </aside>

            <main className="dashboard-main">
              <section className="dashboard-topbar">
                <div>
                  <h2 className="topbar-title">Dashboard</h2>
                  {/* <p className="topbar-subtitle">Your QuickBooks connection hub is now a single, cleaner flow.</p> */}
                </div>

                <div className="topbar-actions">
                  <div className="topbar-user">
                    <strong>{displayName}</strong>
                    <span>{displayEmail}</span>
                  </div>
                  <button type="button" className="btn-ghost" onClick={handleLogout}>
                    Sign Out
                  </button>
                </div>
              </section>

              {status ? <div className="message success">{status}</div> : null}
              {error ? <div className="message error">{error}</div> : null}

              <div className="dashboard-grid">
                <section className="dashboard-card">
                  <h2>QuickBooks Connection</h2>
                  {/* <p>
                    Use the same button for both manual-login users and Intuit SSO users. Intuit will ask for the QuickBooks
                    email when needed during the connect flow.
                  </p> */}

                  <div className="stat-row">
                    <div className="stat-box">
                      <span>Total companies</span>
                      <strong>{companies.length}</strong>
                    </div>
                    <div className="stat-box">
                      <span>Connected companies</span>
                      <strong>{activeCompanies.length}</strong>
                    </div>
                    <div className="stat-box">
                      <span>Disconnected companies</span>
                      <strong>{Math.max(companies.length - activeCompanies.length, 0)}</strong>
                    </div>
                  </div>

                  <div style={{ marginTop: 22 }}>
                    <button type="button" className="btn-solid" onClick={handleConnectQuickBooks} disabled={actionLoading}>
                      {companies.length > 0 ? "Connect Another QuickBooks Company" : "Connect to QuickBooks"}
                    </button>
                  </div>

                  {activeCompanies.length > 0 ? (
                    <div className="company-picker">
                      <label htmlFor="active-company">Active Company Selection</label>
                      <select
                        id="active-company"
                        value={selectedRealmId}
                        onChange={(e) => setSelectedRealmId(e.target.value)}
                      >
                        {activeCompanies.map((company) => {
                          const realmId = company.realmId || company.RealmId;
                          const companyName = company.companyName || company.CompanyName;

                          return (
                            <option key={realmId} value={realmId}>
                              {companyName} ({realmId})
                            </option>
                          );
                        })}
                      </select>
                    </div>
                  ) : (
                    <div className="empty-state">
                      No connected companies yet. Once you complete the QuickBooks connect flow, the company list will appear
                      here automatically.
                    </div>
                  )}
                </section>

                {/* <section className="dashboard-card">
                  <h2>What Changed</h2>
                  <p>
                    The dashboard no longer reveals a second redundant connect button after the first click. It now keeps one
                    company list and one primary connect action.
                  </p>

                  <div className="empty-state" style={{ marginTop: 20 }}>
                    Forms for Account, Customer, Item, and Invoice are still pending. This screen is now focused on fixing the
                    auth and company-connection flow first.
                  </div>
                </section> */}
              </div>

              <section className="dashboard-card" style={{ marginTop: 22 }}>
                <h2>Companies</h2>
                <p>All linked companies are listed here. Disconnected companies remain visible for reconnect actions.</p>

                {companies.length === 0 ? (
                  <div className="empty-state">
                    No company records found yet. Start with the QuickBooks connect flow and the first company will be stored in
                    MongoDB.
                  </div>
                ) : (
                  <div style={{ overflowX: "auto" }}>
                    <table className="company-list">
                      <thead>
                        <tr>
                          <th>Company</th>
                          <th>Country</th>
                          <th>Realm ID</th>
                          <th>Status</th>
                          <th>Action</th>
                        </tr>
                      </thead>
                      <tbody>
                        {companies.map((company) => {
                          const realmId = company.realmId || company.RealmId;
                          const companyName = company.companyName || company.CompanyName;
                          const country = company.country || company.Country;
                          const isActive = company.isActive ?? company.IsActive;

                          return (
                            <tr key={realmId}>
                              <td>
                                <div className="company-name">{companyName}</div>
                                <div className="company-meta">Used for future dropdown selection</div>
                              </td>
                              <td>{country}</td>
                              <td>{realmId}</td>
                              <td>
                                <span className={`status-pill ${isActive ? "active" : "inactive"}`}>
                                  {isActive ? "Connected" : "Disconnected"}
                                </span>
                              </td>
                              <td>
                                <button
                                  type="button"
                                  className={isActive ? "btn-ghost" : "btn-solid"}
                                  onClick={() => handleToggleCompany(realmId, isActive)}
                                  disabled={actionLoading}
                                >
                                  {isActive ? "Disconnect" : "Reconnect in QuickBooks"}
                                </button>
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                )}
              </section>
            </main>
          </div>
        </div>
      )}
    </>
  );
};

export default Dashboard;
