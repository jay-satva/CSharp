import { useEffect } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useDispatch } from 'react-redux'
import { setCredentials } from '../../features/auth/model/authSlice'
import { Spin } from 'antd'

const AuthCallback = () => {
  const [searchParams] = useSearchParams()
  const dispatch = useDispatch()
  const navigate = useNavigate()

  useEffect(() => {
    const accessToken = searchParams.get('accessToken')
    const refreshToken = searchParams.get('refreshToken')
    const userId = searchParams.get('userId')
    const firstName = searchParams.get('firstName')
    const lastName = searchParams.get('lastName')
    const email = searchParams.get('email')
    const authProvider = searchParams.get('authProvider')
    const phoneNumber = searchParams.get('phoneNumber')
    const profilePhotoUrl = searchParams.get('profilePhotoUrl')
    const incompleteFields = (searchParams.get('incompleteFields') || '')
      .split(',')
      .map((field) => field.trim())
      .filter(Boolean)

    if (accessToken && refreshToken && userId) {
      dispatch(
        setCredentials({
          accessToken,
          refreshToken,
          userId,
          firstName,
          lastName,
          email,
          authProvider,
          phoneNumber: phoneNumber || null,
          profilePhotoUrl: profilePhotoUrl || null,
          incompleteFields,
        })
      )
      navigate('/dashboard', { replace: true })
    } else {
      navigate('/signin', { replace: true })
    }
  }, [searchParams, dispatch, navigate])

  return (
    <div style={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
      <Spin size="large" tip="Signing you in..." />
    </div>
  )
}

export default AuthCallback

