import { Routes, Route } from "react-router-dom";

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

function App() {
  return (
    <AppLayout>
      <Routes>
        <Route path="/" element={<DashboardPage />} />
        <Route path="/schedule" element={<SchedulePage />} />
        <Route path="/edit-schedule" element={<EditSchedulePage />} />
        <Route path="/employees" element={<EmployeesPage />} />
        <Route path="/employees/:employeeId" element={<EmployeeDetailsPage />} />
        <Route path="/create-shift" element={<CreateShiftPage />} />
        <Route path="/store-coverage" element={<StoreCoveragePage />} />
        <Route path="/schedule-rules" element={<ScheduleRulesPage />} />
        <Route path="/approvals" element={<ApprovalsPage />} />
      </Routes>
    </AppLayout>
  );
}

export default App;
