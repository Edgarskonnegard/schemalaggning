import { Navigate, useLocation } from "react-router-dom";

import { useAuth } from "../../auth/AuthContext";

function ProtectedRoute({ children, requireAdmin = false }) {
  const location = useLocation();
  const { isAuthenticated, isAdmin } = useAuth();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  if (requireAdmin && !isAdmin) {
    return <Navigate to="/schedule" replace />;
  }

  return children;
}

export default ProtectedRoute;
