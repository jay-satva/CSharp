import {
  Form,
  Input,
  InputNumber,
  Select,
  Button,
  Card,
  Typography,
  message,
  Spin,
  Row,
  Col,
  Divider,
  DatePicker,
  Space,
} from 'antd'
import {
  PlusOutlined,
  DeleteOutlined,
  SaveOutlined,
  ArrowLeftOutlined,
} from '@ant-design/icons'
import { useEffect, useState } from 'react'
import { useDispatch } from 'react-redux'
import { useNavigate, useParams } from 'react-router-dom'
import dayjs from 'dayjs'
import api from '../../shared/api/axiosClient'
import { createInvoice, updateInvoice } from '../../features/invoice/model/invoiceSlice'
import PageHeader from '../../shared/components/PageHeader'
import { parseApiError } from '../../shared/utils/errorUtils'

const { Option } = Select
const { Text } = Typography

const InvoiceForm = () => {
  const [form] = Form.useForm()
  const dispatch = useDispatch()
  const navigate = useNavigate()
  const { id } = useParams()
  const isEdit = Boolean(id)

  const [customers, setCustomers] = useState([])
  const [items, setItems] = useState([])
  const [companies, setCompanies] = useState([])
  const [selectedCompanyId, setSelectedCompanyId] = useState(null)
  const [loading, setLoading] = useState(false)
  const [submitLoading, setSubmitLoading] = useState(false)
  const [lineItems, setLineItems] = useState([
    { itemRef: '', itemName: '', description: '', quantity: 1, unitPrice: 0, amount: 0 },
  ])

  useEffect(() => {
    if (isEdit) {
      fetchInvoice()
    } else {
      fetchCompanies()
    }
  }, [id])

  const fetchCompanies = async () => {
    try {
      const response = await api.get('/quickbooks/companies')
      setCompanies(response.data || [])
    } catch (error) {
      message.error(parseApiError(error, 'Failed to load connected companies.'))
    }
  }

  const fetchDropdownData = async (companyId) => {
    if (!companyId) {
      setCustomers([])
      setItems([])
      return
    }

    try {
      const config = { headers: { 'X-Company-Id': companyId } }
      const [customersRes, itemsRes] = await Promise.all([
        api.get('/customer', config),
        api.get('/item', config),
      ])
      setCustomers(customersRes.data)
      setItems(itemsRes.data)
    } catch (error) {
      message.error(parseApiError(error, 'Failed to load customers and items.'))
    }
  }

  const fetchInvoice = async () => {
    setLoading(true)
    try {
      const response = await api.get(`/invoice/${id}`)
      const invoice = response.data
      setSelectedCompanyId(invoice.companyId)
      await fetchDropdownData(invoice.companyId)
      setCompanies([{ id: invoice.companyId, companyName: invoice.companyName, realmId: invoice.realmId }])
      form.setFieldsValue({
        companyId: invoice.companyId,
        customerRef: invoice.customerRef,
        invoiceDate: dayjs(invoice.invoiceDate),
        dueDate: dayjs(invoice.dueDate),
        memo: invoice.memo,
      })
      setLineItems(
        invoice.lineItems.map((li) => ({
          itemRef: li.itemRef,
          itemName: li.itemName,
          description: li.description || '',
          quantity: li.quantity,
          unitPrice: li.unitPrice,
          amount: li.amount,
        }))
      )
    } catch (error) {
      message.error(parseApiError(error, 'Failed to load invoice.'))
      navigate('/invoices')
    } finally {
      setLoading(false)
    }
  }

  const handleCompanyChange = (companyId) => {
    setSelectedCompanyId(companyId)
    form.setFieldsValue({ customerRef: undefined })
    fetchDropdownData(companyId)
  }

  const handleItemChange = (index, itemId) => {
    const selectedItem = items.find((i) => i.id === itemId)
    if (!selectedItem) return

    const updated = [...lineItems]
    updated[index] = {
      ...updated[index],
      itemRef: itemId,
      itemName: selectedItem.name,
      unitPrice: selectedItem.unitPrice,
      amount: updated[index].quantity * selectedItem.unitPrice,
    }
    setLineItems(updated)
  }

  const handleLineItemChange = (index, field, value) => {
    const updated = [...lineItems]
    updated[index] = { ...updated[index], [field]: value }

    if (field === 'quantity' || field === 'unitPrice') {
      updated[index].amount = (updated[index].quantity || 0) * (updated[index].unitPrice || 0)
    }

    setLineItems(updated)
  }

  const addLineItem = () => {
    setLineItems([
      ...lineItems,
      { itemRef: '', itemName: '', description: '', quantity: 1, unitPrice: 0, amount: 0 },
    ])
  }

  const removeLineItem = (index) => {
    if (lineItems.length === 1) {
      message.warning('At least one line item is required.')
      return
    }
    setLineItems(lineItems.filter((_, i) => i !== index))
  }

  const totalAmount = lineItems.reduce((sum, li) => sum + (li.amount || 0), 0)

  const handleSubmit = async (values) => {
    const invalidItems = lineItems.filter((li) => !li.itemRef || li.quantity <= 0)
    if (invalidItems.length > 0) {
      message.error('Please fill in all line items correctly.')
      return
    }

    setSubmitLoading(true)

    const selectedCustomer = customers.find((c) => c.id === values.customerRef)

    const payload = {
      companyId: selectedCompanyId,
      customerRef: values.customerRef,
      customerName: selectedCustomer?.displayName || '',
      invoiceDate: values.invoiceDate.toISOString(),
      dueDate: values.dueDate.toISOString(),
      memo: values.memo || null,
      lineItems: lineItems.map((li) => ({
        itemRef: li.itemRef,
        itemName: li.itemName,
        description: li.description || null,
        quantity: li.quantity,
        unitPrice: li.unitPrice,
      })),
    }

    try {
      if (isEdit) {
        await dispatch(updateInvoice({ id: parseInt(id), data: payload })).unwrap()
        message.success('Invoice updated successfully.')
      } else {
        await dispatch(createInvoice(payload)).unwrap()
        message.success('Invoice created successfully.')
      }
      navigate('/invoices')
    } catch (error) {
      message.error(parseApiError(error, `Failed to ${isEdit ? 'update' : 'create'} invoice.`))
    } finally {
      setSubmitLoading(false)
    }
  }

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: 80 }}>
        <Spin size="large" />
      </div>
    )
  }

  return (
    <div>
      <PageHeader
        title={isEdit ? 'Edit Invoice' : 'Create Invoice'}
        subtitle={
          isEdit
            ? 'Update invoice details in QuickBooks and the database.'
            : 'Create a new invoice and sync it with QuickBooks.'
        }
        extra={
          <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/invoices')}>
            Back to Invoices
          </Button>
        }
        breadcrumbs={[
          { title: 'Invoices', onClick: () => navigate('/invoices') },
          { title: isEdit ? 'Edit Invoice' : 'New Invoice' },
        ]}
      />

      <Form
        form={form}
        layout="vertical"
        onFinish={handleSubmit}
        requiredMark={false}
        initialValues={{
          companyId: selectedCompanyId,
          invoiceDate: dayjs(),
          dueDate: dayjs().add(30, 'day'),
        }}
      >
        <Card className="form-card" style={{ maxWidth: '100%', marginBottom: 24 }}>
          <Text strong style={{ fontSize: 16, display: 'block', marginBottom: 20 }}>
            Invoice Details
          </Text>

          <Row gutter={[16, 0]}>
            <Col xs={24} md={8}>
              <Form.Item
                name="companyId"
                label="Company"
                rules={[{ required: true, message: 'Company is required' }]}
              >
                <Select
                  placeholder="Select connected company"
                  size="large"
                  onChange={handleCompanyChange}
                  disabled={isEdit}
                >
                  {companies.map((company) => (
                    <Option key={company.id} value={company.id}>
                      {company.companyName}
                    </Option>
                  ))}
                </Select>
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item
                name="customerRef"
                label="Customer"
                rules={[{ required: true, message: 'Customer is required' }]}
              >
                <Select
                  placeholder="Select a customer"
                  size="large"
                  showSearch
                  filterOption={(input, option) =>
                    option?.children?.toLowerCase().includes(input.toLowerCase())
                  }
                >
                  {customers.map((c) => (
                    <Option key={c.id} value={c.id}>
                      {c.displayName}
                    </Option>
                  ))}
                </Select>
              </Form.Item>
            </Col>

            <Col xs={24} md={8}>
              <Form.Item
                name="invoiceDate"
                label="Invoice Date"
                rules={[{ required: true, message: 'Invoice date is required' }]}
              >
                <DatePicker style={{ width: '100%' }} size="large" format="MM/DD/YYYY" />
              </Form.Item>
            </Col>

            <Col xs={24} md={8}>
              <Form.Item
                name="dueDate"
                label="Due Date"
                rules={[
                  { required: true, message: 'Due date is required' },
                  ({ getFieldValue }) => ({
                    validator(_, value) {
                      if (!value || !getFieldValue('invoiceDate')) return Promise.resolve()
                      if (value.isBefore(getFieldValue('invoiceDate'))) {
                        return Promise.reject(new Error('Due date must be on or after invoice date'))
                      }
                      return Promise.resolve()
                    },
                  }),
                ]}
              >
                <DatePicker style={{ width: '100%' }} size="large" format="MM/DD/YYYY" />
              </Form.Item>
            </Col>

            <Col xs={24}>
              <Form.Item name="memo" label="Memo / Notes">
                <Input.TextArea rows={3} placeholder="Optional notes for this invoice..." />
              </Form.Item>
            </Col>
          </Row>
        </Card>

        <Card className="form-card" style={{ maxWidth: '100%', marginBottom: 24 }}>
          <div
            style={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              marginBottom: 20,
            }}
          >
            <Text strong style={{ fontSize: 16 }}>
              Line Items
            </Text>
            <Button
              type="dashed"
              icon={<PlusOutlined />}
              onClick={addLineItem}
            >
              Add Line Item
            </Button>
          </div>

          {lineItems.map((lineItem, index) => (
            <div key={index} className="line-item-row">
              <Row gutter={[12, 0]} align="middle">
                <Col xs={24} sm={24} md={6}>
                  <div style={{ marginBottom: 8 }}>
                    <Text type="secondary" style={{ fontSize: 12 }}>
                      Item *
                    </Text>
                  </div>
                  <Select
                    placeholder="Select item"
                    value={lineItem.itemRef || undefined}
                    onChange={(val) => handleItemChange(index, val)}
                    style={{ width: '100%' }}
                    showSearch
                    filterOption={(input, option) =>
                      option?.children?.toLowerCase().includes(input.toLowerCase())
                    }
                  >
                    {items.map((item) => (
                      <Option key={item.id} value={item.id}>
                        {item.name}
                      </Option>
                    ))}
                  </Select>
                </Col>

                <Col xs={24} sm={24} md={5}>
                  <div style={{ marginBottom: 8 }}>
                    <Text type="secondary" style={{ fontSize: 12 }}>
                      Description
                    </Text>
                  </div>
                  <Input
                    placeholder="Optional"
                    value={lineItem.description}
                    onChange={(e) => handleLineItemChange(index, 'description', e.target.value)}
                  />
                </Col>

                <Col xs={8} sm={8} md={3}>
                  <div style={{ marginBottom: 8 }}>
                    <Text type="secondary" style={{ fontSize: 12 }}>
                      Quantity *
                    </Text>
                  </div>
                  <InputNumber
                    min={0.01}
                    precision={2}
                    value={lineItem.quantity}
                    onChange={(val) => handleLineItemChange(index, 'quantity', val || 0)}
                    style={{ width: '100%' }}
                  />
                </Col>

                <Col xs={8} sm={8} md={4}>
                  <div style={{ marginBottom: 8 }}>
                    <Text type="secondary" style={{ fontSize: 12 }}>
                      Unit Price *
                    </Text>
                  </div>
                  <InputNumber
                    min={0}
                    precision={2}
                    prefix="$"
                    value={lineItem.unitPrice}
                    onChange={(val) => handleLineItemChange(index, 'unitPrice', val || 0)}
                    style={{ width: '100%' }}
                  />
                </Col>

                <Col xs={6} sm={6} md={4}>
                  <div style={{ marginBottom: 8 }}>
                    <Text type="secondary" style={{ fontSize: 12 }}>
                      Amount
                    </Text>
                  </div>
                  <Text strong style={{ color: '#15803d', fontSize: 15 }}>
                    ${(lineItem.amount || 0).toFixed(2)}
                  </Text>
                </Col>

                <Col xs={2} sm={2} md={2} style={{ textAlign: 'center' }}>
                  <Button
                    type="text"
                    danger
                    icon={<DeleteOutlined />}
                    onClick={() => removeLineItem(index)}
                    style={{ marginTop: 24 }}
                  />
                </Col>
              </Row>
            </div>
          ))}

          <Divider />

          <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
            <div
              style={{
                background: '#f8f9ff',
                borderRadius: 8,
                padding: '16px 24px',
                minWidth: 200,
                border: '1px solid #e5e7eb',
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', gap: 32 }}>
                <Text type="secondary">Total Amount:</Text>
                <Text strong style={{ fontSize: 18, color: '#0f766e' }}>
                  ${totalAmount.toFixed(2)}
                </Text>
              </div>
            </div>
          </div>
        </Card>

        <Space>
          <Button
            type="primary"
            htmlType="submit"
            loading={submitLoading}
            icon={<SaveOutlined />}
            size="large"
            style={{ minWidth: 160 }}
          >
            {isEdit ? 'Update Invoice' : 'Create Invoice'}
          </Button>
          <Button size="large" onClick={() => navigate('/invoices')}>
            Cancel
          </Button>
        </Space>
      </Form>
    </div>
  )
}

export default InvoiceForm

