import {
  Card,
  Table,
  Button,
  Typography,
  Tag,
  Space,
  Popconfirm,
  message,
  Input,
  Select,
  Row,
  Col,
} from 'antd'
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  SearchOutlined,
} from '@ant-design/icons'
import { useEffect, useState } from 'react'
import { useDispatch, useSelector } from 'react-redux'
import { useNavigate } from 'react-router-dom'
import { fetchInvoices, deleteInvoice } from '../../features/invoice/model/invoiceSlice'
import PageHeader from '../../shared/components/PageHeader'
import { parseApiError } from '../../shared/utils/errorUtils'

const { Text } = Typography
const { Option } = Select

const statusColors = {
  Draft: { color: '#0f766e', bg: '#ccfbf1' },
  Sent: { color: '#b45309', bg: '#fef3c7' },
  Paid: { color: '#15803d', bg: '#dcfce7' },
  Overdue: { color: '#b91c1c', bg: '#fee2e2' },
  Void: { color: '#6B7280', bg: '#F3F4F6' },
}

const InvoiceList = () => {
  const dispatch = useDispatch()
  const navigate = useNavigate()
  const { invoices, loading } = useSelector((state) => state.invoice)
  const [searchText, setSearchText] = useState('')
  const [statusFilter, setStatusFilter] = useState('all')
  const [deletingId, setDeletingId] = useState(null)

  useEffect(() => {
    dispatch(fetchInvoices())
  }, [dispatch])

  const handleDelete = async (id) => {
    setDeletingId(id)
    try {
      await dispatch(deleteInvoice(id)).unwrap()
      message.success('Invoice deleted successfully.')
    } catch (error) {
      message.error(parseApiError(error, 'Failed to delete invoice.'))
    } finally {
      setDeletingId(null)
    }
  }

  const filteredInvoices = invoices.filter((inv) => {
    const matchesSearch =
      inv.customerName.toLowerCase().includes(searchText.toLowerCase()) ||
      inv.quickBooksInvoiceId.toLowerCase().includes(searchText.toLowerCase())
    const matchesStatus = statusFilter === 'all' || inv.status === statusFilter
    return matchesSearch && matchesStatus
  })

  const columns = [
    {
      title: 'Invoice #',
      dataIndex: 'quickBooksInvoiceId',
      key: 'quickBooksInvoiceId',
      render: (val) => (
        <Text strong style={{ color: '#0f766e' }}>
          #{val}
        </Text>
      ),
    },
    {
      title: 'Customer',
      dataIndex: 'customerName',
      key: 'customerName',
      render: (name) => <Text strong>{name}</Text>,
    },
    {
      title: 'Company',
      dataIndex: 'companyName',
      key: 'companyName',
      render: (name) => <Tag color="geekblue">{name || '-'}</Tag>,
    },
    {
      title: 'Invoice Date',
      dataIndex: 'invoiceDate',
      key: 'invoiceDate',
      render: (date) => new Date(date).toLocaleDateString(),
      sorter: (a, b) => new Date(a.invoiceDate) - new Date(b.invoiceDate),
    },
    {
      title: 'Due Date',
      dataIndex: 'dueDate',
      key: 'dueDate',
      render: (date) => {
        const due = new Date(date)
        const isOverdue = due < new Date()
        return (
          <Text style={{ color: isOverdue ? '#b91c1c' : 'inherit' }}>
            {due.toLocaleDateString()}
          </Text>
        )
      },
      sorter: (a, b) => new Date(a.dueDate) - new Date(b.dueDate),
    },
    {
      title: 'Amount',
      dataIndex: 'totalAmount',
      key: 'totalAmount',
      render: (val) => (
        <Text strong style={{ color: '#15803d' }}>
          ${val.toFixed(2)}
        </Text>
      ),
      sorter: (a, b) => a.totalAmount - b.totalAmount,
    },
    {
      title: 'Status',
      dataIndex: 'status',
      key: 'status',
      render: (status) => {
        const colors = statusColors[status] || { color: '#6B7280', bg: '#F3F4F6' }
        return (
          <span
            className="status-badge"
            style={{ background: colors.bg, color: colors.color }}
          >
            {status}
          </span>
        )
      },
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (_, record) => (
        <Space>
          <Button
            type="text"
            icon={<EditOutlined />}
            onClick={() => navigate(`/invoices/edit/${record.id}`)}
            style={{ color: '#0f766e' }}
          >
            Edit
          </Button>
          <Popconfirm
            title="Delete Invoice"
            description="This will delete the invoice from both QuickBooks and the database. Are you sure?"
            onConfirm={() => handleDelete(record.id)}
            okText="Yes, Delete"
            cancelText="Cancel"
            okButtonProps={{ danger: true }}
          >
            <Button
              type="text"
              icon={<DeleteOutlined />}
              danger
              loading={deletingId === record.id}
            >
              Delete
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ]

  return (
    <div>
      <PageHeader
        title="Invoices"
        subtitle="Manage all your QuickBooks invoices."
        extra={
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => navigate('/invoices/new')}
            size="large"
          >
            New Invoice
          </Button>
        }
      />

      <Card className="invoice-table-card">
        <Row gutter={[16, 16]} style={{ marginBottom: 16 }}>
          <Col xs={24} sm={14} md={16}>
            <Input
              prefix={<SearchOutlined style={{ color: '#9CA3AF' }} />}
              placeholder="Search by customer or invoice number..."
              value={searchText}
              onChange={(e) => setSearchText(e.target.value)}
              allowClear
              size="large"
            />
          </Col>
          <Col xs={24} sm={10} md={8}>
            <Select
              value={statusFilter}
              onChange={setStatusFilter}
              style={{ width: '100%' }}
              size="large"
            >
              <Option value="all">All Statuses</Option>
              <Option value="Draft">Draft</Option>
              <Option value="Sent">Sent</Option>
              <Option value="Paid">Paid</Option>
              <Option value="Overdue">Overdue</Option>
              <Option value="Void">Void</Option>
            </Select>
          </Col>
        </Row>

        <Table
          dataSource={filteredInvoices}
          columns={columns}
          rowKey="id"
          loading={loading}
          pagination={{
            pageSize: 10,
            showSizeChanger: true,
            showTotal: (total) => `Total ${total} invoices`,
          }}
          locale={{
            emptyText: (
              <div style={{ padding: '40px 0', textAlign: 'center' }}>
                <Text type="secondary">No invoices found. Create your first invoice.</Text>
              </div>
            ),
          }}
        />
      </Card>
    </div>
  )
}

export default InvoiceList

