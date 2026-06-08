import "./Card.css";

function Card({ children, className = "", as: Component = "section" }) {
  return (
    <Component className={`card ${className}`.trim()}>
      {children}
    </Component>
  );
}

export default Card;
