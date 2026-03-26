import { Form, Input, Button, Card, Typography, message, Divider } from 'antd'
import { MailOutlined, LockOutlined } from '@ant-design/icons'
import { useNavigate, Link, useSearchParams } from 'react-router-dom'
import { useDispatch } from 'react-redux'
import { useEffect, useState } from 'react'
import { setCredentials } from '../../features/auth/model/authSlice'
import api from '../../shared/api/axiosClient'
import { parseApiError } from '../../shared/utils/errorUtils'

const { Title, Text } = Typography

const SignIn = () => {
  const [loading, setLoading] = useState(false)
  const [intuitLoading, setIntuitLoading] = useState(false)
  const [form] = Form.useForm()
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const dispatch = useDispatch()

  useEffect(() => {
    const authError = searchParams.get('authError')
    if (authError) {
      message.error(authError)
      navigate('/signin', { replace: true })
    }
  }, [searchParams, navigate])

  const handleSignIn = async (values) => {
    setLoading(true)
    try {
      const response = await api.post('/auth/signin', values)
      dispatch(setCredentials(response.data))
      message.success('Welcome back!')
      navigate('/dashboard')
    } catch (error) {
      message.error(parseApiError(error, 'Unable to sign in. Please check your credentials and try again.'))
    } finally {
      setLoading(false)
    }
  }

  const handleIntuitSignIn = async () => {
    setIntuitLoading(true)
    try {
      const response = await api.get('/auth/intuit/login?mode=signin')
      window.location.href = response.data.url
    } catch (error) {
      message.error(parseApiError(error, 'Failed to initiate Intuit sign in.'))
      setIntuitLoading(false)
    }
  }

  return (
    <div className="auth-page">
      <Card className="auth-card" bordered={false}>
        <div className="auth-logo">
          <Title level={2} style={{ color: '#0f766e', margin: 0 }}>CIITA</Title>
          <Text type="secondary">Sign in to your account</Text>
        </div>

        <Divider />

        <Button
          className="intuit-btn"
          loading={intuitLoading}
          onClick={handleIntuitSignIn}
          icon={
            <img
              src="../../../public/Intuit_Logo.png"
              alt="Intuit"
              className="intuit-logo"
            />
          }
        >
          Sign in with Intuit
        </Button>

        <div className="auth-divider">or sign in with email</div>

        <Form
          form={form}
          layout="vertical"
          onFinish={handleSignIn}
          requiredMark={false}
          size="large"
        >
          <Form.Item
            name="email"
            label="Email Address"
            rules={[
              { required: true, message: 'Please enter your email' },
              { type: 'email', message: 'Please enter a valid email' },
            ]}
          >
            <Input prefix={<MailOutlined style={{ color: '#9CA3AF' }} />} placeholder="john@example.com" />
          </Form.Item>

          <Form.Item
            name="password"
            label="Password"
            rules={[{ required: true, message: 'Please enter your password' }]}
          >
            <Input.Password prefix={<LockOutlined style={{ color: '#9CA3AF' }} />} placeholder="********" />
          </Form.Item>

          <Form.Item style={{ marginBottom: 12 }}>
            <Button
              type="primary"
              htmlType="submit"
              block
              loading={loading}
              style={{ height: 44, fontWeight: 600 }}
            >
              Sign In
            </Button>
          </Form.Item>
        </Form>

        <div style={{ textAlign: 'center' }}>
          <Text type="secondary">Don&apos;t have an account? </Text>
          <Link to="/signup" style={{ color: '#0f766e', fontWeight: 600 }}>Sign up</Link>
        </div>
      </Card>
    </div>
  )
}

export default SignIn

