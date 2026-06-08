import "./Input.css";

function Input({
  label,
  type = "text",
  value,
  onChange,
  placeholder,
  className = "",
  ...props
}) {
  return (
    <label className={`input-wrapper ${className}`.trim()}>
      {label && <span>{label}</span>}
      <input
        className="input"
        type={type}
        value={value}
        onChange={onChange}
        placeholder={placeholder}
        {...props}
      />
    </label>
  );
}

export default Input;
