import { Layout, Menu, Avatar, Dropdown, Typography } from 'antd'
import {
  DashboardOutlined,
  ApiOutlined,
  BankOutlined,
  UserOutlined,
  ShoppingOutlined,
  FileTextOutlined,
  LogoutOutlined,
  ProfileOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
} from '@ant-design/icons'
import { useState } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { useDispatch, useSelector } from 'react-redux'
import { clearCredentials } from '../features/auth/model/authSlice'
import api from '../shared/api/axiosClient'

const { Sider, Header, Content } = Layout
const { Text } = Typography

const AppLayout = ({ children }) => {
  const [collapsed, setCollapsed] = useState(false)
  const navigate = useNavigate()
  const location = useLocation()
  const dispatch = useDispatch()
  const { name, email, profilePhotoUrl, refreshToken } = useSelector((state) => state.auth)

  const menuItems = [
    { key: '/dashboard', icon: <DashboardOutlined />, label: 'Dashboard' },
    { key: '/connection', icon: <ApiOutlined />, label: 'QuickBooks' },
    { key: '/accounts', icon: <BankOutlined />, label: 'Accounts' },
    { key: '/customers', icon: <UserOutlined />, label: 'Customers' },
    { key: '/items', icon: <ShoppingOutlined />, label: 'Items' },
    { key: '/invoices', icon: <FileTextOutlined />, label: 'Invoices' },
  ]

  const handleLogout = async () => {
    try {
      if (refreshToken) {
        await api.post('/auth/revoke-token', { refreshToken })
      }
    } catch { /* empty */ } finally {
      dispatch(clearCredentials())
      navigate('/signin')
    }
  }

  const userMenuItems = [
    {
      key: 'profile',
      icon: <ProfileOutlined />,
      label: 'Profile',
      onClick: () => navigate('/profile'),
    },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: 'Sign Out',
      danger: true,
      onClick: handleLogout,
    },
  ]

  return (
    <Layout className="page-layout">
      <Sider
        collapsible
        collapsed={collapsed}
        onCollapse={setCollapsed}
        trigger={null}
        width={240}
        breakpoint="lg"
        collapsedWidth={72}
        className="sidebar"
      >
        <div className="sidebar-logo">
          <span className="sidebar-logo-mark">C</span>
          {!collapsed && <span>CIITA</span>}
        </div>
        <Menu
          mode="inline"
          selectedKeys={[location.pathname]}
          items={menuItems}
          onClick={({ key }) => navigate(key)}
        />
      </Sider>

      <Layout>
        <Header className="app-header">
          <button
            type="button"
            onClick={() => setCollapsed(!collapsed)}
            className="menu-trigger"
            aria-label="Toggle navigation"
          >
            {collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
          </button>

          <Dropdown menu={{ items: userMenuItems }} placement="bottomRight">
            <div className="user-chip">
              <Avatar className="user-chip-avatar" src={profilePhotoUrl || undefined}>
                {name?.charAt(0).toUpperCase()}
              </Avatar>
              <div className="user-chip-meta">
                <Text strong className="user-chip-name">{name}</Text>
                <Text type="secondary" className="user-chip-email">{email}</Text>
              </div>
            </div>
          </Dropdown>
        </Header>

        <Content className="main-content">{children}</Content>
      </Layout>
    </Layout>
  )
}

export default AppLayout

