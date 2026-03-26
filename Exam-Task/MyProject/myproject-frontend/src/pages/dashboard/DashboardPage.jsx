import { Row, Col, Card, Statistic, Typography, Spin, Empty } from 'antd'
import {
  FileTextOutlined,
  DollarOutlined,
  CheckCircleOutlined,
  ClockCircleOutlined,
} from '@ant-design/icons'
import { useEffect } from 'react'
import { useDispatch, useSelector } from 'react-redux'
import { fetchInvoices } from '../../features/invoice/model/invoiceSlice'
import PageHeader from '../../shared/components/PageHeader'

const { Text } = Typography

const Dashboard = () => {
  const dispatch = useDispatch()
  const { invoices, loading } = useSelector((state) => state.invoice)
  const { name } = useSelector((state) => state.auth)

  useEffect(() => {
    dispatch(fetchInvoices())
  }, [dispatch])

  const totalInvoices = invoices.length
  const totalAmount = invoices.reduce((sum, inv) => sum + inv.totalAmount, 0)
  const paidInvoices = invoices.filter((inv) => inv.status === 'Paid').length
  const pendingInvoices = invoices.filter((inv) => inv.status === 'Draft' || inv.status === 'Sent').length

  const stats = [
    {
      title: 'Total Invoices',
      value: totalInvoices,
      icon: <FileTextOutlined style={{ fontSize: 28, color: '#0f766e' }} />,
      color: '#ccfbf1',
    },
    {
      title: 'Total Revenue',
      value: totalAmount,
      prefix: '$',
      precision: 2,
      icon: <DollarOutlined style={{ fontSize: 28, color: '#15803d' }} />,
      color: '#dcfce7',
    },
    {
      title: 'Paid Invoices',
      value: paidInvoices,
      icon: <CheckCircleOutlined style={{ fontSize: 28, color: '#15803d' }} />,
      color: '#dcfce7',
    },
    {
      title: 'Pending Invoices',
      value: pendingInvoices,
      icon: <ClockCircleOutlined style={{ fontSize: 28, color: '#b45309' }} />,
      color: '#fef3c7',
    },
  ]

  return (
    <div>
      <PageHeader
        title={`Welcome back, ${name || 'there'}!`}
        subtitle="Here's an overview of your invoicing activity."
      />

      {loading ? (
        <div style={{ textAlign: 'center', padding: 80 }}>
          <Spin size="large" />
        </div>
      ) : (
        <>
          <Row gutter={[16, 16]}>
            {stats.map((stat) => (
              <Col xs={24} sm={12} lg={6} key={stat.title}>
                <Card className="stat-card">
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                    <Statistic
                      title={<Text type="secondary" style={{ fontSize: 13 }}>{stat.title}</Text>}
                      value={stat.value}
                      prefix={stat.prefix}
                      precision={stat.precision}
                      valueStyle={{ fontSize: 28, fontWeight: 700 }}
                    />
                    <div
                      style={{
                        background: stat.color,
                        borderRadius: 12,
                        padding: 12,
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                      }}
                    >
                      {stat.icon}
                    </div>
                  </div>
                </Card>
              </Col>
            ))}
          </Row>

          <Card className="stat-card" style={{ marginTop: 24 }}>
            <Text strong style={{ fontSize: 16 }}>Recent Invoices</Text>
            {invoices.length === 0 ? (
              <Empty description="No invoices yet" style={{ padding: '40px 0' }} />
            ) : (
              <div style={{ marginTop: 16 }}>
                {invoices.slice(0, 5).map((invoice) => (
                  <div
                    key={invoice.id}
                    style={{
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'center',
                      padding: '12px 0',
                      borderBottom: '1px solid #f0f0f0',
                    }}
                  >
                    <div>
                      <Text strong>{invoice.customerName}</Text>
                      <br />
                      <Text type="secondary" style={{ fontSize: 12 }}>
                        {new Date(invoice.invoiceDate).toLocaleDateString()}
                      </Text>
                    </div>
                    <div style={{ textAlign: 'right' }}>
                      <Text strong style={{ color: '#0f766e' }}>
                        ${invoice.totalAmount.toFixed(2)}
                      </Text>
                      <br />
                      <span
                        className="status-badge"
                        style={{
                          background:
                            invoice.status === 'Paid'
                              ? '#dcfce7'
                              : invoice.status === 'Overdue'
                              ? '#fee2e2'
                              : '#ccfbf1',
                          color:
                            invoice.status === 'Paid'
                              ? '#15803d'
                              : invoice.status === 'Overdue'
                              ? '#b91c1c'
                              : '#0f766e',
                        }}
                      >
                        {invoice.status}
                      </span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </Card>
        </>
      )}
    </div>
  )
}

export default Dashboard

