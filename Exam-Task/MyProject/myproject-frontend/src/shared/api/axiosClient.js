import axios from 'axios'
import { store } from '../../app/store'
import { setCredentials, clearCredentials } from '../../features/auth/model/authSlice'

const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
})

api.interceptors.request.use(
  (config) => {
    const state = store.getState()
    const token = state.auth.accessToken
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => Promise.reject(error)
)

let isRefreshing = false
let failedQueue = []

const redirectTo = (path) => {
  if (window.location.pathname !== path) {
    window.location.assign(path)
  }
}

const processQueue = (error, token = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error)
    } else {
      prom.resolve(token)
    }
  })
  failedQueue = []
}

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config

    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject })
        })
          .then((token) => {
            originalRequest.headers.Authorization = `Bearer ${token}`
            return api(originalRequest)
          })
          .catch((err) => Promise.reject(err))
      }

      originalRequest._retry = true
      isRefreshing = true

      const state = store.getState()
      const refreshToken = state.auth.refreshToken

      if (!refreshToken) {
        store.dispatch(clearCredentials())
        redirectTo('/signin')
        return Promise.reject(error)
      }

      try {
        const response = await axios.post('/api/auth/refresh-token', {
          refreshToken,
        })

        const {
          accessToken,
          refreshToken: newRefreshToken,
          userId,
          firstName,
          lastName,
          name,
          email,
          authProvider,
          phoneNumber,
          profilePhotoUrl,
          incompleteFields,
        } = response.data

        store.dispatch(
          setCredentials({
            accessToken,
            refreshToken: newRefreshToken,
            userId,
            firstName,
            lastName,
            name,
            email,
            authProvider,
            phoneNumber,
            profilePhotoUrl,
            incompleteFields,
          })
        )

        processQueue(null, accessToken)
        originalRequest.headers.Authorization = `Bearer ${accessToken}`
        return api(originalRequest)
      } catch (refreshError) {
        processQueue(refreshError, null)
        store.dispatch(clearCredentials())
        redirectTo('/signin')
        return Promise.reject(refreshError)
      } finally {
        isRefreshing = false
      }
    }

    if (error.response?.status === 403) {
      redirectTo('/unauthorized')
    }

    return Promise.reject(error)
  }
)

export default api

