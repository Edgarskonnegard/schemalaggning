import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";

import { getPendingBaseScheduleApprovalCount } from "../../api/approvalsApi";
import { useAuth } from "../../auth/AuthContext";
import "./Header.css";
function Header({ onToggleMenu }) {
  const { isAdmin, logout, user } = useAuth();
  const navigate = useNavigate();
  const [pendingCount, setPendingCount] = useState(0);

  function handleLogout() {
    logout();
    navigate("/login", { replace: true });
  }

  useEffect(() => {
    async function loadPendingCount() {
      try {
        const count = await getPendingBaseScheduleApprovalCount();
        setPendingCount(count);
      } catch {
        setPendingCount(0);
      }
    }

    loadPendingCount();
    window.addEventListener("approvals-updated", loadPendingCount);

    return () => {
      window.removeEventListener("approvals-updated", loadPendingCount);
    };
  }, []);

  return (
    <header>
      <button className="menu-button" onClick={onToggleMenu}>
        ☰
      </button>

      <h1>Schemaläggningssystem</h1>

      {isAdmin && (
        <Link className="notification-link" to="/approvals" aria-label="Godkännanden">
          <span className="notification-icon">!</span>
          {pendingCount > 0 && (
            <span className="notification-badge">{pendingCount}</span>
          )}
        </Link>
      )}

      <div className="header-user">
        <span>{user?.email}</span>
        <small>{user?.accessRole}</small>
      </div>

      <button className="logout-button" type="button" onClick={handleLogout}>
        Logga ut
      </button>
    </header>
  );
}

export default Header;
