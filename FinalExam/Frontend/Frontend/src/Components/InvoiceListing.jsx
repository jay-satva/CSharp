import React, { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { authorizedFetch } from "../auth/api";

const InvoiceListing = () => {
  const navigate = useNavigate();
  const [invoices, setInvoices] = useState([]);
  const [loading, setLoading] = useState(true);
  const [deletingId, setDeletingId] = useState(null);
  const [search, setSearch] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const filteredInvoices = useMemo(() => {
    const query = search.trim().toLowerCase();
    if (!query) {
      return invoices;
    }

    return invoices.filter((invoice) => {
      const customerName = (invoice.customerName || "").toLowerCase();
      const invoiceNumber = (invoice.quickBooksInvoiceId || "").toLowerCase();
      const companyName = (invoice.companyName || "").toLowerCase();
      return customerName.includes(query) || invoiceNumber.includes(query) || companyName.includes(query);
    });
  }, [invoices, search]);

  const loadInvoices = async () => {
    setLoading(true);
    setError("");

    try {
      const res = await authorizedFetch("/invoice", { method: "GET" });
      if (res.status === 401) {
        navigate("/login");
        return;
      }

      const data = await res.json().catch(() => ({}));
      if (!res.ok) {
        throw new Error(data.message || "Failed to load invoices.");
      }

      setInvoices(Array.isArray(data) ? data : []);
    } catch (e) {
      setError(e.message || "Failed to load invoices.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadInvoices();
  }, [navigate]);

  const handleDelete = async (invoice) => {
    const invoiceId = invoice.id;
    const invoiceNumber = invoice.quickBooksInvoiceId || invoiceId;
    const confirmed = window.confirm(`Delete invoice ${invoiceNumber} from QuickBooks and SQL?`);
    if (!confirmed) {
      return;
    }

    setDeletingId(invoiceId);
    setError("");
    setSuccess("");

    try {
      const res = await authorizedFetch(`/invoice/${encodeURIComponent(invoiceId)}`, {
        method: "DELETE",
      });

      if (res.status === 401) {
        navigate("/login");
        return;
      }

      const data = await res.json().catch(() => ({}));
      if (!res.ok) {
        throw new Error(data.message || "Failed to delete invoice.");
      }

      setSuccess(data.message || "Invoice deleted successfully.");
      await loadInvoices();
    } catch (e) {
      setError(e.message || "Failed to delete invoice.");
    } finally {
      setDeletingId(null);
    }
  };

  return (
    <>
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;700&family=Instrument+Serif:ital@0;1&display=swap');
        .invoice-list-page{min-height:100vh;background:radial-gradient(circle at top left,rgba(44,107,237,.08),transparent 28%),linear-gradient(180deg,#f7f3eb 0%,#f2eee7 100%);font-family:'DM Sans',sans-serif;color:#1f1a17;padding:28px}
        .invoice-list-shell{max-width:1280px;margin:0 auto}
        .invoice-list-topbar,.invoice-list-card{background:rgba(255,255,255,.88);border:1px solid rgba(31,26,23,.06);border-radius:24px;box-shadow:0 18px 34px rgba(31,26,23,.08)}
        .invoice-list-topbar{display:flex;justify-content:space-between;align-items:center;gap:18px;padding:24px}
        .invoice-list-title{font-family:'Instrument Serif',serif;font-size:34px;line-height:1;margin:0 0 8px;font-weight:400}
        .invoice-list-subtitle{margin:0;color:#6f6761;font-size:15px}
        .invoice-list-card{margin-top:22px;padding:24px}
        .btn-solid,.btn-ghost,.btn-danger{border-radius:14px;padding:12px 18px;font-weight:700;font-size:14px}
        .btn-solid{border:none;background:#2c6bed;color:#fff;box-shadow:0 14px 28px rgba(44,107,237,.18)}
        .btn-ghost{border:1px solid rgba(31,26,23,.12);background:#fff;color:#1f1a17}
        .btn-danger{border:none;background:#bf3d35;color:#fff}
        .btn-solid:disabled,.btn-ghost:disabled,.btn-danger:disabled{opacity:.7;cursor:not-allowed}
        .invoice-list-actions,.row-actions,.invoice-toolbar{display:flex;gap:12px;align-items:center}
        .invoice-toolbar{justify-content:space-between;margin-bottom:18px}
        .invoice-search{width:min(420px,100%);border-radius:14px;border:1px solid rgba(31,26,23,.12);background:#fff;padding:13px 14px;font-size:15px;color:#1f1a17}
        .message{border-radius:16px;padding:14px 16px;margin-top:18px;font-weight:500}
        .message.success{background:#edf8ef;color:#21653a;border:1px solid #cfe8d4}
        .message.error{background:#fff1f0;color:#b23a33;border:1px solid #f2cac7}
        .invoice-table-wrap{overflow-x:auto}
        .invoice-table{width:100%;border-collapse:collapse}
        .invoice-table th,.invoice-table td{text-align:left;padding:14px 10px;border-bottom:1px solid rgba(31,26,23,.08);vertical-align:middle}
        .invoice-table th{color:#7a716a;font-size:12px;text-transform:uppercase;letter-spacing:.05em}
        .invoice-number{font-weight:700;color:#1f1a17}
        .invoice-meta{font-size:13px;color:#766e67;margin-top:3px}
        .status-pill{display:inline-flex;align-items:center;border-radius:999px;padding:7px 12px;font-size:12px;font-weight:700;background:#edf8ef;color:#21653a}
        .empty-state{border-radius:20px;padding:24px;background:#f8f5ef;border:1px dashed rgba(31,26,23,.14);color:#665f58;line-height:1.6}
        @media (max-width:900px){.invoice-list-topbar,.invoice-toolbar,.invoice-list-actions,.row-actions{flex-direction:column;align-items:stretch}.btn-solid,.btn-ghost,.btn-danger{width:100%}}
      `}</style>

      <div className="invoice-list-page">
        <div className="invoice-list-shell">
          <section className="invoice-list-topbar">
            <div>
              <h1 className="invoice-list-title">Invoices</h1>
              <p className="invoice-list-subtitle">Manage SQL-saved invoices and keep QuickBooks in sync.</p>
            </div>

            <div className="invoice-list-actions">
              <button type="button" className="btn-ghost" onClick={() => navigate("/dashboard")}>
                Back to Dashboard
              </button>
              <button type="button" className="btn-solid" onClick={() => navigate("/invoice/new")}>
                Create Invoice
              </button>
            </div>
          </section>

          {success ? <div className="message success">{success}</div> : null}
          {error ? <div className="message error">{error}</div> : null}

          <section className="invoice-list-card">
            <div className="invoice-toolbar">
              <input
                className="invoice-search"
                placeholder="Search by customer, company, or QuickBooks invoice id"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
            </div>

            {loading ? (
              <div className="empty-state">Loading invoices...</div>
            ) : filteredInvoices.length === 0 ? (
              <div className="empty-state">No invoices found yet. Create your first invoice to save it in QuickBooks and SQL.</div>
            ) : (
              <div className="invoice-table-wrap">
                <table className="invoice-table">
                  <thead>
                    <tr>
                      <th>Invoice</th>
                      <th>Customer</th>
                      <th>Company</th>
                      <th>Invoice Date</th>
                      <th>Due Date</th>
                      <th>Amount</th>
                      <th>Status</th>
                      <th>Action</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredInvoices.map((invoice) => (
                      <tr key={invoice.id}>
                        <td>
                          <div className="invoice-number">#{invoice.quickBooksInvoiceId}</div>
                          <div className="invoice-meta">Local Id: {invoice.id}</div>
                        </td>
                        <td>{invoice.customerName}</td>
                        <td>{invoice.companyName}</td>
                        <td>{invoice.invoiceDate ? new Date(invoice.invoiceDate).toLocaleDateString() : "-"}</td>
                        <td>{invoice.dueDate ? new Date(invoice.dueDate).toLocaleDateString() : "-"}</td>
                        <td>${Number(invoice.totalAmount || 0).toFixed(2)}</td>
                        <td><span className="status-pill">{invoice.status || "Draft"}</span></td>
                        <td>
                          <div className="row-actions">
                            <button type="button" className="btn-ghost" onClick={() => navigate(`/invoice/edit/${invoice.id}`)}>
                              Edit
                            </button>
                            <button
                              type="button"
                              className="btn-danger"
                              onClick={() => handleDelete(invoice)}
                              disabled={deletingId === invoice.id}
                            >
                              {deletingId === invoice.id ? "Deleting..." : "Delete"}
                            </button>
                          </div>
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
    </>
  );
};

export default InvoiceListing;
