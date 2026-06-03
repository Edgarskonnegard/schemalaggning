import "./Input.css";

function Input({
 label,
 type="text",
 value,
 onChange,
 placeholder,
 ...props
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
      {...props}
    />

   </label>
 )
}

export default Input;
