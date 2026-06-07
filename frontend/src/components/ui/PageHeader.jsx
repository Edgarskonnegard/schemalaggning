import "./PageHeader.css";

function PageHeader({ title, description, actions, eyebrow, children }) {
  return (
    <div className="page-header">
      <div>
        {eyebrow && <p className="page-header-eyebrow">{eyebrow}</p>}
        <h1>{title}</h1>
        {description && <p>{description}</p>}
        {children}
      </div>
      {actions && <div className="page-header-actions">{actions}</div>}
    </div>
  );
}

export default PageHeader;
