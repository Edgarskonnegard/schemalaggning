export function getConsecutiveWorkdayWarnings(
  employees,
  dates,
  shiftsByEmployeeAndDate,
  maxConsecutiveDays = 5
) {
  const warnings = {};

  function markRun(employeeId, run) {
    if (run.length <= maxConsecutiveDays) {
      return;
    }

    run.forEach((date) => {
      warnings[`${employeeId}-${date}`] = {
        runLength: run.length,
      };
    });
  }

  employees.forEach((employee) => {
    let currentRun = [];

    dates.forEach((date) => {
      const hasShift =
        (shiftsByEmployeeAndDate[`${employee.id}-${date}`] ?? []).length > 0;

      if (hasShift) {
        currentRun.push(date);
        return;
      }

      markRun(employee.id, currentRun);
      currentRun = [];
    });

    markRun(employee.id, currentRun);
  });

  return warnings;
}
