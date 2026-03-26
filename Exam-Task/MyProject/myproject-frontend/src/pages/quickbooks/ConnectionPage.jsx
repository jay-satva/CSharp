import { Card, Button, Typography, Spin, Alert, Popconfirm, message } from 'antd'
import {
  ApiOutlined,
  CheckCircleFilled,
  CloseCircleFilled,
  LinkOutlined,
  DisconnectOutlined,
} from '@ant-design/icons'
import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import api from '../../shared/api/axiosClient'
import PageHeader from '../../shared/components/PageHeader'
import { parseApiError } from '../../shared/utils/errorUtils'

const { Title, Text } = Typography

const Connection = () => {
  const [companies, setCompanies] = useState([])
  const [loading, setLoading] = useState(true)
  const [connectLoading, setConnectLoading] = useState(false)
  const [disconnectingCompanyId, setDisconnectingCompanyId] = useState(null)
  const [searchParams] = useSearchParams()

  useEffect(() => {
    if (searchParams.get('connected') === 'true') {
      message.success('Successfully connected to QuickBooks!')
    }
    fetchCompanies()
  }, [])

  const fetchCompanies = async () => {
    setLoading(true)
    try {
      const response = await api.get('/quickbooks/companies')
      const connectedCompanies = response.data || []
      setCompanies(connectedCompanies)
    } catch {
      setCompanies([])
    } finally {
      setLoading(false)
    }
  }

  const handleConnect = async () => {
    setConnectLoading(true)
    try {
      const response = await api.get('/quickbooks/connect')
      window.location.href = response.data.url
    } catch (error) {
      message.error(parseApiError(error, 'Failed to initiate QuickBooks connection.'))
      setConnectLoading(false)
    }
  }

  const handleDisconnect = async (companyId) => {
    setDisconnectingCompanyId(companyId)
    try {
      await api.post(`/quickbooks/disconnect?companyId=${encodeURIComponent(companyId)}`)
      const remaining = companies.filter((company) => company.id !== companyId)
      setCompanies(remaining)

      message.success('Disconnected from QuickBooks.')
    } catch (error) {
      message.error(parseApiError(error, 'Failed to disconnect from QuickBooks.'))
    } finally {
      setDisconnectingCompanyId(null)
    }
  }

  const hasConnectedCompanies = companies.length > 0

  return (
    <div>
      <PageHeader
        title="QuickBooks Connection"
        subtitle="Connect your QuickBooks account to manage your finances."
      />

      {loading ? (
        <div style={{ textAlign: 'center', padding: 80 }}>
          <Spin size="large" />
        </div>
      ) : (
        <Card className="connection-card">
          <div style={{ textAlign: 'center', padding: '24px 0' }}>
            <div
              style={{
                width: 80,
                height: 80,
                borderRadius: '50%',
                background: hasConnectedCompanies ? '#dcfce7' : '#e2e8f0',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                margin: '0 auto 20px',
              }}
            >
              <ApiOutlined
                style={{
                  fontSize: 36,
                  color: hasConnectedCompanies ? '#15803d' : '#64748b',
                }}
              />
            </div>

            {hasConnectedCompanies ? (
              <>
                <span className="connected-badge" style={{ marginBottom: 16, display: 'inline-flex' }}>
                  <CheckCircleFilled /> Connected ({companies.length})
                </span>

                <Button
                  type="primary"
                  icon={<LinkOutlined />}
                  loading={connectLoading}
                  onClick={handleConnect}
                  style={{ marginBottom: 16}}
                >
                  Add Another Company
                </Button>

                <div style={{ display: 'grid', gap: 12, textAlign: 'left' }}>
                  {companies.map((company) => (
                    <Card key={company.id} size="small" bordered style={{ borderRadius: 12 }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, alignItems: 'center' }}>
                        <div>
                          <Title level={5} style={{ margin: 0 }}>{company.companyName}</Title>
                          <Text type="secondary">Realm ID: {company.realmId}</Text>
                          <br />
                          <Text type="secondary">Connected on {new Date(company.connectedAt).toLocaleDateString()}</Text>
                        </div>
                        <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                          <Popconfirm
                            title="Disconnect QuickBooks Company"
                            description="This company will be hidden from app data views until it is connected again."
                            onConfirm={() => handleDisconnect(company.id)}
                            okText="Disconnect"
                            cancelText="Cancel"
                            okButtonProps={{ danger: true }}
                          >
                            <Button
                              danger
                              icon={<DisconnectOutlined />}
                              loading={disconnectingCompanyId === company.id}
                            >
                              Disconnect
                            </Button>
                          </Popconfirm>
                        </div>
                      </div>
                    </Card>
                  ))}
                </div>
              </>
            ) : (
              <>
                <span className="disconnected-badge" style={{ marginBottom: 16, display: 'inline-flex' }}>
                  <CloseCircleFilled /> Not Connected
                </span>
                <Title level={4} style={{ margin: '12px 0 4px' }}>
                  Connect to QuickBooks
                </Title>
                <Text type="secondary">
                  Link your QuickBooks account to start managing your finances
                </Text>

                <Alert
                  message="QuickBooks is not connected."
                  description="Connect your QuickBooks account to create and manage accounts, customers, items, and invoices."
                  type="warning"
                  showIcon
                  style={{ margin: '24px 0', textAlign: 'left' }}
                />

                <Button
                  type="primary"
                  size="large"
                  icon={<LinkOutlined />}
                  loading={connectLoading}
                  onClick={handleConnect}
                  style={{ minWidth: 200 }}
                >
                  Connect QuickBooks
                </Button>
              </>
            )}
          </div>
        </Card>
      )}
    </div>
  )
}

export default Connection

