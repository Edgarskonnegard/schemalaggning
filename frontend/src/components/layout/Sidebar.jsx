import { NavLink } from "react-router-dom";
import "./Sidebar.css";

function Sidebar({ isOpen, onClose }) {
  return (
    <>
      <div
        className={`drawer-overlay ${isOpen ? "open" : ""}`}
        onClick={onClose}
      />

      <aside className={`drawer ${isOpen ? "open" : ""}`}>
        <h2>Meny</h2>

        <nav className="drawer-nav">
          <NavLink to="/" onClick={onClose}>
            Dashboard
          </NavLink>

          <NavLink to="/schedule" onClick={onClose}>
            Schema
          </NavLink>

          <NavLink to="/edit-schedule" onClick={onClose}>
            Skapa schema
          </NavLink>

          <NavLink to="/employees" onClick={onClose}>
            Anställda
          </NavLink>

          <NavLink to="/create-shift" onClick={onClose}>
            Skapa pass
          </NavLink>

          <NavLink to="/store-coverage" onClick={onClose}>
            Bemanningsbehov
          </NavLink>

          <NavLink to="/schedule-rules" onClick={onClose}>
            Genereringsregler
          </NavLink>

          <NavLink to="/approvals" onClick={onClose}>
            Godkännanden
          </NavLink>
        </nav>
      </aside>
    </>
  );
}

export default Sidebar;
