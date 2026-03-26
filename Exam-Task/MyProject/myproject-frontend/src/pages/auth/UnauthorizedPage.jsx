import { Button, Card, Typography } from 'antd'
import { useSelector } from 'react-redux'
import { useNavigate } from 'react-router-dom'

const { Title, Paragraph } = Typography

const Unauthorized = () => {
  const navigate = useNavigate()
  const { isAuthenticated } = useSelector((state) => state.auth)

  return (
    <div className="unauthorized-page">
      <Card className="unauthorized-card" bordered={false}>
        <Title level={2}>Access denied</Title>
        <Paragraph type="secondary">
          You are signed in, but your account does not have permission to access this resource.
        </Paragraph>
        <Button type="primary" onClick={() => navigate(isAuthenticated ? '/dashboard' : '/signin')}>
          {isAuthenticated ? 'Back to Dashboard' : 'Go to Sign In'}
        </Button>
      </Card>
    </div>
  )
}

export default Unauthorized
