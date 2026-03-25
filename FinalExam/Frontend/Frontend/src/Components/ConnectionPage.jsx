import React, { useEffect, useState } from "react";
import { useLocation } from "react-router-dom";

const API_BASE = "https://localhost:7142";

const ConnectionPage = () => {
  const location = useLocation();
  const params = new URLSearchParams(location.search);
  const status = params.get("status");
  const error = params.get("error");

  const [companies, setCompanies] = useState([]);
  const [loading, setLoading] = useState(false);
  const [loadError, setLoadError] = useState("");

  const loadCompanies = async () => {
    setLoading(true);
    setLoadError("");
    try {
      const res = await fetch(`${API_BASE}/qb/companies`, {
        method: "GET",
        credentials: "include",
      });
      if (!res.ok) {
        const data = await res.json().catch(() => ({}));
        throw new Error(data.message || "Failed to load companies.");
      }
      const data = await res.json();
      setCompanies(Array.isArray(data) ? data : []);
    } catch (e) {
      setLoadError(e.message || "Failed to load companies.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadCompanies();
    // loadCompanies is stable enough for this component
  }, []);

  const toggleCompany = async (realmId, isActive) => {
    setLoading(true);
    setLoadError("");
    try {
      const endpoint = isActive
        ? `/qb/companies/${encodeURIComponent(realmId)}/disconnect`
        : `/qb/companies/${encodeURIComponent(realmId)}/connect`;

      const res = await fetch(`${API_BASE}${endpoint}`, {
        method: "POST",
        credentials: "include",
      });

      if (!res.ok) {
        const data = await res.json().catch(() => ({}));
        throw new Error(data.message || "Failed to update company status.");
      }

      await loadCompanies();
    } catch (e) {
      setLoadError(e.message || "Failed to update company status.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container mt-5">
      <div className="card shadow">
        <div className="card-body p-5">
          <h1 className="display-5">QuickBooks Connection</h1>
          <p className="text-muted mt-2">Connect to your companies and enable one or more for use.</p>

          {status === "connected" && (
            <div className="alert alert-success mt-4" role="alert">
              Connected to QuickBooks successfully.
            </div>
          )}

          {error && (
            <div className="alert alert-danger mt-4" role="alert">
              {error}
            </div>
          )}

          {loadError && (
            <div className="alert alert-danger mt-4" role="alert">
              {loadError}
            </div>
          )}

          <div className="d-flex justify-content-between align-items-center mt-4">
            <div className="fw-semibold">Companies</div>
            <button
              className="btn btn-outline-primary"
              disabled={loading}
              onClick={() => (window.location.href = `${API_BASE}/auth/qb/connect`)}
            >
              Connect another company
            </button>
          </div>

          {loading ? (
            <div className="mt-4">Loading...</div>
          ) : companies.length === 0 ? (
            <div className="mt-4 text-muted">
              No connected companies yet. Use the <b>Connect to QuickBooks</b> button on the dashboard.
            </div>
          ) : (
            <div className="mt-4">
              <div className="table-responsive">
                <table className="table table-striped align-middle">
                  <thead>
                    <tr>
                      <th>Company</th>
                      <th>Country</th>
                      <th>RealmId</th>
                      <th>Status</th>
                      <th style={{ width: 200 }}>Action</th>
                    </tr>
                  </thead>
                  <tbody>
                    {companies.map((c) => (
                      <tr key={c.realmId || c.RealmId}>
                        <td>{c.companyName || c.CompanyName}</td>
                        <td>{c.country || c.Country}</td>
                        <td>{c.realmId || c.RealmId}</td>
                        <td>{(c.isActive ?? c.IsActive) ? "Connected" : "Disconnected"}</td>
                        <td>
                          <button
                            className={`btn ${((c.isActive ?? c.IsActive) ? "btn-danger" : "btn-success")}`}
                            disabled={loading}
                            onClick={() =>
                              toggleCompany(c.realmId || c.RealmId, c.isActive ?? c.IsActive)
                            }
                          >
                            {(c.isActive ?? c.IsActive) ? "Disconnect" : "Connect"}
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default ConnectionPage;

