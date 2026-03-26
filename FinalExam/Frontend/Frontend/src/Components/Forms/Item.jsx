import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { authorizedFetch } from "../../auth/api";
import AppShell from "../AppShell";
import TablePagination from "../TablePagination";
import useBodyScrollLock from "../../hooks/useBodyScrollLock";

const itemTypes = ["Service", "Inventory", "NonInventory", "Category", "Bundle"];

const emptyForm = {
  id: "",
  syncToken: "",
  realmId: "",
  name: "",
  qtyOnHand: "",
  type: "",
  description: "",
  sku: "",
  unitPrice: "",
  incomeAccountRef: "",
  expenseAccountRef: "",
  assetAccountRef: "",
};

const Item = () => {
  const navigate = useNavigate();
  const [companies, setCompanies] = useState([]);
  const [selectedRealmId, setSelectedRealmId] = useState("");
  const [items, setItems] = useState([]);
  const [accounts, setAccounts] = useState([]);
  const [loadingCompanies, setLoadingCompanies] = useState(true);
  const [loadingItems, setLoadingItems] = useState(false);
  const [loadingAccounts, setLoadingAccounts] = useState(false);
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
  const filteredItems = items.filter((item) => {
    const query = search.trim().toLowerCase();
    const name = (item.name || item.Name || "").toLowerCase();
    const type = (item.type || item.Type || "").toLowerCase();
    const sku = (item.sku || item.Sku || "").toLowerCase();
    return !query || name.includes(query) || type.includes(query) || sku.includes(query);
  });
  const pagedItems = filteredItems.slice((page - 1) * pageSize, page * pageSize);

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
    const loadAccounts = async () => {
      if (!selectedRealmId) {
        setAccounts([]);
        return;
      }

      setLoadingAccounts(true);
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
          throw new Error(data.message || "Failed to load company accounts.");
        }

        setAccounts(Array.isArray(data) ? data : []);
      } catch (e) {
        setError(e.message || "Failed to load company accounts.");
      } finally {
        setLoadingAccounts(false);
      }
    };

    loadAccounts();
  }, [navigate, selectedRealmId]);

  useEffect(() => {
    const loadItems = async () => {
      if (!selectedRealmId) {
        setItems([]);
        return;
      }

      setLoadingItems(true);
      setError("");

      try {
        const res = await authorizedFetch(`/item?realmId=${encodeURIComponent(selectedRealmId)}`, {
          method: "GET",
        });
        if (res.status === 401) {
          navigate("/login");
          return;
        }

        const data = await res.json().catch(() => ({}));
        if (!res.ok) {
          throw new Error(data.message || "Failed to load QuickBooks items.");
        }

        setItems(Array.isArray(data) ? data : []);
        setPage(1);
      } catch (e) {
        setError(e.message || "Failed to load QuickBooks items.");
      } finally {
        setLoadingItems(false);
      }
    };

    loadItems();
  }, [navigate, selectedRealmId]);

  const refreshItems = async (realmId) => {
    const res = await authorizedFetch(`/item?realmId=${encodeURIComponent(realmId)}`, {
      method: "GET",
    });

    if (res.status === 401) {
      navigate("/login");
      return;
    }

    const data = await res.json().catch(() => ([]));
    if (!res.ok) {
      const message = data.message || "Failed to load QuickBooks items.";
      throw new Error(message);
    }

    setItems(Array.isArray(data) ? data : []);
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

  const handleOpenEdit = (item) => {
    setIsEditing(true);
    setError("");
    setSuccess("");
    setForm({
      id: item.id || item.Id,
      syncToken: item.syncToken || item.SyncToken,
      realmId: item.realmId || item.RealmId,
      name: item.name || item.Name || "",
      qtyOnHand: (item.qtyOnHand ?? item.QtyOnHand ?? "").toString(),
      type: item.type || item.Type || "",
      description: item.description || item.Description || "",
      sku: item.sku || item.Sku || "",
      unitPrice: (item.unitPrice ?? item.UnitPrice ?? "").toString(),
      incomeAccountRef: "",
      expenseAccountRef: "",
      assetAccountRef: "",
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

    if (!form.realmId || !form.name.trim()) {
      setError("Company and item name are required.");
      return;
    }

    if (form.name.includes(":") || form.name.includes("\t") || form.name.includes("\n") || form.name.includes("\r")) {
      setError("Item name cannot contain colons, tabs, or new lines.");
      return;
    }

    if (!form.type.trim()) {
      setError("Item type is required.");
      return;
    }

    if (!isEditing && !form.incomeAccountRef) {
      setError("Income account is required.");
      return;
    }

    if (form.qtyOnHand !== "" && Number(form.qtyOnHand) < 0) {
      setError("Quantity on hand cannot be negative.");
      return;
    }

    if (form.unitPrice !== "" && Number(form.unitPrice) < 0) {
      setError("Unit price cannot be negative.");
      return;
    }

    if (!isEditing && form.type.trim().toLowerCase() === "inventory" && (!form.expenseAccountRef || !form.assetAccountRef)) {
      setError("Inventory items require expense and asset accounts.");
      return;
    }

    setSubmitting(true);
    try {
      const qtyValue = form.qtyOnHand === "" ? null : Number(form.qtyOnHand);
      const unitPriceValue = form.unitPrice === "" ? null : Number(form.unitPrice);
      const payload = {
        realmId: form.realmId,
        name: form.name.trim(),
        qtyOnHand: Number.isNaN(qtyValue) ? null : qtyValue,
        type: form.type.trim() || null,
        description: form.description.trim() || null,
        sku: form.sku.trim() || null,
        unitPrice: Number.isNaN(unitPriceValue) ? null : unitPriceValue,
        incomeAccountRef: form.incomeAccountRef || null,
        expenseAccountRef: form.expenseAccountRef || null,
        assetAccountRef: form.assetAccountRef || null,
        syncToken: form.syncToken,
      };

      const endpoint = isEditing ? `/item/${encodeURIComponent(form.id)}` : "/item";
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
        throw new Error(data.message || `Failed to ${isEditing ? "update" : "create"} item.`);
      }

      setSelectedRealmId(form.realmId);
      setShowModal(false);
      setSuccess(`Item ${isEditing ? "updated" : "created"} successfully in QuickBooks.`);
      await refreshItems(form.realmId);
    } catch (e) {
      setError(e.message || `Failed to ${isEditing ? "update" : "create"} item.`);
    } finally {
      setSubmitting(false);
    }
  };

  const incomeAccounts = accounts.filter((account) => {
    const type = (account.accountType || account.AccountType || "").toLowerCase();
    return type.includes("income");
  });

  const expenseAccounts = accounts.filter((account) => {
    const type = (account.accountType || account.AccountType || "").toLowerCase();
    return type.includes("cost of goods sold") || type.includes("expense");
  });

  const assetAccounts = accounts.filter((account) => {
    const type = (account.accountType || account.AccountType || "").toLowerCase();
    return type.includes("asset");
  });

  const handleDelete = async (item) => {
    const realmId = item.realmId || item.RealmId;
    const itemId = item.id || item.Id;
    const syncToken = item.syncToken || item.SyncToken;
    const itemName = item.name || item.Name || "this item";

    const confirmed = window.confirm(`Delete ${itemName} from QuickBooks?`);
    if (!confirmed) return;

    setError("");
    setSuccess("");
    setSubmitting(true);

    try {
      const res = await authorizedFetch(
        `/item/${encodeURIComponent(itemId)}?realmId=${encodeURIComponent(realmId)}&syncToken=${encodeURIComponent(syncToken)}`,
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
        throw new Error(data.message || "Failed to delete item.");
      }

      setSuccess("Item deleted successfully from QuickBooks.");
      await refreshItems(realmId);
    } catch (e) {
      setError(e.message || "Failed to delete item.");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <>
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;700&family=Instrument+Serif:ital@0;1&display=swap');

        .item-page { font-family: 'DM Sans', sans-serif; color: #1f1a17; }

        .item-shell {
          max-width: 1240px;
          margin: 0 auto;
        }

        .item-topbar,
        .item-card {
          background: rgba(255, 255, 255, 0.88);
          border: 1px solid rgba(31, 26, 23, 0.06);
          border-radius: 24px;
          box-shadow: 0 18px 34px rgba(31, 26, 23, 0.08);
        }

        .item-topbar {
          display: flex;
          justify-content: space-between;
          align-items: center;
          gap: 18px;
          padding: 24px;
        }

        .item-title {
          font-family: 'Instrument Serif', serif;
          font-size: 34px;
          line-height: 1;
          margin: 0 0 8px;
          font-weight: 400;
        }

        .item-subtitle {
          margin: 0;
          color: #6f6761;
          font-size: 15px;
        }

        .item-actions {
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

        .item-card {
          margin-top: 22px;
          padding: 20px;
        }

        .item-toolbar {
          display: flex;
          justify-content: space-between;
          align-items: end;
          gap: 18px;
          margin-bottom: 18px;
        }

        .item-toolbar label,
        .item-form label {
          display: block;
          margin-bottom: 8px;
          font-size: 13px;
          font-weight: 700;
          color: #756d67;
          text-transform: uppercase;
          letter-spacing: 0.05em;
        }

        .item-select,
        .item-input {
          width: 100%;
          border-radius: 14px;
          border: 1px solid rgba(31, 26, 23, 0.12);
          background: #fff;
          padding: 13px 14px;
          font-size: 15px;
          color: #1f1a17;
        }

        .item-table-wrap {
          overflow-x: auto;
        }

        .item-table {
          width: 100%;
          border-collapse: collapse;
        }

        .item-table th,
        .item-table td {
          text-align: left;
          padding: 14px 10px;
          border-bottom: 1px solid rgba(31, 26, 23, 0.08);
          vertical-align: middle;
        }

        .item-table th {
          color: #7a716a;
          font-size: 12px;
          text-transform: uppercase;
          letter-spacing: 0.05em;
        }

        .item-name {
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
          max-height: min(88vh, 880px);
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

        .item-form {
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
          .item-topbar,
          .item-toolbar,
          .form-row {
            grid-template-columns: 1fr;
            flex-direction: column;
            align-items: stretch;
          }

          .item-actions,
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

      <AppShell activeKey="item">
      <div className="item-page">
        <div className="item-shell">
          <section className="item-topbar">
            <div>
              <h1 className="item-title">Items</h1>
              <p className="item-subtitle">Create, update, and delete QuickBooks items for a connected company.</p>
            </div>

            <div className="item-actions">
              <button type="button" className="btn-ghost" onClick={() => navigate("/dashboard")}>
                Back to Dashboard
              </button>
              <button
                type="button"
                className="btn-solid"
                onClick={handleOpenCreate}
                disabled={loadingCompanies || activeCompanies.length === 0}
              >
                New Item
              </button>
            </div>
          </section>

          {success ? <div className="message success">{success}</div> : null}
          {error ? <div className="message error">{error}</div> : null}

          <section className="item-card">
            <div className="item-toolbar">
              <div style={{ minWidth: 280, flex: 1 }}>
                <label htmlFor="companyFilter">Connected Company</label>
                <select
                  id="companyFilter"
                  className="item-select"
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
                placeholder="Search items..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
            </div>

            {loadingCompanies || loadingItems ? (
              <div className="empty-state">Loading QuickBooks items...</div>
            ) : activeCompanies.length === 0 ? (
              <div className="empty-state">
                No connected companies are available. Connect a QuickBooks company first, then return here to manage items.
              </div>
            ) : items.length === 0 ? (
              <div className="empty-state">
                No items were returned for the selected company. Use the New Item button to create one in QuickBooks.
              </div>
            ) : (
              <div className="item-table-wrap">
                <table className="item-table">
                  <thead>
                    <tr>
                      <th>Item Name</th>
                      <th>Type</th>
                      <th>Income Account</th>
                      {/* <th>Unit Price</th> */}
                      {/* <th>Status</th> */}
                      <th>Action</th>
                    </tr>
                  </thead>
                  <tbody>
                    {pagedItems.map((item) => (
                      <tr key={item.id || item.Id}>
                        <td>
                          <div className="item-name">{item.name || item.Name}</div>
                        </td>
                        <td>{item.type || item.Type || "-"}</td>
                        <td>
                          {item.incomeAccountName || item.IncomeAccountName || "-"}
                        </td>
                        {/* <td>{item.unitPrice ?? item.UnitPrice ?? "-"}</td> */}
                        {/* <td>
                          <span className={`status-pill ${(item.active ?? item.Active) ? "active" : "inactive"}`}>
                            {(item.active ?? item.Active) ? "Active" : "Inactive"}
                          </span>
                        </td> */}
                        <td>
                          <div className="row-actions">
                            <button
                              type="button"
                              className="btn-ghost"
                              onClick={() => handleOpenEdit(item)}
                              disabled={submitting}
                            >
                              Edit
                            </button>
                            <button
                              type="button"
                              className="btn-danger"
                              onClick={() => handleDelete(item)}
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
              totalItems={filteredItems.length}
              onPageChange={setPage}
            />
          </section>
        </div>
      </div>

      {showModal ? (
        <div className="modal-backdrop" style={{}} onClick={handleCloseModal}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <h2 className="modal-title">{isEditing ? "Edit Item" : "Create Item"}</h2>
            <p className="modal-subtitle">
              {isEditing
                ? "Update the item directly in the selected QuickBooks company."
                : "This will create the item directly in the selected QuickBooks company."}
            </p>

            <form className="item-form" onSubmit={handleSubmit}>
              <div>
                <label htmlFor="modalCompany">Company</label>
                <select
                  id="modalCompany"
                  className="item-select"
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
                <label htmlFor="itemName">Item Name</label>
                <input
                  id="itemName"
                  className="item-input"
                  value={form.name}
                  onChange={(e) => handleFormChange("name", e.target.value)}
                  placeholder="Item name"
                />
              </div>

              <div className="form-row">
                <div>
                  <label htmlFor="qtyOnHand">Quantity On Hand</label>
                  <input
                    id="qtyOnHand"
                    type="number"
                    step="0.01"
                    className="item-input"
                    value={form.qtyOnHand}
                    onChange={(e) => handleFormChange("qtyOnHand", e.target.value)}
                    placeholder="0"
                    disabled={isEditing}
                  />
                </div>

                <div>
                  <label htmlFor="itemType">Type</label>
                  <select
                    id="itemType"
                    className="item-select"
                    value={form.type}
                    onChange={(e) => handleFormChange("type", e.target.value)}
                    disabled={isEditing}
                  >
                    <option value="">Select item type</option>
                    {itemTypes.map((type) => (
                      <option key={type} value={type}>
                        {type}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="form-row">
                <div>
                  <label htmlFor="itemSku">SKU</label>
                  <input
                    id="itemSku"
                    className="item-input"
                    value={form.sku}
                    onChange={(e) => handleFormChange("sku", e.target.value)}
                    placeholder="Optional SKU"
                  />
                </div>

                <div>
                  <label htmlFor="unitPrice">Unit Price</label>
                  <input
                    id="unitPrice"
                    type="number"
                    step="0.01"
                    min="0"
                    className="item-input"
                    value={form.unitPrice}
                    onChange={(e) => handleFormChange("unitPrice", e.target.value)}
                    placeholder="0.00"
                  />
                </div>
              </div>

              <div>
                <label htmlFor="itemDescription">Description</label>
                <input
                  id="itemDescription"
                  className="item-input"
                  value={form.description}
                  onChange={(e) => handleFormChange("description", e.target.value)}
                  placeholder="Optional description"
                />
              </div>

              {!isEditing ? (
                <>
                  <div>
                    <label htmlFor="incomeAccountRef">Income Account</label>
                    <select
                      id="incomeAccountRef"
                      className="item-select"
                      value={form.incomeAccountRef}
                      onChange={(e) => handleFormChange("incomeAccountRef", e.target.value)}
                      disabled={loadingAccounts}
                    >
                      <option value="">Select income account</option>
                      {incomeAccounts.map((account) => {
                        const accountId = account.id || account.Id;
                        const accountName = account.name || account.Name;
                        return (
                          <option key={accountId} value={accountId}>
                            {accountName}
                          </option>
                        );
                      })}
                    </select>
                  </div>

                  {form.type.trim().toLowerCase() === "inventory" ? (
                    <div className="form-row">
                      <div>
                        <label htmlFor="expenseAccountRef">Expense Account</label>
                        <select
                          id="expenseAccountRef"
                          className="item-select"
                          value={form.expenseAccountRef}
                          onChange={(e) => handleFormChange("expenseAccountRef", e.target.value)}
                          disabled={loadingAccounts}
                        >
                          <option value="">Select expense account</option>
                          {expenseAccounts.map((account) => {
                            const accountId = account.id || account.Id;
                            const accountName = account.name || account.Name;
                            return (
                              <option key={accountId} value={accountId}>
                                {accountName}
                              </option>
                            );
                          })}
                        </select>
                      </div>

                      <div>
                        <label htmlFor="assetAccountRef">Asset Account</label>
                        <select
                          id="assetAccountRef"
                          className="item-select"
                          value={form.assetAccountRef}
                          onChange={(e) => handleFormChange("assetAccountRef", e.target.value)}
                          disabled={loadingAccounts}
                        >
                          <option value="">Select asset account</option>
                          {assetAccounts.map((account) => {
                            const accountId = account.id || account.Id;
                            const accountName = account.name || account.Name;
                            return (
                              <option key={accountId} value={accountId}>
                                {accountName}
                              </option>
                            );
                          })}
                        </select>
                      </div>
                    </div>
                  ) : null}
                </>
              ) : null}

              {isEditing ? (
                <div className="empty-state" style={{ padding: 16 }}>
                  QuickBooks does not allow normal edit flow changes for item type, and quantity on hand should be changed through inventory adjustment.
                </div>
              ) : null}

              <div className="modal-actions">
                <button type="button" className="btn-ghost" onClick={handleCloseModal} disabled={submitting}>
                  Cancel
                </button>
                <button type="submit" className="btn-solid" disabled={submitting}>
                  {submitting ? (isEditing ? "Updating..." : "Creating...") : isEditing ? "Update Item" : "Create Item"}
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

export default Item;
