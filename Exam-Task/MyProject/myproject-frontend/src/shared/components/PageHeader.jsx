import { Card, Typography, Breadcrumb } from 'antd'

const { Title, Text } = Typography

const PageHeader = ({ title, subtitle, extra, breadcrumbs }) => {
  return (
    <Card className="page-header-card">
      {breadcrumbs && (
        <Breadcrumb items={breadcrumbs} className="page-header-breadcrumbs" />
      )}
      <div className="page-header-content">
        <div className="page-header-copy">
          <Title level={4} className="page-header-title">{title}</Title>
          {subtitle && <Text type="secondary" className="page-header-subtitle">{subtitle}</Text>}
        </div>
        {extra && <div className="page-header-extra">{extra}</div>}
      </div>
    </Card>
  )
}

export default PageHeader