import { createSlice } from '@reduxjs/toolkit'

export const initialAuthState = {
  accessToken: null,
  refreshToken: null,
  userId: null,
  firstName: null,
  lastName: null,
  name: null,
  email: null,
  authProvider: null,
  phoneNumber: null,
  profilePhotoUrl: null,
  incompleteFields: [],
  isAuthenticated: false,
}

const authSlice = createSlice({
  name: 'auth',
  initialState: initialAuthState,
  reducers: {
    setCredentials: (state, action) => {
      const firstName = action.payload.firstName ?? null
      const lastName = action.payload.lastName ?? null
      const fallbackName = [firstName, lastName].filter(Boolean).join(' ').trim()
      state.accessToken = action.payload.accessToken
      state.refreshToken = action.payload.refreshToken
      state.userId = action.payload.userId
      state.firstName = firstName
      state.lastName = lastName
      state.name = action.payload.name ?? fallbackName
      state.email = action.payload.email
      state.authProvider = action.payload.authProvider ?? null
      state.phoneNumber = action.payload.phoneNumber ?? null
      state.profilePhotoUrl = action.payload.profilePhotoUrl ?? null
      state.incompleteFields = action.payload.incompleteFields ?? []
      state.isAuthenticated = true
    },
    setProfile: (state, action) => {
      const firstName = action.payload.firstName ?? state.firstName
      const lastName = action.payload.lastName ?? state.lastName
      state.firstName = firstName
      state.lastName = lastName
      state.name = action.payload.name ?? [firstName, lastName].filter(Boolean).join(' ').trim()
      state.email = action.payload.email ?? state.email
      state.authProvider = action.payload.authProvider ?? state.authProvider
      state.phoneNumber = action.payload.phoneNumber ?? null
      state.profilePhotoUrl = action.payload.profilePhotoUrl ?? null
      state.incompleteFields = action.payload.incompleteFields ?? []
    },
    clearCredentials: (state) => {
      state.accessToken = null
      state.refreshToken = null
      state.userId = null
      state.firstName = null
      state.lastName = null
      state.name = null
      state.email = null
      state.authProvider = null
      state.phoneNumber = null
      state.profilePhotoUrl = null
      state.incompleteFields = []
      state.isAuthenticated = false
    },
  },
})

export const { setCredentials, setProfile, clearCredentials } = authSlice.actions
export default authSlice.reducer
