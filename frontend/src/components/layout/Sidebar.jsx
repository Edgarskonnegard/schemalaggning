import { NavLink } from "react-router-dom";
import { useAuth } from "../../auth/AuthContext";
import "./Sidebar.css";

function Sidebar({ isOpen, onClose }) {
  const { isAdmin } = useAuth();

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

          {isAdmin && (
            <>
              <NavLink to="/me" onClick={onClose}>
                Min sida
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
            </>
          )}

          {!isAdmin && (
            <>
              <NavLink to="/my-schedule" onClick={onClose}>
                Mitt schema
              </NavLink>

              <NavLink to="/leave" onClick={onClose}>
                Ledighet
              </NavLink>

              <NavLink to="/me" onClick={onClose}>
                Min profil
              </NavLink>
            </>
          )}
        </nav>
      </aside>
    </>
  );
}

export default Sidebar;
