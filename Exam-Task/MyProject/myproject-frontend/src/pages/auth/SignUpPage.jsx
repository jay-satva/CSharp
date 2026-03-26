import { Form, Input, Button, Card, Typography, message, Divider } from 'antd'
import { UserOutlined, MailOutlined, LockOutlined } from '@ant-design/icons'
import { useNavigate, Link, useSearchParams } from 'react-router-dom'
import { useDispatch } from 'react-redux'
import { useEffect, useState } from 'react'
import { setCredentials } from '../../features/auth/model/authSlice'
import api from '../../shared/api/axiosClient'
import { parseApiError } from '../../shared/utils/errorUtils'

const { Title, Text } = Typography

const SignUp = () => {
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
      navigate('/signup', { replace: true })
    }
  }, [searchParams, navigate])

  const handleSignUp = async (values) => {
    setLoading(true)
    try {
      const payload = {
        firstName: values.firstName,
        lastName: values.lastName,
        email: values.email,
        password: values.password,
      }

      const response = await api.post('/auth/signup', payload)
      dispatch(setCredentials(response.data))
      message.success('Account created successfully!')
      navigate('/dashboard')
    } catch (error) {
      message.error(parseApiError(error, 'Sign up failed. Please try again.'))
    } finally {
      setLoading(false)
    }
  }

  const handleIntuitSignUp = async () => {
    setIntuitLoading(true)
    try {
      const response = await api.get('/auth/intuit/login?mode=signup')
      window.location.href = response.data.url
    } catch (error) {
      message.error(parseApiError(error, 'Failed to initiate Intuit sign up.'))
      setIntuitLoading(false)
    }
  }

  return (
    <div className="auth-page">
      <Card className="auth-card" bordered={false}>
        <div className="auth-logo">
          <Title level={2} style={{ color: '#0f766e', margin: 0 }}>CIITA</Title>
          <Text type="secondary">Create your account to get started</Text>
        </div>

        <Divider />

        <Button
          className="intuit-btn"
          loading={intuitLoading}
          onClick={handleIntuitSignUp}
          icon={
            <img
              src="../../../public/Intuit_Logo.png"
              alt="Intuit"
              className="intuit-logo"
            />
          }
        >
          Sign up with Intuit
        </Button>

        <div className="auth-divider">or sign up with email</div>

        <Form
          form={form}
          layout="vertical"
          onFinish={handleSignUp}
          requiredMark={false}
          size="large"
          scrollToFirstError
        >
          <Form.Item
            name="firstName"
            label="First Name"
            rules={[
              { required: true, message: 'Please enter your first name' },
              { min: 2, message: 'First name must be at least 2 characters' },
              { max: 50, message: 'First name cannot exceed 50 characters' },
            ]}
          >
            <Input prefix={<UserOutlined style={{ color: '#9CA3AF' }} />} placeholder="John" />
          </Form.Item>

          <Form.Item
            name="lastName"
            label="Last Name"
            rules={[
              { required: true, message: 'Please enter your last name' },
              { min: 2, message: 'Last name must be at least 2 characters' },
              { max: 50, message: 'Last name cannot exceed 50 characters' },
            ]}
          >
            <Input prefix={<UserOutlined style={{ color: '#9CA3AF' }} />} placeholder="Doe" />
          </Form.Item>

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
            rules={[
              { required: true, message: 'Please enter your password' },
              { min: 8, message: 'Password must be at least 8 characters' },
              {
                pattern: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$/,
                message: 'Password must include uppercase, lowercase, number and special character',
              },
            ]}
          >
            <Input.Password prefix={<LockOutlined style={{ color: '#9CA3AF' }} />} placeholder="********" />
          </Form.Item>
          <Text type="secondary" style={{ display: 'block', marginTop: -8, marginBottom: 12 }}>
            Password policy: at least 8 characters, including uppercase, lowercase, number, and special character.
          </Text>

          <Form.Item
            name="confirmPassword"
            label="Confirm Password"
            dependencies={['password']}
            rules={[
              { required: true, message: 'Please confirm your password' },
              ({ getFieldValue }) => ({
                validator(_, value) {
                  if (!value || getFieldValue('password') === value) {
                    return Promise.resolve()
                  }
                  return Promise.reject(new Error('Passwords do not match'))
                },
              }),
            ]}
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
              Create Account
            </Button>
          </Form.Item>
        </Form>

        <div style={{ textAlign: 'center' }}>
          <Text type="secondary">Already have an account? </Text>
          <Link to="/signin" style={{ color: '#0f766e', fontWeight: 600 }}>Sign in</Link>
        </div>
      </Card>
    </div>
  )
}

export default SignUp

