import { Routes, Route } from "react-router-dom";

import ProtectedRoute from "./components/auth/ProtectedRoute";
import AppLayout from "./components/layout/AppLayout";

import DashboardPage from "./pages/DashboardPage";
import SchedulePage from "./pages/SchedulePage";
import EditSchedulePage from "./pages/EditSchedulePage";
import EmployeesPage from "./pages/EmployeesPage";
import CreateShiftPage from "./pages/CreateShiftPage";
import EmployeeDetailsPage from "./pages/EmployeeDetailsPage";
import StoreCoveragePage from "./pages/StoreCoveragePage";
import ApprovalsPage from "./pages/ApprovalsPage";
import ScheduleRulesPage from "./pages/ScheduleRulesPage";
import LoginPage from "./pages/LoginPage";

function AdminRoute({ children }) {
  return <ProtectedRoute requireAdmin>{children}</ProtectedRoute>;
}

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/*"
        element={
          <ProtectedRoute>
            <AppLayout>
              <Routes>
                <Route path="/" element={<DashboardPage />} />
                <Route path="/schedule" element={<SchedulePage />} />
                <Route
                  path="/edit-schedule"
                  element={
                    <AdminRoute>
                      <EditSchedulePage />
                    </AdminRoute>
                  }
                />
                <Route
                  path="/employees"
                  element={
                    <AdminRoute>
                      <EmployeesPage />
                    </AdminRoute>
                  }
                />
                <Route
                  path="/employees/:employeeId"
                  element={
                    <AdminRoute>
                      <EmployeeDetailsPage />
                    </AdminRoute>
                  }
                />
                <Route
                  path="/create-shift"
                  element={
                    <AdminRoute>
                      <CreateShiftPage />
                    </AdminRoute>
                  }
                />
                <Route
                  path="/store-coverage"
                  element={
                    <AdminRoute>
                      <StoreCoveragePage />
                    </AdminRoute>
                  }
                />
                <Route
                  path="/schedule-rules"
                  element={
                    <AdminRoute>
                      <ScheduleRulesPage />
                    </AdminRoute>
                  }
                />
                <Route
                  path="/approvals"
                  element={
                    <AdminRoute>
                      <ApprovalsPage />
                    </AdminRoute>
                  }
                />
              </Routes>
            </AppLayout>
          </ProtectedRoute>
        }
      />
    </Routes>
  );
}

export default App;
