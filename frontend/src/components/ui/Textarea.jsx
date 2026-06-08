import "./Textarea.css";

function Textarea({ className = "", label, ...props }) {
  return (
    <label className={`textarea-wrapper ${className}`.trim()}>
      {label && <span>{label}</span>}
      <textarea className="textarea" {...props} />
    </label>
  );
}

export default Textarea;
