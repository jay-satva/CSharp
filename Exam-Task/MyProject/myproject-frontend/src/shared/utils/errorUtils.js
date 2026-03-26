export const parseApiError = (error, fallbackMessage = 'Something went wrong. Please try again.') => {
  if (!error) {
    return fallbackMessage
  }

  if (typeof error === 'string' && error.trim()) {
    return error
  }

  const data = error?.response?.data

  if (typeof data === 'string' && data.trim()) {
    return data
  }

  if (data?.errorCode === 'VALIDATION_ERROR' && data?.errors && typeof data.errors === 'object') {
    const firstField = Object.keys(data.errors)[0]
    const firstError = firstField ? data.errors[firstField]?.[0] : null
    if (firstError) {
      return firstError
    }
  }

  if (data?.message && typeof data.message === 'string') {
    return data.message
  }

  if (data?.errors && typeof data.errors === 'object') {
    const firstField = Object.keys(data.errors)[0]
    const firstError = firstField ? data.errors[firstField]?.[0] : null
    if (firstError) {
      return firstError
    }
  }

  if (error.message && typeof error.message === 'string') {
    return error.message
  }

  return fallbackMessage
}
