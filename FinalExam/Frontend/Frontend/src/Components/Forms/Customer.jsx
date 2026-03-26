import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { authorizedFetch } from "../../auth/api";
import AppShell from "../AppShell";
import TablePagination from "../TablePagination";
import useBodyScrollLock from "../../hooks/useBodyScrollLock";

const emptyForm = {
  id: "",
  syncToken: "",
  realmId: "",
  displayName: "",
  title: "",
  givenName: "",
  middleName: "",
  familyName: "",
  suffix: "",
  companyName: "",
  email: "",
  phone: "",
  mobile: "",
  billAddrLine1: "",
  billAddrCity: "",
  billAddrPostalCode: "",
  billAddrCountrySubDivisionCode: "",
};

const Customer = () => {
  const navigate = useNavigate();
  const [companies, setCompanies] = useState([]);
  const [selectedRealmId, setSelectedRealmId] = useState("");
  const [customers, setCustomers] = useState([]);
  const [loadingCompanies, setLoadingCompanies] = useState(true);
  const [loadingCustomers, setLoadingCustomers] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [showModal, setShowModal] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [form, setForm] = useState(emptyForm);

  const activeCompanies = companies.filter((company) => company.isActive ?? company.IsActive);
  const pageSize = 8;
  const filteredCustomers = customers.filter((customer) => {
    const query = search.trim().toLowerCase();
    const displayName = (customer.displayName || customer.DisplayName || "").toLowerCase();
    const email = (customer.email || customer.Email || "").toLowerCase();
    const phone = (customer.phone || customer.Phone || "").toLowerCase();
    return !query || displayName.includes(query) || email.includes(query) || phone.includes(query);
  });
  const pagedCustomers = filteredCustomers.slice((page - 1) * pageSize, page * pageSize);

  useBodyScrollLock(showModal);

  useEffect(() => {
    setPage(1);
  }, [search]);

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
    const loadCustomers = async () => {
      if (!selectedRealmId) {
        setCustomers([]);
        return;
      }

      setLoadingCustomers(true);
      setError("");

      try {
        const res = await authorizedFetch(`/customer?realmId=${encodeURIComponent(selectedRealmId)}`, {
          method: "GET",
        });
        if (res.status === 401) {
          navigate("/login");
          return;
        }

        const data = await res.json().catch(() => ({}));
        if (!res.ok) {
          throw new Error(data.message || "Failed to load QuickBooks customers.");
        }

        setCustomers(Array.isArray(data) ? data : []);
        setPage(1);
      } catch (e) {
        setError(e.message || "Failed to load QuickBooks customers.");
      } finally {
        setLoadingCustomers(false);
      }
    };

    loadCustomers();
  }, [navigate, selectedRealmId]);

  const refreshCustomers = async (realmId) => {
    const res = await authorizedFetch(`/customer?realmId=${encodeURIComponent(realmId)}`, {
      method: "GET",
    });

    if (res.status === 401) {
      navigate("/login");
      return;
    }

    const data = await res.json().catch(() => ([]));
    if (!res.ok) {
      const message = data.message || "Failed to load QuickBooks customers.";
      throw new Error(message);
    }

    setCustomers(Array.isArray(data) ? data : []);
    setPage(1);
  };

  const handleOpenCreate = () => {
    setIsEditing(false);
    setError("");
    setSuccess("");
    setForm({
      ...emptyForm,
      realmId: selectedRealmId || activeCompanies[0]?.realmId || activeCompanies[0]?.RealmId || "",
    });
    setShowModal(true);
  };

  const handleOpenEdit = (customer) => {
    setIsEditing(true);
    setError("");
    setSuccess("");
    setForm({
      id: customer.id || customer.Id,
      syncToken: customer.syncToken || customer.SyncToken,
      realmId: customer.realmId || customer.RealmId,
      displayName: customer.displayName || customer.DisplayName || "",
      title: customer.title || customer.Title || "",
      givenName: customer.givenName || customer.GivenName || "",
      middleName: customer.middleName || customer.MiddleName || "",
      familyName: customer.familyName || customer.FamilyName || "",
      suffix: customer.suffix || customer.Suffix || "",
      companyName: customer.customerCompanyName || customer.CustomerCompanyName || "",
      email: customer.email || customer.Email || "",
      phone: customer.phone || customer.Phone || "",
      mobile: customer.mobile || customer.Mobile || "",
      billAddrLine1: customer.billAddrLine1 || customer.BillAddrLine1 || "",
      billAddrCity: customer.billAddrCity || customer.BillAddrCity || "",
      billAddrPostalCode: customer.billAddrPostalCode || customer.BillAddrPostalCode || "",
      billAddrCountrySubDivisionCode: customer.billAddrCountrySubDivisionCode || customer.BillAddrCountrySubDivisionCode || "",
    });
    setShowModal(true);
  };

  const handleCloseModal = () => {
    setShowModal(false);
    setForm(emptyForm);
  };

  const handleFormChange = (field, value) => {
    setForm((current) => ({ ...current, [field]: value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setSuccess("");

    if (!form.realmId) {
      setError("Please select a connected company.");
      return;
    }

    if (!form.displayName.trim() && !form.givenName.trim() && !form.familyName.trim()) {
      setError("Display name or at least one customer name field is required.");
      return;
    }

    if (form.email.trim() && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email.trim())) {
      setError("Customer email format is invalid.");
      return;
    }

    setSubmitting(true);
    try {
      const payload = {
        realmId: form.realmId,
        displayName: form.displayName.trim() || null,
        title: form.title.trim() || null,
        givenName: form.givenName.trim() || null,
        middleName: form.middleName.trim() || null,
        familyName: form.familyName.trim() || null,
        suffix: form.suffix.trim() || null,
        companyName: form.companyName.trim() || null,
        email: form.email.trim() || null,
        phone: form.phone.trim() || null,
        mobile: form.mobile.trim() || null,
        billAddrLine1: form.billAddrLine1.trim() || null,
        billAddrCity: form.billAddrCity.trim() || null,
        billAddrPostalCode: form.billAddrPostalCode.trim() || null,
        billAddrCountrySubDivisionCode: form.billAddrCountrySubDivisionCode.trim() || null,
        syncToken: form.syncToken,
      };

      const endpoint = isEditing ? `/customer/${encodeURIComponent(form.id)}` : "/customer";
      const method = isEditing ? "PUT" : "POST";

      const res = await authorizedFetch(endpoint, {
        method,
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(payload),
      });

      if (res.status === 401) {
        navigate("/login");
        return;
      }

      const data = await res.json().catch(() => ({}));
      if (!res.ok) {
        throw new Error(data.message || `Failed to ${isEditing ? "update" : "create"} customer.`);
      }

      setSelectedRealmId(form.realmId);
      setShowModal(false);
      setSuccess(`Customer ${isEditing ? "updated" : "created"} successfully in QuickBooks.`);
      await refreshCustomers(form.realmId);
    } catch (e) {
      setError(e.message || `Failed to ${isEditing ? "update" : "create"} customer.`);
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (customer) => {
    const realmId = customer.realmId || customer.RealmId;
    const customerId = customer.id || customer.Id;
    const syncToken = customer.syncToken || customer.SyncToken;
    const customerName = customer.displayName || customer.DisplayName || "this customer";

    const confirmed = window.confirm(`Delete ${customerName} from QuickBooks?`);
    if (!confirmed) return;

    setError("");
    setSuccess("");
    setSubmitting(true);

    try {
      const res = await authorizedFetch(
        `/customer/${encodeURIComponent(customerId)}?realmId=${encodeURIComponent(realmId)}&syncToken=${encodeURIComponent(syncToken)}`,
        {
          method: "DELETE",
        }
      );

      if (res.status === 401) {
        navigate("/login");
        return;
      }

      const data = await res.json().catch(() => ({}));
      if (!res.ok) {
        throw new Error(data.message || "Failed to delete customer.");
      }

      setSuccess("Customer deleted successfully from QuickBooks.");
      await refreshCustomers(realmId);
    } catch (e) {
      setError(e.message || "Failed to delete customer.");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <>
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;700&family=Instrument+Serif:ital@0;1&display=swap');

        .customer-page { font-family: 'DM Sans', sans-serif; color: #1f1a17; }

        .customer-shell {
          max-width: 1240px;
          margin: 0 auto;
        }

        .customer-topbar,
        .customer-card {
          background: rgba(255, 255, 255, 0.88);
          border: 1px solid rgba(31, 26, 23, 0.06);
          border-radius: 24px;
          box-shadow: 0 18px 34px rgba(31, 26, 23, 0.08);
        }

        .customer-topbar {
          display: flex;
          justify-content: space-between;
          align-items: center;
          gap: 18px;
          padding: 24px;
        }

        .customer-title {
          font-family: 'Instrument Serif', serif;
          font-size: 34px;
          line-height: 1;
          margin: 0 0 8px;
          font-weight: 400;
        }

        .customer-subtitle {
          margin: 0;
          color: #6f6761;
          font-size: 15px;
        }

        .customer-actions {
          display: flex;
          gap: 12px;
          align-items: center;
        }

        .btn-solid,
        .btn-ghost,
        .btn-danger {
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

        .btn-danger {
          border: none;
          background: #bf3d35;
          color: #fff;
        }

        .btn-solid:disabled,
        .btn-ghost:disabled,
        .btn-danger:disabled {
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

        .customer-card {
          margin-top: 22px;
          padding: 20px;
        }

        .customer-toolbar {
          display: flex;
          justify-content: space-between;
          align-items: end;
          gap: 18px;
          margin-bottom: 18px;
        }

        .customer-toolbar label,
        .customer-form label {
          display: block;
          margin-bottom: 8px;
          font-size: 13px;
          font-weight: 700;
          color: #756d67;
          text-transform: uppercase;
          letter-spacing: 0.05em;
        }

        .customer-select,
        .customer-input {
          width: 100%;
          border-radius: 14px;
          border: 1px solid rgba(31, 26, 23, 0.12);
          background: #fff;
          padding: 13px 14px;
          font-size: 15px;
          color: #1f1a17;
        }

        .customer-table-wrap {
          overflow-x: auto;
        }

        .customer-table {
          width: 100%;
          border-collapse: collapse;
        }

        .customer-table th,
        .customer-table td {
          text-align: left;
          padding: 14px 10px;
          border-bottom: 1px solid rgba(31, 26, 23, 0.08);
          vertical-align: middle;
        }

        .customer-table th {
          color: #7a716a;
          font-size: 12px;
          text-transform: uppercase;
          letter-spacing: 0.05em;
        }

        .customer-name {
          font-weight: 700;
          color: #1f1a17;
        }

        .customer-meta {
          font-size: 13px;
          color: #766e67;
          margin-top: 3px;
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

        .row-actions {
          display: flex;
          gap: 10px;
          flex-wrap: wrap;
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
          width: min(620px, 100%);
          background: #fff;
          border-radius: 24px;
          padding: 24px;
          box-shadow: 0 28px 60px rgba(17, 17, 17, 0.18);
          max-height: min(88vh, 860px);
          overflow-y: auto;
          scrollbar-width: none;
          -ms-overflow-style: none;
        }

        .modal-card::-webkit-scrollbar {
          width: 0;
          height: 0;
          display: none;
        }

        .table-search {
          width: min(360px, 100%);
          border-radius: 14px;
          border: 1px solid rgba(31, 26, 23, 0.12);
          background: #fff;
          padding: 12px 14px;
          font-size: 14px;
          color: #1f1a17;
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

        .customer-form {
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
          .customer-topbar,
          .customer-toolbar,
          .form-row {
            grid-template-columns: 1fr;
            flex-direction: column;
            align-items: stretch;
          }

          .customer-actions,
          .modal-actions,
          .row-actions {
            flex-direction: column;
          }

          .btn-solid,
          .btn-ghost,
          .btn-danger {
            width: 100%;
          }
        }
      `}</style>

      <AppShell activeKey="customer">
      <div className="customer-page">
        <div className="customer-shell">
          <section className="customer-topbar">
            <div>
              <h1 className="customer-title">Customers</h1>
              <p className="customer-subtitle">Create, update, and delete QuickBooks customers for a connected company.</p>
            </div>

            <div className="customer-actions">
              <button type="button" className="btn-ghost" onClick={() => navigate("/dashboard")}>
                Back to Dashboard
              </button>
              <button
                type="button"
                className="btn-solid"
                onClick={handleOpenCreate}
                disabled={loadingCompanies || activeCompanies.length === 0}
              >
                New Customer
              </button>
            </div>
          </section>

          {success ? <div className="message success">{success}</div> : null}
          {error ? <div className="message error">{error}</div> : null}

          <section className="customer-card">
            <div className="customer-toolbar">
              <div style={{ minWidth: 280, flex: 1 }}>
                <label htmlFor="companyFilter">Connected Company</label>
                <select
                  id="companyFilter"
                  className="customer-select"
                  value={selectedRealmId}
                  onChange={(e) => setSelectedRealmId(e.target.value)}
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
              <input
                className="table-search"
                placeholder="Search customers..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
            </div>

            {loadingCompanies || loadingCustomers ? (
              <div className="empty-state">Loading QuickBooks customers...</div>
            ) : activeCompanies.length === 0 ? (
              <div className="empty-state">
                No connected companies are available. Connect a QuickBooks company first, then return here to manage customers.
              </div>
            ) : customers.length === 0 ? (
              <div className="empty-state">
                No customers were returned for the selected company. Use the New Customer button to create one in QuickBooks.
              </div>
            ) : (
              <div className="customer-table-wrap">
                <table className="customer-table">
                  <thead>
                    <tr>
                      <th>Customer</th>
                      <th>Email</th>
                      <th>Phone</th>
                      <th>Company</th>
                      {/* <th>Status</th> */}
                      <th>Action</th>
                    </tr>
                  </thead>
                  <tbody>
                    {pagedCustomers.map((customer) => (
                      <tr key={customer.id || customer.Id}>
                        <td>
                          <div className="customer-name">{customer.displayName || customer.DisplayName}</div>
                          <div className="customer-meta">
                            {[customer.givenName || customer.GivenName, customer.familyName || customer.FamilyName]
                              .filter(Boolean)
                              .join(" ") || "No first or last name"}
                          </div>
                        </td>
                        <td>{customer.email || customer.Email || "-"}</td>
                        <td>{customer.phone || customer.Phone || "-"}</td>
                        <td>{customer.customerCompanyName || customer.CustomerCompanyName || "-"}</td>
                        {/* <td>
                          <span className={`status-pill ${(customer.active ?? customer.Active) ? "active" : "inactive"}`}>
                            {(customer.active ?? customer.Active) ? "Active" : "Inactive"}
                          </span>
                        </td> */}
                        <td>
                          <div className="row-actions">
                            <button
                              type="button"
                              className="btn-ghost"
                              onClick={() => handleOpenEdit(customer)}
                              disabled={submitting}
                            >
                              Edit
                            </button>
                            <button
                              type="button"
                              className="btn-danger"
                              onClick={() => handleDelete(customer)}
                              disabled={submitting}
                            >
                              Delete
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            <TablePagination
              page={page}
              pageSize={pageSize}
              totalItems={filteredCustomers.length}
              onPageChange={setPage}
            />
          </section>
        </div>
      </div>

      {showModal ? (
        <div className="modal-backdrop" onClick={handleCloseModal}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <h2 className="modal-title">{isEditing ? "Edit Customer" : "Create Customer"}</h2>
            <p className="modal-subtitle">
              {isEditing
                ? "Update the customer directly in the selected QuickBooks company."
                : "This will create the customer directly in the selected QuickBooks company."}
            </p>

            <form className="customer-form" onSubmit={handleSubmit}>
              <div>
                <label htmlFor="modalCompany">Company</label>
                <select
                  id="modalCompany"
                  className="customer-select"
                  value={form.realmId}
                  onChange={(e) => handleFormChange("realmId", e.target.value)}
                  disabled={isEditing}
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
                <label htmlFor="displayName">Display Name</label>
                <input
                  id="displayName"
                  className="customer-input"
                  value={form.displayName}
                  onChange={(e) => handleFormChange("displayName", e.target.value)}
                  placeholder="Display name"
                />
              </div>

              <div className="form-row">
                <div>
                  <label htmlFor="title">Title</label>
                  <input
                    id="title"
                    className="customer-input"
                    value={form.title}
                    onChange={(e) => handleFormChange("title", e.target.value)}
                    placeholder="Title"
                  />
                </div>

                <div>
                  <label htmlFor="givenName">First Name</label>
                  <input
                    id="givenName"
                    className="customer-input"
                    value={form.givenName}
                    onChange={(e) => handleFormChange("givenName", e.target.value)}
                    placeholder="First name"
                  />
                </div>

                <div>
                  <label htmlFor="middleName">Middle Name</label>
                  <input
                    id="middleName"
                    className="customer-input"
                    value={form.middleName}
                    onChange={(e) => handleFormChange("middleName", e.target.value)}
                    placeholder="Middle name"
                  />
                </div>

                <div>
                  <label htmlFor="familyName">Last Name</label>
                  <input
                    id="familyName"
                    className="customer-input"
                    value={form.familyName}
                    onChange={(e) => handleFormChange("familyName", e.target.value)}
                    placeholder="Last name"
                  />
                </div>
              </div>

              <div className="form-row">
                <div>
                  <label htmlFor="suffix">Suffix</label>
                  <input
                    id="suffix"
                    className="customer-input"
                    value={form.suffix}
                    onChange={(e) => handleFormChange("suffix", e.target.value)}
                    placeholder="Suffix"
                  />
                </div>

                <div>
                  <label htmlFor="companyName">Company Name</label>
                  <input
                    id="companyName"
                    className="customer-input"
                    value={form.companyName}
                    onChange={(e) => handleFormChange("companyName", e.target.value)}
                    placeholder="Company name"
                  />
                </div>
              </div>

              <div className="form-row">
                <div>
                  <label htmlFor="email">Email</label>
                  <input
                    id="email"
                    type="email"
                    className="customer-input"
                    value={form.email}
                    onChange={(e) => handleFormChange("email", e.target.value)}
                    placeholder="customer@example.com"
                  />
                </div>

                <div>
                  <label htmlFor="phone">Phone</label>
                  <input
                    id="phone"
                    className="customer-input"
                    value={form.phone}
                    onChange={(e) => handleFormChange("phone", e.target.value)}
                    placeholder="Phone number"
                  />
                </div>
              </div>

              <div className="form-row">
                <div>
                  <label htmlFor="mobile">Mobile</label>
                  <input
                    id="mobile"
                    className="customer-input"
                    value={form.mobile}
                    onChange={(e) => handleFormChange("mobile", e.target.value)}
                    placeholder="Mobile number"
                  />
                </div>

                <div>
                  <label htmlFor="billAddrLine1">Address Line 1</label>
                  <input
                    id="billAddrLine1"
                    className="customer-input"
                    value={form.billAddrLine1}
                    onChange={(e) => handleFormChange("billAddrLine1", e.target.value)}
                    placeholder="Street address"
                  />
                </div>
              </div>

              <div className="form-row">
                <div>
                  <label htmlFor="billAddrCity">City</label>
                  <input
                    id="billAddrCity"
                    className="customer-input"
                    value={form.billAddrCity}
                    onChange={(e) => handleFormChange("billAddrCity", e.target.value)}
                    placeholder="City"
                  />
                </div>

                <div>
                  <label htmlFor="billAddrPostalCode">Postal Code</label>
                  <input
                    id="billAddrPostalCode"
                    className="customer-input"
                    value={form.billAddrPostalCode}
                    onChange={(e) => handleFormChange("billAddrPostalCode", e.target.value)}
                    placeholder="Postal code"
                  />
                </div>
              </div>

              <div>
                <label htmlFor="billAddrCountrySubDivisionCode">State / Province Code</label>
                <input
                  id="billAddrCountrySubDivisionCode"
                  className="customer-input"
                  value={form.billAddrCountrySubDivisionCode}
                  onChange={(e) => handleFormChange("billAddrCountrySubDivisionCode", e.target.value)}
                  placeholder="State or province code"
                />
              </div>

              <div className="modal-actions">
                <button type="button" className="btn-ghost" onClick={handleCloseModal} disabled={submitting}>
                  Cancel
                </button>
                <button type="submit" className="btn-solid" disabled={submitting}>
                  {submitting ? (isEditing ? "Updating..." : "Creating...") : isEditing ? "Update Customer" : "Create Customer"}
                </button>
              </div>
            </form>
          </div>
        </div>
      ) : null}
      </AppShell>
    </>
  );
};

export default Customer;
