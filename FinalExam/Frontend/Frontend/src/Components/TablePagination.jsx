import React from "react";

const TablePagination = ({ page, pageSize, totalItems, onPageChange }) => {
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));

  if (totalItems <= pageSize) {
    return null;
  }

  const start = (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, totalItems);

  return (
    <>
      <style>{`
        .table-pagination {
          display: flex;
          justify-content: space-between;
          align-items: center;
          gap: 12px;
          margin-top: 18px;
          flex-wrap: wrap;
        }

        .table-pagination-info {
          color: #6f6761;
          font-size: 14px;
        }

        .table-pagination-actions {
          display: flex;
          gap: 10px;
          flex-wrap: wrap;
        }

        .table-pagination-btn {
          border: 1px solid rgba(31, 26, 23, 0.12);
          border-radius: 12px;
          padding: 10px 14px;
          background: #fff;
          color: #1f1a17;
          font-weight: 700;
          font-size: 13px;
        }

        .table-pagination-btn:disabled {
          opacity: 0.55;
          cursor: not-allowed;
        }
      `}</style>

      <div className="table-pagination">
        <div className="table-pagination-info">
          Showing {start}-{end} of {totalItems}
        </div>
        <div className="table-pagination-actions">
          <button
            type="button"
            className="table-pagination-btn"
            onClick={() => onPageChange(page - 1)}
            disabled={page <= 1}
          >
            Previous
          </button>
          <button
            type="button"
            className="table-pagination-btn"
            onClick={() => onPageChange(page + 1)}
            disabled={page >= totalPages}
          >
            Next
          </button>
        </div>
      </div>
    </>
  );
};

export default TablePagination;
