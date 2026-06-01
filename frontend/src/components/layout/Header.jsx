import "./Header.css";
function Header({ onToggleMenu }) {
  return (
    <header
      style={{
        display: "flex",
        gap: "20px",
        alignItems: "center",
      }}
    >
      <button onClick={onToggleMenu}>
        ☰
      </button>

      <h1>Schemaläggningssystem</h1>
    </header>
  );
}

export default Header;