import { Form, Input, InputNumber, Select, Button, Card, Table, Typography, message, Spin, Empty, Tag } from 'antd'
import { PlusOutlined, ShoppingOutlined } from '@ant-design/icons'
import { useEffect, useState } from 'react'
import api from '../../shared/api/axiosClient'
import PageHeader from '../../shared/components/PageHeader'
import { parseApiError } from '../../shared/utils/errorUtils'

const { Option } = Select
const { Text } = Typography

const ItemForm = () => {
    const [form] = Form.useForm()
    const [items, setItems] = useState([])
    const [accounts, setAccounts] = useState([])
    const [loading, setLoading] = useState(false)
    const [submitLoading, setSubmitLoading] = useState(false)
    const [showForm, setShowForm] = useState(false)

    useEffect(() => {
        fetchItems()
        fetchAccounts()
    }, [])

    const fetchItems = async () => {
        setLoading(true)
        try {
            const response = await api.get('/item')
            setItems(response.data)
        } catch (error) {
            message.error(parseApiError(error, 'Failed to load items.'))
        } finally {
            setLoading(false)
        }
    }

    const fetchAccounts = async () => {
        try {
            const response = await api.get('/account')
            setAccounts(response.data.filter((a) => a.accountType === 'Income'))
        } catch (error) {
            message.error(parseApiError(error, 'Failed to load income accounts.'))
        }
    }

    const handleSubmit = async (values) => {
        setSubmitLoading(true)
        try {
            const response = await api.post('/item', values)
            setItems((prev) => [response.data, ...prev])
            form.resetFields()
            setShowForm(false)
            message.success('Item created successfully in QuickBooks!')
        } catch (error) {
            message.error(parseApiError(error, 'Failed to create item.'))
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
            dataIndex: 'type',
            key: 'type',
            render: (type) => <Tag color="cyan">{type}</Tag>,
        },
        {
            title: 'Description',
            dataIndex: 'description',
            key: 'description',
            render: (val) => val || <Text type="secondary">-</Text>,
        },
        {
            title: 'Unit Price',
            dataIndex: 'unitPrice',
            key: 'unitPrice',
            render: (val) => <Text strong style={{ color: '#0f766e' }}>${val?.toFixed(2)}</Text>,
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
                title="Items"
                subtitle="Manage your QuickBooks products and services."
                extra={
                    <Button
                        type="primary"
                        icon={<PlusOutlined />}
                        onClick={() => setShowForm(!showForm)}
                    >
                        {showForm ? 'Cancel' : 'New Item'}
                    </Button>
                }
            />

            {showForm && (
                <Card className="form-card" style={{ marginBottom: 24 }}>
                    <Text strong style={{ fontSize: 16, display: 'block', marginBottom: 20 }}>
                        Create Item
                    </Text>
                    <Form form={form} layout="vertical" onFinish={handleSubmit} requiredMark={false}>
                        <Form.Item
                            name="name"
                            label="Item Name"
                            rules={[
                                { required: true, message: 'Item name is required' },
                                { max: 100, message: 'Cannot exceed 100 characters' },
                            ]}
                        >
                            <Input placeholder="e.g., Consulting Service" size="large" />
                        </Form.Item>

                        <Form.Item
                            name="type"
                            label="Item Type"
                            initialValue="Service"
                            rules={[{ required: true, message: 'Item type is required' }]}
                        >
                            <Select size="large">
                                <Option value="Service">Service</Option>
                                <Option value="NonInventory">Non-Inventory</Option>
                                <Option value="Inventory">Inventory</Option>
                            </Select>

                        </Form.Item>

                        <Form.Item name="description" label="Description">
                            <Input.TextArea rows={3} placeholder="Optional description" />
                        </Form.Item>

                        <Form.Item
                            name="unitPrice"
                            label="Unit Price"
                            rules={[
                                { required: true, message: 'Unit price is required' },
                                { type: 'number', min: 0, message: 'Unit price must be 0 or greater' },
                            ]}
                        >
                            <InputNumber
                                prefix="$"
                                min={0}
                                precision={2}
                                style={{ width: '100%' }}
                                size="large"
                                placeholder="0.00"
                            />
                        </Form.Item>

                        <Form.Item
                            name="incomeAccountRef"
                            label="Income Account"
                            rules={[{ required: true, message: 'Income account is required' }]}
                        >
                            <Select
                                placeholder="Select income account"
                                size="large"
                                showSearch
                                filterOption={(input, option) =>
                                    option?.children?.toLowerCase().includes(input.toLowerCase())
                                }
                            >
                                {accounts.map((acc) => (
                                    <Option key={acc.id} value={acc.id}>{acc.name}</Option>
                                ))}
                            </Select>
                        </Form.Item>

                        <Form.Item>
                            <Button
                                type="primary"
                                htmlType="submit"
                                loading={submitLoading}
                                icon={<ShoppingOutlined />}
                                size="large"
                            >
                                Create Item
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
                ) : items.length === 0 ? (
                    <Empty description="No items found. Create your first item." style={{ padding: '40px 0' }} />
                ) : (
                    <Table
                        dataSource={items}
                        columns={columns}
                        rowKey="id"
                        pagination={{ pageSize: 10, showSizeChanger: true }}
                    />
                )}
            </Card>
        </div>
    )
}

export default ItemForm

