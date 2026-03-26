import { Form, Input, Select, Button, Card, Table, Typography, message, Spin, Empty, Tag } from 'antd'
import { PlusOutlined, BankOutlined } from '@ant-design/icons'
import { useEffect, useState } from 'react'
import api from '../../shared/api/axiosClient'
import PageHeader from '../../shared/components/PageHeader'
import { parseApiError } from '../../shared/utils/errorUtils'

const { Option } = Select
const { Text } = Typography

const accountTypes = [
  'Bank', 'Accounts Receivable', 'Other Current Asset', 'Fixed Asset',
  'Other Asset', 'Accounts Payable', 'Credit Card', 'Other Current Liability',
  'Long Term Liability', 'Equity', 'Income', 'Cost of Goods Sold',
  'Expense', 'Other Income', 'Other Expense',
]

const AccountForm = () => {
  const [form] = Form.useForm()
  const [accounts, setAccounts] = useState([])
  const [loading, setLoading] = useState(false)
  const [submitLoading, setSubmitLoading] = useState(false)
  const [showForm, setShowForm] = useState(false)

  useEffect(() => {
    fetchAccounts()
  }, [])

  const fetchAccounts = async () => {
    setLoading(true)
    try {
      const response = await api.get('/account')
      setAccounts(response.data)
    } catch (error) {
      message.error(parseApiError(error, 'Failed to load accounts.'))
    } finally {
      setLoading(false)
    }
  }

  const handleSubmit = async (values) => {
    setSubmitLoading(true)
    try {
      const response = await api.post('/account', values)
      setAccounts((prev) => [response.data, ...prev])
      form.resetFields()
      setShowForm(false)
      message.success('Account created successfully in QuickBooks!')
    } catch (error) {
      message.error(parseApiError(error, 'Failed to create account.'))
    } finally {
      setSubmitLoading(false)
    }
  }

  const columns = [
    {
      title: 'Name',
      dataIndex: 'name',
      key: 'name',
      render: (text) => <Text strong>{text}</Text>,
    },
    {
      title: 'Company',
      dataIndex: 'companyName',
      key: 'companyName',
      render: (val) => <Tag color="geekblue">{val || '-'}</Tag>,
    },
    {
      title: 'Type',
      dataIndex: 'accountType',
      key: 'accountType',
      render: (type) => <Tag color="blue">{type}</Tag>,
    },
    {
      title: 'Sub Type',
      dataIndex: 'accountSubType',
      key: 'accountSubType',
      render: (val) => val || <Text type="secondary">-</Text>,
    },
    {
      title: 'Description',
      dataIndex: 'description',
      key: 'description',
      render: (val) => val || <Text type="secondary">-</Text>,
    },
    {
      title: 'Status',
      dataIndex: 'active',
      key: 'active',
      render: (active) => (
        <Tag color={active ? 'green' : 'red'}>{active ? 'Active' : 'Inactive'}</Tag>
      ),
    },
  ]

  return (
    <div>
      <PageHeader
        title="Accounts"
        subtitle="Manage your QuickBooks chart of accounts."
        extra={
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => setShowForm(!showForm)}
          >
            {showForm ? 'Cancel' : 'New Account'}
          </Button>
        }
      />

      {showForm && (
        <Card className="form-card" style={{ marginBottom: 24 }}>
          <Text strong style={{ fontSize: 16, display: 'block', marginBottom: 20 }}>
            Create Account
          </Text>
          <Form form={form} layout="vertical" onFinish={handleSubmit} requiredMark={false}>
            <Form.Item
              name="name"
              label="Account Name"
              rules={[
                { required: true, message: 'Account name is required' },
                { max: 100, message: 'Cannot exceed 100 characters' },
              ]}
            >
              <Input placeholder="e.g., Checking Account" size="large" />
            </Form.Item>

            <Form.Item
              name="accountType"
              label="Account Type"
              rules={[{ required: true, message: 'Account type is required' }]}
            >
              <Select placeholder="Select account type" size="large" showSearch>
                {accountTypes.map((type) => (
                  <Option key={type} value={type}>{type}</Option>
                ))}
              </Select>
            </Form.Item>

            <Form.Item name="accountSubType" label="Account Sub Type">
              <Input placeholder="Optional sub type" size="large" />
            </Form.Item>

            <Form.Item name="description" label="Description">
              <Input.TextArea rows={3} placeholder="Optional description" />
            </Form.Item>

            <Form.Item>
              <Button
                type="primary"
                htmlType="submit"
                loading={submitLoading}
                icon={<BankOutlined />}
                size="large"
              >
                Create Account
              </Button>
            </Form.Item>
          </Form>
        </Card>
      )}

      <Card className="invoice-table-card">
        {loading ? (
          <div style={{ textAlign: 'center', padding: 60 }}>
            <Spin size="large" />
          </div>
        ) : accounts.length === 0 ? (
          <Empty description="No accounts found. Create your first account." style={{ padding: '40px 0' }} />
        ) : (
          <Table
            dataSource={accounts}
            columns={columns}
            rowKey="id"
            pagination={{ pageSize: 10, showSizeChanger: true }}
          />
        )}
      </Card>
    </div>
  )
}

export default AccountForm

