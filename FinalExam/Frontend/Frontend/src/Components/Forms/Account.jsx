import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { authorizedFetch } from "../../auth/api";

const accountTypes = [
  "Bank",
  "Accounts Receivable",
  "Other Current Asset",
  "Fixed Asset",
  "Other Asset",
  "Accounts Payable",
  "Credit Card",
  "Other Current Liability",
  "Long Term Liability",
  "Equity",
  "Income",
  "Cost of Goods Sold",
  "Expense",
  "Other Income",
  "Other Expense",
];

const Account = () => {
  const navigate = useNavigate();
  const [companies, setCompanies] = useState([]);
  const [selectedRealmId, setSelectedRealmId] = useState("");
  const [accounts, setAccounts] = useState([]);
  const [loadingCompanies, setLoadingCompanies] = useState(true);
  const [loadingAccounts, setLoadingAccounts] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [showModal, setShowModal] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [form, setForm] = useState({
    realmId: "",
    name: "",
    acctNum: "",
    accountType: "",
  });

  const activeCompanies = companies.filter((company) => company.isActive ?? company.IsActive);

  useEffect(() => {
    const loadCompanies = async () => {
      setLoadingCompanies(true);
      setError("");

      try {
        const res = await authorizedFetch("/qb/companies", { method: "GET" });
        if (res.status === 401) {
          navigate("/login");
          return;
        }

        const data = await res.json().catch(() => ({}));
        if (!res.ok) {
          throw new Error(data.message || "Failed to load connected companies.");
        }

        const list = Array.isArray(data) ? data : [];
        setCompanies(list);

        const firstActive = list.find((company) => company.isActive ?? company.IsActive);
        if (firstActive) {
          const realmId = firstActive.realmId || firstActive.RealmId;
          setSelectedRealmId(realmId);
          setForm((current) => ({ ...current, realmId }));
        }
      } catch (e) {
        setError(e.message || "Failed to load connected companies.");
      } finally {
        setLoadingCompanies(false);
      }
    };

    loadCompanies();
  }, [navigate]);

  useEffect(() => {
    const loadAccounts = async () => {
      if (!selectedRealmId) {
        setAccounts([]);
        return;
      }

      setLoadingAccounts(true);
      setError("");

      try {
        const res = await authorizedFetch(`/account?realmId=${encodeURIComponent(selectedRealmId)}`, {
          method: "GET",
        });
        if (res.status === 401) {
          navigate("/login");
          return;
        }

        const data = await res.json().catch(() => ({}));
        if (!res.ok) {
          throw new Error(data.message || "Failed to load QuickBooks accounts.");
        }

        setAccounts(Array.isArray(data) ? data : []);
      } catch (e) {
        setError(e.message || "Failed to load QuickBooks accounts.");
      } finally {
        setLoadingAccounts(false);
      }
    };

    loadAccounts();
  }, [navigate, selectedRealmId]);

  const handleOpenModal = () => {
    setSuccess("");
    setError("");
    setForm({
      realmId: selectedRealmId || activeCompanies[0]?.realmId || activeCompanies[0]?.RealmId || "",
      name: "",
      acctNum: "",
      accountType: "",
    });
    setShowModal(true);
  };

  const handleCloseModal = () => {
    setShowModal(false);
  };

  const handleFormChange = (field, value) => {
    setForm((current) => ({ ...current, [field]: value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setSuccess("");

    if (!form.realmId || !form.name.trim() || !form.accountType) {
      setError("Company, account name, and account type are required.");
      return;
    }

    if (form.name.includes('"') || form.name.includes(":")) {
      setError('Account name cannot contain double quotes or colon.');
      return;
    }

    if (form.acctNum.includes(":")) {
      setError("Account number cannot contain colon.");
      return;
    }

    setSubmitting(true);
    try {
      const res = await authorizedFetch("/account", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          realmId: form.realmId,
          name: form.name.trim(),
          acctNum: form.acctNum.trim() || null,
          accountType: form.accountType,
        }),
      });

      if (res.status === 401) {
        navigate("/login");
        return;
      }

      const data = await res.json().catch(() => ({}));
      if (!res.ok) {
        throw new Error(data.message || "Failed to create account in QuickBooks.");
      }

      setSelectedRealmId(form.realmId);
      setShowModal(false);
      setSuccess("Account created successfully in QuickBooks.");

      const refreshed = await authorizedFetch(`/account?realmId=${encodeURIComponent(form.realmId)}`, {
        method: "GET",
      });
      const refreshedData = await refreshed.json().catch(() => ([]));
      setAccounts(Array.isArray(refreshedData) ? refreshedData : []);
    } catch (e) {
      setError(e.message || "Failed to create account in QuickBooks.");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <>
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;700&family=Instrument+Serif:ital@0;1&display=swap');

        .account-page {
          min-height: 100vh;
          background:
            radial-gradient(circle at top left, rgba(44, 107, 237, 0.08), transparent 28%),
            linear-gradient(180deg, #f7f3eb 0%, #f2eee7 100%);
          font-family: 'DM Sans', sans-serif;
          color: #1f1a17;
          padding: 28px;
        }

        .account-shell {
          max-width: 1240px;
          margin: 0 auto;
        }

        .account-topbar,
        .account-card {
          background: rgba(255, 255, 255, 0.88);
          border: 1px solid rgba(31, 26, 23, 0.06);
          border-radius: 24px;
          box-shadow: 0 18px 34px rgba(31, 26, 23, 0.08);
        }

        .account-topbar {
          display: flex;
          justify-content: space-between;
          align-items: center;
          gap: 18px;
          padding: 24px;
        }

        .account-title {
          font-family: 'Instrument Serif', serif;
          font-size: 34px;
          line-height: 1;
          margin: 0 0 8px;
          font-weight: 400;
        }

        .account-subtitle {
          margin: 0;
          color: #6f6761;
          font-size: 15px;
        }

        .account-actions {
          display: flex;
          gap: 12px;
          align-items: center;
        }

        .btn-solid,
        .btn-ghost {
          border-radius: 14px;
          padding: 12px 18px;
          font-weight: 700;
          font-size: 14px;
        }

        .btn-solid {
          border: none;
          background: #2c6bed;
          color: #fff;
          box-shadow: 0 14px 28px rgba(44, 107, 237, 0.18);
        }

        .btn-ghost {
          border: 1px solid rgba(31, 26, 23, 0.12);
          background: #fff;
          color: #1f1a17;
        }

        .btn-solid:disabled,
        .btn-ghost:disabled {
          opacity: 0.7;
          cursor: not-allowed;
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

        .account-card {
          margin-top: 22px;
          padding: 24px;
        }

        .account-toolbar {
          display: flex;
          justify-content: space-between;
          align-items: end;
          gap: 18px;
          margin-bottom: 18px;
        }

        .account-toolbar label,
        .account-form label {
          display: block;
          margin-bottom: 8px;
          font-size: 13px;
          font-weight: 700;
          color: #756d67;
          text-transform: uppercase;
          letter-spacing: 0.05em;
        }

        .account-select,
        .account-input {
          width: 100%;
          border-radius: 14px;
          border: 1px solid rgba(31, 26, 23, 0.12);
          background: #fff;
          padding: 13px 14px;
          font-size: 15px;
          color: #1f1a17;
        }

        .account-table-wrap {
          overflow-x: auto;
        }

        .account-table {
          width: 100%;
          border-collapse: collapse;
        }

        .account-table th,
        .account-table td {
          text-align: left;
          padding: 14px 10px;
          border-bottom: 1px solid rgba(31, 26, 23, 0.08);
        }

        .account-table th {
          color: #7a716a;
          font-size: 12px;
          text-transform: uppercase;
          letter-spacing: 0.05em;
        }

        .account-name {
          font-weight: 700;
          color: #1f1a17;
        }

        .empty-state {
          border-radius: 20px;
          padding: 24px;
          background: #f8f5ef;
          border: 1px dashed rgba(31, 26, 23, 0.14);
          color: #665f58;
          line-height: 1.6;
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

        .modal-backdrop {
          position: fixed;
          inset: 0;
          background: rgba(17, 17, 17, 0.42);
          display: flex;
          align-items: center;
          justify-content: center;
          padding: 24px;
          z-index: 1000;
        }

        .modal-card {
          width: min(560px, 100%);
          background: #fff;
          border-radius: 24px;
          padding: 24px;
          box-shadow: 0 28px 60px rgba(17, 17, 17, 0.18);
        }

        .modal-title {
          margin: 0 0 8px;
          font-size: 24px;
          font-family: 'Instrument Serif', serif;
          font-weight: 400;
        }

        .modal-subtitle {
          margin: 0 0 20px;
          color: #6f6761;
        }

        .account-form {
          display: grid;
          gap: 16px;
        }

        .form-row {
          display: grid;
          gap: 16px;
          grid-template-columns: 1fr 1fr;
        }

        .modal-actions {
          display: flex;
          justify-content: flex-end;
          gap: 12px;
          margin-top: 8px;
        }

        @media (max-width: 720px) {
          .account-page {
            padding: 18px;
          }

          .account-topbar,
          .account-toolbar,
          .form-row {
            grid-template-columns: 1fr;
            flex-direction: column;
            align-items: stretch;
          }

          .account-actions,
          .modal-actions {
            flex-direction: column;
          }

          .btn-solid,
          .btn-ghost {
            width: 100%;
          }
        }
      `}</style>

      <div className="account-page">
        <div className="account-shell">
          <section className="account-topbar">
            <div>
              <h1 className="account-title">Accounts</h1>
              <p className="account-subtitle">Create QuickBooks accounts and review the accounts available for a connected company.</p>
            </div>

            <div className="account-actions">
              <button type="button" className="btn-ghost" onClick={() => navigate("/dashboard")}>
                Back to Dashboard
              </button>
              <button
                type="button"
                className="btn-solid"
                onClick={handleOpenModal}
                disabled={loadingCompanies || activeCompanies.length === 0}
              >
                Create Account
              </button>
            </div>
          </section>

          {success ? <div className="message success">{success}</div> : null}
          {error ? <div className="message error">{error}</div> : null}

          <section className="account-card">
            <div className="account-toolbar">
              <div style={{ minWidth: 280, flex: 1 }}>
                <label htmlFor="companyFilter">Connected Company</label>
                <select
                  id="companyFilter"
                  className="account-select"
                  value={selectedRealmId}
                  onChange={(e) => {
                    setSelectedRealmId(e.target.value);
                    setForm((current) => ({ ...current, realmId: e.target.value }));
                  }}
                  disabled={loadingCompanies || activeCompanies.length === 0}
                >
                  {activeCompanies.length === 0 ? (
                    <option value="">No connected companies</option>
                  ) : (
                    activeCompanies.map((company) => {
                      const realmId = company.realmId || company.RealmId;
                      const companyName = company.companyName || company.CompanyName;

                      return (
                        <option key={realmId} value={realmId}>
                          {companyName} ({realmId})
                        </option>
                      );
                    })
                  )}
                </select>
              </div>
            </div>

            {loadingCompanies || loadingAccounts ? (
              <div className="empty-state">Loading QuickBooks accounts...</div>
            ) : activeCompanies.length === 0 ? (
              <div className="empty-state">
                No connected companies are available. Connect a QuickBooks company first, then return here to create and view accounts.
              </div>
            ) : accounts.length === 0 ? (
              <div className="empty-state">
                No accounts were returned for the selected company. Use the Create Account button to add one in QuickBooks.
              </div>
            ) : (
              <div className="account-table-wrap">
                <table className="account-table">
                  <thead>
                    <tr>
                      <th>Account Name</th>
                      {/* <th>Account Number</th> */}
                      <th>Type</th>
                      <th>Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {accounts.map((account) => (
                      <tr key={account.id || account.Id}>
                        <td>
                          <div className="account-name">{account.name || account.Name}</div>
                        </td>
                        {/* <td>{account.acctNum || account.AcctNum || "-"}</td> */}
                        <td>{account.accountType || account.AccountType || "-"}</td>
                        <td>
                          <span className={`status-pill ${(account.active ?? account.Active) ? "active" : "inactive"}`}>
                            {(account.active ?? account.Active) ? "Active" : "Inactive"}
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </div>
      </div>

      {showModal ? (
        <div className="modal-backdrop" onClick={handleCloseModal}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <h2 className="modal-title">Create Account</h2>
            <p className="modal-subtitle">This will create the account directly in the selected QuickBooks company.</p>

            <form className="account-form" onSubmit={handleSubmit}>
              <div>
                <label htmlFor="modalCompany">Company</label>
                <select
                  id="modalCompany"
                  className="account-select"
                  value={form.realmId}
                  onChange={(e) => handleFormChange("realmId", e.target.value)}
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

              <div>
                <label htmlFor="accountName">Account Name</label>
                <input
                  id="accountName"
                  className="account-input"
                  value={form.name}
                  onChange={(e) => handleFormChange("name", e.target.value)}
                  placeholder="Enter account name"
                  maxLength={100}
                />
              </div>

              <div className="form-row">
                <div>
                  <label htmlFor="accountNumber">Account Number</label>
                  <input
                    id="accountNumber"
                    className="account-input"
                    value={form.acctNum}
                    onChange={(e) => handleFormChange("acctNum", e.target.value)}
                    placeholder="Optional account number"
                  />
                </div>

                <div>
                  <label htmlFor="accountType">Account Type</label>
                  <select
                    id="accountType"
                    className="account-select"
                    value={form.accountType}
                    onChange={(e) => handleFormChange("accountType", e.target.value)}
                  >
                    <option value="">Select account type</option>
                    {accountTypes.map((type) => (
                      <option key={type} value={type}>
                        {type}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="modal-actions">
                <button type="button" className="btn-ghost" onClick={handleCloseModal} disabled={submitting}>
                  Cancel
                </button>
                <button type="submit" className="btn-solid" disabled={submitting}>
                  {submitting ? "Creating..." : "Create Account"}
                </button>
              </div>
            </form>
          </div>
        </div>
      ) : null}
    </>
  );
};

export default Account;
