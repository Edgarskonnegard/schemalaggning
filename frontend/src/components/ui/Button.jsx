import "./Button.css";

function Button({
  children,
  className = "",
  onClick,
  type = "button",
  variant = "primary",
  fullWidth = false,
  ...props
}) {
  return (
    <button
      className={`button button-${variant} ${fullWidth ? "button-full" : ""} ${className}`.trim()}
      type={type}
      onClick={onClick}
      {...props}
    >
      {children}
    </button>
  );
}

export default Button;
