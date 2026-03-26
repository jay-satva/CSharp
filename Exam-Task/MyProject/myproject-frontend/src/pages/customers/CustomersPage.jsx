import { Form, Input, Button, Card, Table, Typography, message, Spin, Empty, Tag, Avatar } from 'antd'
import { PlusOutlined, UserOutlined } from '@ant-design/icons'
import { useEffect, useState } from 'react'
import api from '../../shared/api/axiosClient'
import PageHeader from '../../shared/components/PageHeader'
import { parseApiError } from '../../shared/utils/errorUtils'

const { Text } = Typography

const CustomerForm = () => {
  const [form] = Form.useForm()
  const [customers, setCustomers] = useState([])
  const [loading, setLoading] = useState(false)
  const [submitLoading, setSubmitLoading] = useState(false)
  const [showForm, setShowForm] = useState(false)

  useEffect(() => {
    fetchCustomers()
  }, [])

  const fetchCustomers = async () => {
    setLoading(true)
    try {
      const response = await api.get('/customer')
      setCustomers(response.data)
    } catch (error) {
      message.error(parseApiError(error, 'Failed to load customers.'))
    } finally {
      setLoading(false)
    }
  }

  const handleSubmit = async (values) => {
    setSubmitLoading(true)
    try {
      const response = await api.post('/customer', values)
      setCustomers((prev) => [response.data, ...prev])
      form.resetFields()
      setShowForm(false)
      message.success('Customer created successfully in QuickBooks!')
    } catch (error) {
      message.error(parseApiError(error, 'Failed to create customer.'))
    } finally {
      setSubmitLoading(false)
    }
  }

  const columns = [
    {
      title: 'Customer',
      dataIndex: 'displayName',
      key: 'displayName',
      render: (name) => (
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <Avatar style={{ backgroundColor: '#0f766e', flexShrink: 0 }}>
            {name?.charAt(0).toUpperCase()}
          </Avatar>
          <Text strong>{name}</Text>
        </div>
      ),
    },
    {
      title: 'Company',
      dataIndex: 'companyName',
      key: 'companyName',
      render: (val) => <Tag color="geekblue">{val || '-'}</Tag>,
    },
    {
      title: 'Email',
      dataIndex: 'email',
      key: 'email',
      render: (val) => val || <Text type="secondary">-</Text>,
    },
    {
      title: 'Phone',
      dataIndex: 'phone',
      key: 'phone',
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
        title="Customers"
        subtitle="Manage your QuickBooks customers."
        extra={
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => setShowForm(!showForm)}
          >
            {showForm ? 'Cancel' : 'New Customer'}
          </Button>
        }
      />

      {showForm && (
        <Card className="form-card" style={{ marginBottom: 24 }}>
          <Text strong style={{ fontSize: 16, display: 'block', marginBottom: 20 }}>
            Create Customer
          </Text>
          <Form form={form} layout="vertical" onFinish={handleSubmit} requiredMark={false}>
            <Form.Item
              name="displayName"
              label="Display Name"
              rules={[
                { required: true, message: 'Display name is required' },
                { max: 100, message: 'Cannot exceed 100 characters' },
              ]}
            >
              <Input placeholder="e.g., Acme Corp" size="large" />
            </Form.Item>

            <Form.Item name="firstName" label="First Name">
              <Input placeholder="First name" size="large" />
            </Form.Item>

            <Form.Item name="lastName" label="Last Name">
              <Input placeholder="Last name" size="large" />
            </Form.Item>

            <Form.Item
              name="email"
              label="Email"
              rules={[{ type: 'email', message: 'Please enter a valid email' }]}
            >
              <Input placeholder="customer@example.com" size="large" />
            </Form.Item>

            <Form.Item name="phone" label="Phone">
              <Input placeholder="+1 (555) 000-0000" size="large" />
            </Form.Item>

            <Form.Item>
              <Button
                type="primary"
                htmlType="submit"
                loading={submitLoading}
                icon={<UserOutlined />}
                size="large"
              >
                Create Customer
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
        ) : customers.length === 0 ? (
          <Empty description="No customers found. Create your first customer." style={{ padding: '40px 0' }} />
        ) : (
          <Table
            dataSource={customers}
            columns={columns}
            rowKey="id"
            pagination={{ pageSize: 10, showSizeChanger: true }}
          />
        )}
      </Card>
    </div>
  )
}

export default CustomerForm

