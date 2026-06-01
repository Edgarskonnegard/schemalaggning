import "./Select.css";

function Select({
 label,
 value,
 onChange,
 options
}) {

 return(
<label className="select-wrapper">

<span>{label}</span>

<select
 className="select"
 value={value}
 onChange={onChange}
>

{options.map(option=>(

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