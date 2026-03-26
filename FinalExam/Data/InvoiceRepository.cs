using FinalExam.Models;
using Microsoft.Data.SqlClient;

namespace FinalExam.Data;

public class InvoiceRepository
{
    private readonly string _connectionString;

    public InvoiceRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");
    }

    public async Task<List<InvoiceRecord>> GetAllByUserIdAsync(string userId)
    {
        var invoices = new Dictionary<int, InvoiceRecord>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
SELECT
    i.Id,
    i.UserId,
    i.RealmId,
    i.QuickBooksInvoiceId,
    i.CustomerRef,
    i.CustomerName,
    i.InvoiceDate,
    i.DueDate,
    i.Memo,
    i.TotalAmount,
    i.Status,
    i.CreatedAt,
    i.UpdatedAt,
    li.Id AS LineItemId,
    li.InvoiceId AS LineInvoiceId,
    li.ItemRef,
    li.ItemName,
    li.Description,
    li.Quantity,
    li.UnitPrice,
    li.Amount
FROM Invoices i
LEFT JOIN InvoiceLineItems li ON li.InvoiceId = i.Id
WHERE i.UserId = @UserId
ORDER BY i.CreatedAt DESC, li.Id ASC;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var invoiceId = reader.GetInt32(reader.GetOrdinal("Id"));
            if (!invoices.TryGetValue(invoiceId, out var invoice))
            {
                invoice = new InvoiceRecord
                {
                    Id = invoiceId,
                    UserId = reader.GetString(reader.GetOrdinal("UserId")),
                    RealmId = reader.GetString(reader.GetOrdinal("RealmId")),
                    QuickBooksInvoiceId = reader.GetString(reader.GetOrdinal("QuickBooksInvoiceId")),
                    CustomerRef = reader.GetString(reader.GetOrdinal("CustomerRef")),
                    CustomerName = reader.GetString(reader.GetOrdinal("CustomerName")),
                    InvoiceDate = reader.GetDateTime(reader.GetOrdinal("InvoiceDate")),
                    DueDate = reader.IsDBNull(reader.GetOrdinal("DueDate")) ? null : reader.GetDateTime(reader.GetOrdinal("DueDate")),
                    Memo = reader.IsDBNull(reader.GetOrdinal("Memo")) ? null : reader.GetString(reader.GetOrdinal("Memo")),
                    TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                };
                invoices[invoiceId] = invoice;
            }

            if (!reader.IsDBNull(reader.GetOrdinal("LineItemId")))
            {
                invoice.LineItems.Add(new InvoiceLineItemRecord
                {
                    Id = reader.GetInt32(reader.GetOrdinal("LineItemId")),
                    InvoiceId = reader.GetInt32(reader.GetOrdinal("LineInvoiceId")),
                    ItemRef = reader.GetString(reader.GetOrdinal("ItemRef")),
                    ItemName = reader.GetString(reader.GetOrdinal("ItemName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    Quantity = reader.GetDecimal(reader.GetOrdinal("Quantity")),
                    UnitPrice = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                    Amount = reader.GetDecimal(reader.GetOrdinal("Amount"))
                });
            }
        }

        return invoices.Values.ToList();
    }

    public async Task<InvoiceRecord?> GetByIdAsync(int id, string userId)
    {
        return (await GetByIdsInternalAsync(userId, id)).FirstOrDefault();
    }

    public async Task<InvoiceRecord> CreateAsync(InvoiceRecord invoice)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            const string insertInvoiceSql = @"
INSERT INTO Invoices
(
    UserId,
    RealmId,
    QuickBooksInvoiceId,
    CustomerRef,
    CustomerName,
    InvoiceDate,
    DueDate,
    Memo,
    TotalAmount,
    Status,
    CreatedAt,
    UpdatedAt
)
OUTPUT INSERTED.Id
VALUES
(
    @UserId,
    @RealmId,
    @QuickBooksInvoiceId,
    @CustomerRef,
    @CustomerName,
    @InvoiceDate,
    @DueDate,
    @Memo,
    @TotalAmount,
    @Status,
    @CreatedAt,
    @UpdatedAt
);";

            await using (var command = new SqlCommand(insertInvoiceSql, connection, (SqlTransaction)transaction))
            {
                AddInvoiceParameters(command, invoice);
                var insertedId = await command.ExecuteScalarAsync();
                invoice.Id = Convert.ToInt32(insertedId);
            }

            foreach (var lineItem in invoice.LineItems)
            {
                lineItem.InvoiceId = invoice.Id;
                await InsertLineItemAsync(connection, (SqlTransaction)transaction, lineItem);
            }

            await transaction.CommitAsync();
            return invoice;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateAsync(InvoiceRecord invoice)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            const string updateInvoiceSql = @"
UPDATE Invoices
SET
    CustomerRef = @CustomerRef,
    CustomerName = @CustomerName,
    InvoiceDate = @InvoiceDate,
    DueDate = @DueDate,
    Memo = @Memo,
    TotalAmount = @TotalAmount,
    Status = @Status,
    UpdatedAt = @UpdatedAt
WHERE Id = @Id AND UserId = @UserId;";

            await using (var command = new SqlCommand(updateInvoiceSql, connection, (SqlTransaction)transaction))
            {
                command.Parameters.AddWithValue("@Id", invoice.Id);
                command.Parameters.AddWithValue("@UserId", invoice.UserId);
                command.Parameters.AddWithValue("@CustomerRef", invoice.CustomerRef);
                command.Parameters.AddWithValue("@CustomerName", invoice.CustomerName);
                command.Parameters.AddWithValue("@InvoiceDate", invoice.InvoiceDate);
                command.Parameters.AddWithValue("@DueDate", (object?)invoice.DueDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@Memo", (object?)invoice.Memo ?? DBNull.Value);
                command.Parameters.AddWithValue("@TotalAmount", invoice.TotalAmount);
                command.Parameters.AddWithValue("@Status", invoice.Status);
                command.Parameters.AddWithValue("@UpdatedAt", invoice.UpdatedAt);
                await command.ExecuteNonQueryAsync();
            }

            await using (var deleteLinesCommand = new SqlCommand(
                "DELETE FROM InvoiceLineItems WHERE InvoiceId = @InvoiceId;",
                connection,
                (SqlTransaction)transaction))
            {
                deleteLinesCommand.Parameters.AddWithValue("@InvoiceId", invoice.Id);
                await deleteLinesCommand.ExecuteNonQueryAsync();
            }

            foreach (var lineItem in invoice.LineItems)
            {
                lineItem.InvoiceId = invoice.Id;
                await InsertLineItemAsync(connection, (SqlTransaction)transaction, lineItem);
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteAsync(int id, string userId)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = "DELETE FROM Invoices WHERE Id = @Id AND UserId = @UserId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@UserId", userId);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<List<InvoiceRecord>> GetByIdsInternalAsync(string userId, params int[] ids)
    {
        if (ids.Length == 0)
            return new List<InvoiceRecord>();

        var invoices = new Dictionary<int, InvoiceRecord>();
        var parameterNames = ids.Select((_, index) => $"@Id{index}").ToList();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = $@"
SELECT
    i.Id,
    i.UserId,
    i.RealmId,
    i.QuickBooksInvoiceId,
    i.CustomerRef,
    i.CustomerName,
    i.InvoiceDate,
    i.DueDate,
    i.Memo,
    i.TotalAmount,
    i.Status,
    i.CreatedAt,
    i.UpdatedAt,
    li.Id AS LineItemId,
    li.InvoiceId AS LineInvoiceId,
    li.ItemRef,
    li.ItemName,
    li.Description,
    li.Quantity,
    li.UnitPrice,
    li.Amount
FROM Invoices i
LEFT JOIN InvoiceLineItems li ON li.InvoiceId = i.Id
WHERE i.UserId = @UserId AND i.Id IN ({string.Join(", ", parameterNames)})
ORDER BY i.CreatedAt DESC, li.Id ASC;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        for (var i = 0; i < ids.Length; i++)
            command.Parameters.AddWithValue(parameterNames[i], ids[i]);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var invoiceId = reader.GetInt32(reader.GetOrdinal("Id"));
            if (!invoices.TryGetValue(invoiceId, out var invoice))
            {
                invoice = new InvoiceRecord
                {
                    Id = invoiceId,
                    UserId = reader.GetString(reader.GetOrdinal("UserId")),
                    RealmId = reader.GetString(reader.GetOrdinal("RealmId")),
                    QuickBooksInvoiceId = reader.GetString(reader.GetOrdinal("QuickBooksInvoiceId")),
                    CustomerRef = reader.GetString(reader.GetOrdinal("CustomerRef")),
                    CustomerName = reader.GetString(reader.GetOrdinal("CustomerName")),
                    InvoiceDate = reader.GetDateTime(reader.GetOrdinal("InvoiceDate")),
                    DueDate = reader.IsDBNull(reader.GetOrdinal("DueDate")) ? null : reader.GetDateTime(reader.GetOrdinal("DueDate")),
                    Memo = reader.IsDBNull(reader.GetOrdinal("Memo")) ? null : reader.GetString(reader.GetOrdinal("Memo")),
                    TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                };
                invoices[invoiceId] = invoice;
            }

            if (!reader.IsDBNull(reader.GetOrdinal("LineItemId")))
            {
                invoice.LineItems.Add(new InvoiceLineItemRecord
                {
                    Id = reader.GetInt32(reader.GetOrdinal("LineItemId")),
                    InvoiceId = reader.GetInt32(reader.GetOrdinal("LineInvoiceId")),
                    ItemRef = reader.GetString(reader.GetOrdinal("ItemRef")),
                    ItemName = reader.GetString(reader.GetOrdinal("ItemName")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    Quantity = reader.GetDecimal(reader.GetOrdinal("Quantity")),
                    UnitPrice = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                    Amount = reader.GetDecimal(reader.GetOrdinal("Amount"))
                });
            }
        }

        return invoices.Values.ToList();
    }

    private static void AddInvoiceParameters(SqlCommand command, InvoiceRecord invoice)
    {
        command.Parameters.AddWithValue("@UserId", invoice.UserId);
        command.Parameters.AddWithValue("@RealmId", invoice.RealmId);
        command.Parameters.AddWithValue("@QuickBooksInvoiceId", invoice.QuickBooksInvoiceId);
        command.Parameters.AddWithValue("@CustomerRef", invoice.CustomerRef);
        command.Parameters.AddWithValue("@CustomerName", invoice.CustomerName);
        command.Parameters.AddWithValue("@InvoiceDate", invoice.InvoiceDate);
        command.Parameters.AddWithValue("@DueDate", (object?)invoice.DueDate ?? DBNull.Value);
        command.Parameters.AddWithValue("@Memo", (object?)invoice.Memo ?? DBNull.Value);
        command.Parameters.AddWithValue("@TotalAmount", invoice.TotalAmount);
        command.Parameters.AddWithValue("@Status", invoice.Status);
        command.Parameters.AddWithValue("@CreatedAt", invoice.CreatedAt);
        command.Parameters.AddWithValue("@UpdatedAt", invoice.UpdatedAt);
    }

    private static async Task InsertLineItemAsync(SqlConnection connection, SqlTransaction transaction, InvoiceLineItemRecord lineItem)
    {
        const string insertLineSql = @"
INSERT INTO InvoiceLineItems
(
    InvoiceId,
    ItemRef,
    ItemName,
    Description,
    Quantity,
    UnitPrice,
    Amount
)
VALUES
(
    @InvoiceId,
    @ItemRef,
    @ItemName,
    @Description,
    @Quantity,
    @UnitPrice,
    @Amount
);";

        await using var command = new SqlCommand(insertLineSql, connection, transaction);
        command.Parameters.AddWithValue("@InvoiceId", lineItem.InvoiceId);
        command.Parameters.AddWithValue("@ItemRef", lineItem.ItemRef);
        command.Parameters.AddWithValue("@ItemName", lineItem.ItemName);
        command.Parameters.AddWithValue("@Description", (object?)lineItem.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@Quantity", lineItem.Quantity);
        command.Parameters.AddWithValue("@UnitPrice", lineItem.UnitPrice);
        command.Parameters.AddWithValue("@Amount", lineItem.Amount);
        await command.ExecuteNonQueryAsync();
    }
}
