import React from 'react'
import ReactDOM from 'react-dom/client'
import { Provider } from 'react-redux'
import { ConfigProvider } from 'antd'
import { store } from './app/store'
import App from './App'
import './index.css'

ReactDOM.createRoot(document.getElementById('root')).render(
    <Provider store={store}>
      <ConfigProvider
        theme={{
          token: {
            colorPrimary: '#0f766e',
            colorInfo: '#0f766e',
            colorSuccess: '#15803d',
            colorWarning: '#b45309',
            colorError: '#b91c1c',
            borderRadius: 12,
            fontFamily: "'Manrope', sans-serif",
          },
          components: {
            Typography: {
              titleMarginBottom: 0,
            },
          },
        }}
      >
        <App />
      </ConfigProvider>
    </Provider>
)
