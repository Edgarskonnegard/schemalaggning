import "./Select.css";

function Select({
  label,
  value,
  onChange,
  options = [],
  children,
  className = "",
  ...props
}) {
  return (
    <label className={`select-wrapper ${className}`.trim()}>
      {label && <span>{label}</span>}
      <select
        className="select"
        value={value}
        onChange={onChange}
        {...props}
      >
        {children ?? options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </label>
  );
}

export default Select;
