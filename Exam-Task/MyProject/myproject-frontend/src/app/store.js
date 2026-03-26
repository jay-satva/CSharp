import { configureStore } from '@reduxjs/toolkit'
import authReducer, { initialAuthState } from '../features/auth/model/authSlice'
import invoiceReducer from '../features/invoice/model/invoiceSlice'

const AUTH_STORAGE_KEY = 'myproject.auth'

const loadAuthState = () => {
  try {
    const serialized = localStorage.getItem(AUTH_STORAGE_KEY)
    if (!serialized) return undefined

    const parsed = JSON.parse(serialized)
    const hasSession = Boolean(parsed?.accessToken && parsed?.refreshToken && parsed?.userId)

    return {
      ...initialAuthState,
      ...parsed,
      isAuthenticated: hasSession,
    }
  } catch {
    return undefined
  }
}

const saveAuthState = (authState) => {
  try {
    localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(authState))
  } catch {
    // Ignore localStorage errors to avoid breaking app runtime.
  }
}

export const store = configureStore({
  reducer: {
    auth: authReducer,
    invoice: invoiceReducer,
  },
  preloadedState: {
    auth: loadAuthState() ?? initialAuthState,
  },
})

store.subscribe(() => {
  saveAuthState(store.getState().auth)
})

