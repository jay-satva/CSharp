import { createSlice, createAsyncThunk } from '@reduxjs/toolkit'
import api from '../../../shared/api/axiosClient'
import { parseApiError } from '../../../shared/utils/errorUtils'

export const fetchInvoices = createAsyncThunk('invoice/fetchAll', async (_, { rejectWithValue }) => {
  try {
    const response = await api.get('/invoice')
    return response.data
  } catch (error) {
    return rejectWithValue(parseApiError(error, 'Failed to fetch invoices.'))
  }
})

export const createInvoice = createAsyncThunk('invoice/create', async (data, { rejectWithValue }) => {
  try {
    const response = await api.post('/invoice', data)
    return response.data
  } catch (error) {
    return rejectWithValue(parseApiError(error, 'Failed to create invoice.'))
  }
})

export const updateInvoice = createAsyncThunk('invoice/update', async ({ id, data }, { rejectWithValue }) => {
  try {
    const response = await api.put(`/invoice/${id}`, data)
    return response.data
  } catch (error) {
    return rejectWithValue(parseApiError(error, 'Failed to update invoice.'))
  }
})

export const deleteInvoice = createAsyncThunk('invoice/delete', async (id, { rejectWithValue }) => {
  try {
    await api.delete(`/invoice/${id}`)
    return id
  } catch (error) {
    return rejectWithValue(parseApiError(error, 'Failed to delete invoice.'))
  }
})

const invoiceSlice = createSlice({
  name: 'invoice',
  initialState: {
    invoices: [],
    loading: false,
    error: null,
  },
  reducers: {
    clearInvoiceError: (state) => {
      state.error = null
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchInvoices.pending, (state) => {
        state.loading = true
        state.error = null
      })
      .addCase(fetchInvoices.fulfilled, (state, action) => {
        state.loading = false
        state.invoices = action.payload
      })
      .addCase(fetchInvoices.rejected, (state, action) => {
        state.loading = false
        state.error = action.payload
      })
      .addCase(createInvoice.fulfilled, (state, action) => {
        state.invoices.unshift(action.payload)
      })
      .addCase(updateInvoice.fulfilled, (state, action) => {
        const index = state.invoices.findIndex((i) => i.id === action.payload.id)
        if (index !== -1) state.invoices[index] = action.payload
      })
      .addCase(deleteInvoice.fulfilled, (state, action) => {
        state.invoices = state.invoices.filter((i) => i.id !== action.payload)
      })
  },
})

export const { clearInvoiceError } = invoiceSlice.actions
export default invoiceSlice.reducer

