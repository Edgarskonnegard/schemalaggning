import "./Input.css";

function Input({
 label,
 type="text",
 value,
 onChange,
 placeholder
}) {

 return(
   <label className="input-wrapper">

    <span>{label}</span>

    <input
      className="input"
      type={type}
      value={value}
      onChange={onChange}
      placeholder={placeholder}
    />

   </label>
 )
}

export default Input;