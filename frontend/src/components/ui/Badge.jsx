import "./Badge.css";

function Badge({ children, variant = "info", className = "" }) {
  return (
    <span className={`badge badge-${variant} ${className}`.trim()}>
      {children}
    </span>
  );
}

export default Badge;
