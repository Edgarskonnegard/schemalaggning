import "./Button.css";
function Button({
  children,
  className = "",
  onClick,
  type = "button",
  variant,
  ...props
}) {
  return (
    <button
      className={`button ${variant ? `button-${variant}` : ""} ${className}`.trim()}
      type={type}
      onClick={onClick}
      {...props}
    >
      {children}
    </button>
  );
}

export default Button;
