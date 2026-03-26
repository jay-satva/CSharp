import { useEffect, useMemo, useState } from 'react'
import { Card, Form, Input, Button, Row, Col, Typography, message, Tag } from 'antd'
import { useDispatch, useSelector } from 'react-redux'
import api from '../../shared/api/axiosClient'
import PageHeader from '../../shared/components/PageHeader'
import { parseApiError } from '../../shared/utils/errorUtils'
import { setProfile } from '../../features/auth/model/authSlice'

const { Text } = Typography

const Profile = () => {
  const [form] = Form.useForm()
  const dispatch = useDispatch()
  const { authProvider } = useSelector((state) => state.auth)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [incompleteFields, setIncompleteFields] = useState([])

  const isManualUser = authProvider === 'manual'
  const incompleteSet = useMemo(() => new Set(incompleteFields), [incompleteFields])

  useEffect(() => {
    const loadProfile = async () => {
      setLoading(true)
      try {
        const { data } = await api.get('/users/me')
        form.setFieldsValue({
          firstName: data.firstName,
          lastName: data.lastName,
          email: data.email,
          phoneNumber: data.phoneNumber,
          currentPassword: '',
          newPassword: '',
          confirmNewPassword: '',
        })
        setIncompleteFields(data.incompleteFields || [])
        dispatch(setProfile(data))
      } catch (error) {
        message.error(parseApiError(error, 'Failed to load profile.'))
      } finally {
        setLoading(false)
      }
    }

    loadProfile()
  }, [dispatch, form])

  const labelWithStatus = (label, fieldKey) => (
    <span>
      {label}
      {incompleteSet.has(fieldKey) && (
        <Tag color="orange" style={{ marginLeft: 8 }}>
          Incomplete
        </Tag>
      )}
    </span>
  )

  const handleSave = async (values) => {
    setSaving(true)
    try {
      const payload = {
        firstName: values.firstName,
        lastName: values.lastName,
        email: values.email,
        phoneNumber: values.phoneNumber || null,
        currentPassword: values.currentPassword || null,
        newPassword: values.newPassword || null,
      }

      const { data } = await api.patch('/users/me', payload)
      dispatch(setProfile(data))
      setIncompleteFields(data.incompleteFields || [])
      form.setFieldsValue({
        currentPassword: '',
        newPassword: '',
        confirmNewPassword: '',
      })
      message.success('Profile updated successfully.')
    } catch (error) {
      message.error(parseApiError(error, 'Failed to update profile.'))
    } finally {
      setSaving(false)
    }
  }

  return (
    <div>
      <PageHeader
        title="Profile"
        subtitle="Manage your account details and complete any missing profile fields."
      />

      <Card className="form-card" loading={loading}>
        <Form
          form={form}
          layout="vertical"
          onFinish={handleSave}
          requiredMark={false}
          size="large"
        >
          <Row gutter={16}>
            <Col xs={24} md={12}>
              <Form.Item
                name="firstName"
                label={labelWithStatus('First Name', 'firstName')}
                rules={[
                  { required: true, message: 'First name is required' },
                  { min: 2, message: 'First name must be at least 2 characters' },
                  { max: 50, message: 'First name cannot exceed 50 characters' },
                ]}
              >
                <Input placeholder="First name" />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item
                name="lastName"
                label={labelWithStatus('Last Name', 'lastName')}
                rules={[
                  { required: true, message: 'Last name is required' },
                  { min: 2, message: 'Last name must be at least 2 characters' },
                  { max: 50, message: 'Last name cannot exceed 50 characters' },
                ]}
              >
                <Input placeholder="Last name" />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col xs={24} md={12}>
              <Form.Item
                name="email"
                label="Email"
                rules={[
                  { required: true, message: 'Email is required' },
                  { type: 'email', message: 'Please enter a valid email' },
                ]}
              >
                <Input placeholder="Email" disabled={!isManualUser} />
              </Form.Item>
              {!isManualUser && (
                <Text type="secondary" style={{ marginTop: -10, marginBottom: 12, display: 'block' }}>
                  Email is managed by Intuit and cannot be edited here.
                </Text>
              )}
            </Col>
            <Col xs={24} md={12}>
              <Form.Item name="phoneNumber" label={labelWithStatus('Phone Number', 'phoneNumber')}>
                <Input placeholder="Phone number (optional)" />
              </Form.Item>
            </Col>
          </Row>

          {isManualUser && (
            <>
              <Text strong style={{ display: 'block', marginBottom: 10 }}>
                Change Password
              </Text>
              <Row gutter={16}>
                <Col xs={24} md={8}>
                  <Form.Item
                    name="currentPassword"
                    label="Current Password"
                    dependencies={['newPassword']}
                    rules={[
                      ({ getFieldValue }) => ({
                        validator(_, value) {
                          if (!getFieldValue('newPassword') || value) {
                            return Promise.resolve()
                          }
                          return Promise.reject(new Error('Current password is required'))
                        },
                      }),
                    ]}
                  >
                    <Input.Password placeholder="Current password" />
                  </Form.Item>
                </Col>
                <Col xs={24} md={8}>
                  <Form.Item
                    name="newPassword"
                    label="New Password"
                    rules={[
                      {
                        pattern: /^$|^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$/,
                        message: 'Min 8 chars with uppercase, lowercase, number and special character',
                      },
                    ]}
                  >
                    <Input.Password placeholder="New password" />
                  </Form.Item>
                </Col>
                <Col xs={24} md={8}>
                  <Form.Item
                    name="confirmNewPassword"
                    label="Confirm New Password"
                    dependencies={['newPassword']}
                    rules={[
                      ({ getFieldValue }) => ({
                        validator(_, value) {
                          if (!getFieldValue('newPassword') || !value || getFieldValue('newPassword') === value) {
                            return Promise.resolve()
                          }
                          return Promise.reject(new Error('Passwords do not match'))
                        },
                      }),
                    ]}
                  >
                    <Input.Password placeholder="Confirm password" />
                  </Form.Item>
                </Col>
              </Row>
            </>
          )}

          <Button type="primary" htmlType="submit" loading={saving} style={{ minWidth: 130 }}>
            Save Profile
          </Button>
        </Form>
      </Card>
    </div>
  )
}

export default Profile

