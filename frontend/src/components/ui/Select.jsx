import "./Select.css";

function Select({
 label,
 value,
 onChange,
 options = [],
 children
}) {

 return(
<label className="select-wrapper">

<span>{label}</span>

<select
 className="select"
 value={value}
 onChange={onChange}
>

{children ?? options.map(option=>(

<option
 key={option.value}
 value={option.value}
>

{option.label}

</option>

))}

</select>

</label>

)

}

export default Select;
