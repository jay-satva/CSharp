import React, { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { authorizedFetch } from "../../auth/api";
import AppShell from "../AppShell";

const emptyLineItem = {
  itemRef: "",
  itemName: "",
  description: "",
  quantity: "1",
  unitPrice: "0",
};

const buildInitialForm = () => ({
  realmId: "",
  customerRef: "",
  accountRef: "",
  invoiceDate: new Date().toISOString().slice(0, 10),
  dueDate: "",
  memo: "",
  lineItems: [{ ...emptyLineItem }],
});

const Invoice = () => {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);

  const [companies, setCompanies] = useState([]);
  const [customers, setCustomers] = useState([]);
  const [items, setItems] = useState([]);
  const [accounts, setAccounts] = useState([]);
  const [form, setForm] = useState(buildInitialForm);
  const [loadingCompanies, setLoadingCompanies] = useState(true);
  const [loadingLookups, setLoadingLookups] = useState(false);
  const [loadingInvoice, setLoadingInvoice] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  const activeCompanies = companies.filter((company) => company.isActive ?? company.IsActive);
  const arAccounts = accounts.filter((account) => {
    const type = (account.accountType || account.AccountType || "").toLowerCase();
    return type.includes("receivable");
  });

  const totalAmount = useMemo(
    () =>
      form.lineItems.reduce((sum, lineItem) => {
        const qty = Number(lineItem.quantity || 0);
        const unitPrice = Number(lineItem.unitPrice || 0);
        return sum + qty * unitPrice;
      }, 0),
    [form.lineItems]
  );

  useEffect(() => {
    const loadCompanies = async () => {
      setLoadingCompanies(true);
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

        setCompanies(Array.isArray(data) ? data : []);
      } catch (e) {
        setError(e.message || "Failed to load connected companies.");
      } finally {
        setLoadingCompanies(false);
      }
    };

    loadCompanies();
  }, [navigate]);

  useEffect(() => {
    if (!isEditing || loadingCompanies) {
      return;
    }

    const loadInvoice = async () => {
      setLoadingInvoice(true);
      try {
        const res = await authorizedFetch(`/invoice/${encodeURIComponent(id)}`, { method: "GET" });
        if (res.status === 401) {
          navigate("/login");
          return;
        }

        const data = await res.json().catch(() => ({}));
        if (!res.ok) {
          throw new Error(data.message || "Failed to load invoice.");
        }

        const lineItems = Array.isArray(data.lineItems)
          ? data.lineItems.map((lineItem) => ({
              itemRef: lineItem.itemRef || "",
              itemName: lineItem.itemName || "",
              description: lineItem.description || "",
              quantity: String(lineItem.quantity ?? 1),
              unitPrice: String(lineItem.unitPrice ?? 0),
            }))
          : [{ ...emptyLineItem }];

        setForm({
          realmId: data.realmId || "",
          customerRef: data.customerRef || "",
          accountRef: "",
          invoiceDate: (data.invoiceDate || "").slice(0, 10),
          dueDate: (data.dueDate || "").slice(0, 10),
          memo: data.memo || "",
          lineItems: lineItems.length > 0 ? lineItems : [{ ...emptyLineItem }],
        });
      } catch (e) {
        setError(e.message || "Failed to load invoice.");
      } finally {
        setLoadingInvoice(false);
      }
    };

    loadInvoice();
  }, [id, isEditing, loadingCompanies, navigate]);

  useEffect(() => {
    const loadLookups = async () => {
      if (!form.realmId) {
        setCustomers([]);
        setItems([]);
        setAccounts([]);
        return;
      }

      setLoadingLookups(true);
      try {
        const [customersRes, itemsRes, accountsRes] = await Promise.all([
          authorizedFetch(`/customer?realmId=${encodeURIComponent(form.realmId)}`, { method: "GET" }),
          authorizedFetch(`/item?realmId=${encodeURIComponent(form.realmId)}`, { method: "GET" }),
          authorizedFetch(`/account?realmId=${encodeURIComponent(form.realmId)}`, { method: "GET" }),
        ]);

        for (const response of [customersRes, itemsRes, accountsRes]) {
          if (response.status === 401) {
            navigate("/login");
            return;
          }
        }

        const customersData = await customersRes.json().catch(() => ({}));
        const itemsData = await itemsRes.json().catch(() => ({}));
        const accountsData = await accountsRes.json().catch(() => ({}));

        if (!customersRes.ok) {
          throw new Error(customersData.message || "Failed to load customers.");
        }

        if (!itemsRes.ok) {
          throw new Error(itemsData.message || "Failed to load items.");
        }

        if (!accountsRes.ok) {
          throw new Error(accountsData.message || "Failed to load accounts.");
        }

        setCustomers(Array.isArray(customersData) ? customersData : []);
        setItems(Array.isArray(itemsData) ? itemsData : []);
        setAccounts(Array.isArray(accountsData) ? accountsData : []);
      } catch (e) {
        setError(e.message || "Failed to load invoice dropdown data.");
      } finally {
        setLoadingLookups(false);
      }
    };

    loadLookups();
  }, [form.realmId, navigate]);

  const updateField = (field, value) => {
    setForm((current) => ({ ...current, [field]: value }));
  };

  const handleCompanyChange = (value) => {
    setError("");
    setForm((current) => ({
      ...current,
      realmId: value,
      customerRef: "",
      accountRef: "",
      lineItems: [{ ...emptyLineItem }],
    }));
  };

  const handleLineItemChange = (index, field, value) => {
    setForm((current) => {
      const nextLineItems = [...current.lineItems];
      const nextItem = { ...nextLineItems[index], [field]: value };

      if (field === "itemRef") {
        const selectedItem = items.find((item) => (item.id || item.Id) === value);
        nextItem.itemName = selectedItem ? selectedItem.name || selectedItem.Name || "" : "";
      }

      nextLineItems[index] = nextItem;
      return { ...current, lineItems: nextLineItems };
    });
  };

  const addLineItem = () => {
    setForm((current) => ({
      ...current,
      lineItems: [...current.lineItems, { ...emptyLineItem }],
    }));
  };

  const removeLineItem = (index) => {
    setForm((current) => {
      if (current.lineItems.length === 1) {
        return current;
      }

      return {
        ...current,
        lineItems: current.lineItems.filter((_, lineIndex) => lineIndex !== index),
      };
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");

    if (!form.realmId) {
      setError("Please choose a connected company first.");
      return;
    }

    if (!form.customerRef) {
      setError("Please select a customer.");
      return;
    }

    if (form.dueDate && form.invoiceDate && form.dueDate < form.invoiceDate) {
      setError("Due date must be on or after the invoice date.");
      return;
    }

    if (!isEditing && !form.accountRef) {
      setError("Please select an accounts receivable account.");
      return;
    }

    const lineItems = form.lineItems.map((lineItem) => ({
      itemRef: lineItem.itemRef,
      itemName: lineItem.itemName,
      description: lineItem.description.trim() || null,
      quantity: Number(lineItem.quantity),
      unitPrice: Number(lineItem.unitPrice),
    }));

    if (
      lineItems.length === 0 ||
      lineItems.some(
        (lineItem) =>
          !lineItem.itemRef ||
          !lineItem.itemName ||
          Number.isNaN(lineItem.quantity) ||
          lineItem.quantity <= 0 ||
          Number.isNaN(lineItem.unitPrice) ||
          lineItem.unitPrice < 0
      )
    ) {
      setError("Each line item must have an item, quantity greater than 0, and a valid unit price.");
      return;
    }

    const selectedCustomer = customers.find((customer) => (customer.id || customer.Id) === form.customerRef);
    if (!selectedCustomer) {
      setError("Selected customer was not found.");
      return;
    }

    setSubmitting(true);
    try {
      const payload = {
        ...(isEditing ? {} : { realmId: form.realmId, accountRef: form.accountRef }),
        customerRef: form.customerRef,
        customerName: selectedCustomer.displayName || selectedCustomer.DisplayName || "",
        invoiceDate: form.invoiceDate,
        dueDate: form.dueDate || null,
        memo: form.memo.trim() || null,
        lineItems,
      };

      const endpoint = isEditing ? `/invoice/${encodeURIComponent(id)}` : "/invoice";
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
        throw new Error(data.message || `Failed to ${isEditing ? "update" : "create"} invoice.`);
      }

      navigate("/invoices");
    } catch (e) {
      setError(e.message || `Failed to ${isEditing ? "update" : "create"} invoice.`);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <>
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;700&family=Instrument+Serif:ital@0;1&display=swap');
        .invoice-page{font-family:'DM Sans',sans-serif;color:#1f1a17}
        .invoice-shell{max-width:1280px;margin:0 auto}
        .invoice-topbar,.invoice-card{background:rgba(255,255,255,.88);border:1px solid rgba(31,26,23,.06);border-radius:24px;box-shadow:0 18px 34px rgba(31,26,23,.08)}
        .invoice-topbar{display:flex;justify-content:space-between;align-items:center;gap:18px;padding:24px}
        .invoice-title{font-family:'Instrument Serif',serif;font-size:34px;line-height:1;margin:0 0 8px;font-weight:400}
        .invoice-subtitle{margin:0;color:#6f6761;font-size:15px}
        .invoice-card{margin-top:22px;padding:20px}
        .invoice-form{display:grid;gap:18px}
        .invoice-form label{display:block;margin-bottom:8px;font-size:13px;font-weight:700;color:#756d67;text-transform:uppercase;letter-spacing:.05em}
        .invoice-input,.invoice-select,.invoice-textarea{width:100%;border-radius:14px;border:1px solid rgba(31,26,23,.12);background:#fff;padding:13px 14px;font-size:15px;color:#1f1a17;font-family:inherit}
        .invoice-textarea{min-height:110px;resize:vertical}
        .btn-solid,.btn-ghost,.btn-danger{border-radius:14px;padding:12px 18px;font-weight:700;font-size:14px}
        .btn-solid{border:none;background:#2c6bed;color:#fff;box-shadow:0 14px 28px rgba(44,107,237,.18)}
        .btn-ghost{border:1px solid rgba(31,26,23,.12);background:#fff;color:#1f1a17}
        .btn-danger{border:none;background:#bf3d35;color:#fff}
        .btn-solid:disabled,.btn-ghost:disabled,.btn-danger:disabled{opacity:.7;cursor:not-allowed}
        .message{border-radius:16px;padding:14px 16px;margin-top:18px;font-weight:500}
        .message.error{background:#fff1f0;color:#b23a33;border:1px solid #f2cac7}
        .form-row{display:grid;gap:16px;grid-template-columns:repeat(2,minmax(0,1fr))}
        .line-items{display:grid;gap:14px}
        .line-item-card{border:1px solid rgba(31,26,23,.08);border-radius:20px;padding:18px;background:#fcfbf8}
        .line-item-grid{display:grid;gap:14px;grid-template-columns:2fr 1.3fr .8fr .9fr .8fr;align-items:end}
        .line-item-actions,.invoice-actions{display:flex;justify-content:space-between;align-items:center;gap:14px}
        .line-item-total{font-weight:700;color:#21653a}
        .empty-state{border-radius:20px;padding:20px;background:#f8f5ef;border:1px dashed rgba(31,26,23,.14);color:#665f58;line-height:1.6}
        .total-box{margin-left:auto;min-width:220px;border-radius:18px;background:#f5fbf7;border:1px solid #d8ecdf;padding:18px 20px}
        .total-box span{display:block;color:#6e7a71;font-size:13px;margin-bottom:8px;text-transform:uppercase;letter-spacing:.05em}
        .total-box strong{font-size:30px;line-height:1;color:#21653a}
        @media (max-width:960px){.invoice-topbar,.form-row,.line-item-grid,.invoice-actions,.line-item-actions{flex-direction:column;grid-template-columns:1fr;align-items:stretch}.btn-solid,.btn-ghost,.btn-danger{width:100%}}
      `}</style>

      <AppShell activeKey="invoice">
      <div className="invoice-page">
        <div className="invoice-shell">
          <section className="invoice-topbar">
            <div>
              <h1 className="invoice-title">{isEditing ? "Edit Invoice" : "Create Invoice"}</h1>
              <p className="invoice-subtitle">
                {isEditing
                  ? "Update the QuickBooks invoice and SQL record together."
                  : "Choose a connected company first, then continue with invoice details."}
              </p>
            </div>
            <button type="button" className="btn-ghost" onClick={() => navigate("/invoices")}>
              Back to Invoice List
            </button>
          </section>

          {error ? <div className="message error">{error}</div> : null}

          <section className="invoice-card">
            {loadingCompanies || loadingInvoice ? (
              <div className="empty-state">Loading invoice details...</div>
            ) : activeCompanies.length === 0 ? (
              <div className="empty-state">No connected companies are available. Connect QuickBooks first, then return here.</div>
            ) : (
              <form className="invoice-form" onSubmit={handleSubmit}>
                <div>
                  <label htmlFor="company">Connected Company</label>
                  <select
                    id="company"
                    className="invoice-select"
                    value={form.realmId}
                    onChange={(e) => handleCompanyChange(e.target.value)}
                    disabled={isEditing}
                  >
                    {!isEditing ? <option value="">Choose company before continuing</option> : null}
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

                {!form.realmId ? (
                  <div className="empty-state">Choose a company first. Customer, item, and account data will load for that specific company.</div>
                ) : loadingLookups ? (
                  <div className="empty-state">Loading customers, items, and accounts...</div>
                ) : (
                  <>
                    <div className="form-row">
                      <div>
                        <label htmlFor="customer">Customer</label>
                        <select id="customer" className="invoice-select" value={form.customerRef} onChange={(e) => updateField("customerRef", e.target.value)}>
                          <option value="">Select customer</option>
                          {customers.map((customer) => (
                            <option key={customer.id || customer.Id} value={customer.id || customer.Id}>
                              {customer.displayName || customer.DisplayName}
                            </option>
                          ))}
                        </select>
                      </div>

                      {!isEditing ? (
                        <div>
                          <label htmlFor="account">Accounts Receivable Account</label>
                          <select id="account" className="invoice-select" value={form.accountRef} onChange={(e) => updateField("accountRef", e.target.value)}>
                            <option value="">Select A/R account</option>
                            {arAccounts.map((account) => (
                              <option key={account.id || account.Id} value={account.id || account.Id}>
                                {account.name || account.Name}
                              </option>
                            ))}
                          </select>
                        </div>
                      ) : (
                        <div className="empty-state" style={{ padding: 16 }}>Company stays locked in edit mode, and the existing QuickBooks account context is preserved.</div>
                      )}
                    </div>

                    <div className="form-row">
                      <div>
                        <label htmlFor="invoiceDate">Invoice Date</label>
                        <input id="invoiceDate" type="date" className="invoice-input" value={form.invoiceDate} onChange={(e) => updateField("invoiceDate", e.target.value)} />
                      </div>
                      <div>
                        <label htmlFor="dueDate">Due Date</label>
                        <input id="dueDate" type="date" min={form.invoiceDate || undefined} className="invoice-input" value={form.dueDate} onChange={(e) => updateField("dueDate", e.target.value)} />
                      </div>
                    </div>

                    <div>
                      <label htmlFor="memo">Memo</label>
                      <textarea id="memo" className="invoice-textarea" value={form.memo} onChange={(e) => updateField("memo", e.target.value)} placeholder="Optional note for this invoice" />
                    </div>

                    <div>
                      <label>Line Items</label>
                      <div className="line-items">
                        {form.lineItems.map((lineItem, index) => {
                          const lineTotal = (Number(lineItem.quantity || 0) * Number(lineItem.unitPrice || 0)).toFixed(2);
                          return (
                            <div key={`line-item-${index}`} className="line-item-card">
                              <div className="line-item-grid">
                                <div>
                                  <label htmlFor={`item-${index}`}>Item</label>
                                  <select id={`item-${index}`} className="invoice-select" value={lineItem.itemRef} onChange={(e) => handleLineItemChange(index, "itemRef", e.target.value)}>
                                    <option value="">Select item</option>
                                    {items.map((item) => (
                                      <option key={item.id || item.Id} value={item.id || item.Id}>
                                        {item.name || item.Name}
                                      </option>
                                    ))}
                                  </select>
                                </div>
                                <div>
                                  <label htmlFor={`description-${index}`}>Description</label>
                                  <input id={`description-${index}`} className="invoice-input" value={lineItem.description} onChange={(e) => handleLineItemChange(index, "description", e.target.value)} placeholder="Optional" />
                                </div>
                                <div>
                                  <label htmlFor={`quantity-${index}`}>Quantity</label>
                                  <input id={`quantity-${index}`} type="number" min="0.01" step="0.01" className="invoice-input" value={lineItem.quantity} onChange={(e) => handleLineItemChange(index, "quantity", e.target.value)} />
                                </div>
                                <div>
                                  <label htmlFor={`unitPrice-${index}`}>Unit Price</label>
                                  <input id={`unitPrice-${index}`} type="number" min="0" step="0.01" className="invoice-input" value={lineItem.unitPrice} onChange={(e) => handleLineItemChange(index, "unitPrice", e.target.value)} />
                                </div>
                                <div>
                                  <label>Amount</label>
                                  <div className="invoice-input" style={{ background: "#f8f5ef" }}>${lineTotal}</div>
                                </div>
                              </div>
                              <div className="line-item-actions" style={{ marginTop: 14 }}>
                                <div className="line-item-total">Line Total: ${lineTotal}</div>
                                <button type="button" className="btn-danger" onClick={() => removeLineItem(index)} disabled={form.lineItems.length === 1}>
                                  Remove Line
                                </button>
                              </div>
                            </div>
                          );
                        })}
                      </div>
                      <div style={{ marginTop: 16 }}>
                        <button type="button" className="btn-ghost" onClick={addLineItem}>Add Another Line Item</button>
                      </div>
                    </div>

                    <div className="total-box">
                      <span>Total Amount</span>
                      <strong>${totalAmount.toFixed(2)}</strong>
                    </div>

                    <div className="invoice-actions">
                      <button type="button" className="btn-ghost" onClick={() => navigate("/invoices")} disabled={submitting}>Cancel</button>
                      <button type="submit" className="btn-solid" disabled={submitting}>
                        {submitting ? (isEditing ? "Updating..." : "Creating...") : isEditing ? "Update Invoice" : "Create Invoice"}
                      </button>
                    </div>
                  </>
                )}
              </form>
            )}
          </section>
        </div>
      </div>
      </AppShell>
    </>
  );
};

export default Invoice;
