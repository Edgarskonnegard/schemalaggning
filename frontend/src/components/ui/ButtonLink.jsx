import { Link } from "react-router-dom";

import "./Button.css";

function ButtonLink({
  children,
  className = "",
  fullWidth = false,
  to,
  variant = "primary",
  ...props
}) {
  return (
    <Link
      className={`button button-${variant} ${
        fullWidth ? "button-full" : ""
      } ${className}`.trim()}
      to={to}
      {...props}
    >
      {children}
    </Link>
  );
}

export default ButtonLink;
