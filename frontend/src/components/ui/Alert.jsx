import "./Alert.css";

function Alert({ children, variant = "error", className = "" }) {
  if (!children) {
    return null;
  }

  return (
    <p className={`alert alert-${variant} ${className}`.trim()}>
      {children}
    </p>
  );
}

export default Alert;
