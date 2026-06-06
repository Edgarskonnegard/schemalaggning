import { useEffect, useState } from "react";
import { Link } from "react-router-dom";

import { getPendingBaseScheduleApprovalCount } from "../../api/approvalsApi";
import "./Header.css";
function Header({ onToggleMenu }) {
  const [pendingCount, setPendingCount] = useState(0);

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

      <Link className="notification-link" to="/approvals" aria-label="Godkännanden">
        <span className="notification-icon">!</span>
        {pendingCount > 0 && (
          <span className="notification-badge">{pendingCount}</span>
        )}
      </Link>
    </header>
  );
}

export default Header;
