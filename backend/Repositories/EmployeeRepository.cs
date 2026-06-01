using Microsoft.EntityFrameworkCore;
using Schemalaggning.Data;
using Schemalaggning.Models;
using Schemalaggning.Repositories.Interfaces;

namespace Schemalaggning.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _context;

    public EmployeeRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<Employee>> GetAllAsync()
    {
        return _context.Employees
            .AsNoTracking()
            .Include(employee => employee.Role)
            .Include(employee => employee.EmployeeShiftTypes)
            .OrderBy(employee => employee.Name)
            .ToListAsync();
    }

    public Task<Employee?> GetByIdAsync(int id)
    {
        return _context.Employees
            .Include(employee => employee.Role)
            .Include(employee => employee.EmployeeShiftTypes)
            .FirstOrDefaultAsync(employee => employee.Id == id);
    }

    public Task<Employee?> GetByIdWithDetailsAsync(int id)
    {
        return _context.Employees
            .AsNoTracking()
            .Include(employee => employee.Role)
            .Include(employee => employee.EmployeeShiftTypes)
                .ThenInclude(employeeShiftType => employeeShiftType.ShiftType)
            .Include(employee => employee.BaseScheduleRules)
                .ThenInclude(rule => rule.ShiftType)
            .FirstOrDefaultAsync(employee => employee.Id == id);
    }

    public async Task<Employee> CreateAsync(Employee employee)
    {
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();
        return employee;
    }

    public async Task<bool> UpdateAsync(Employee employee)
    {
        _context.Employees.Update(employee);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee is null)
        {
            return false;
        }

        _context.Employees.Remove(employee);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAllowedShiftTypesAsync(int employeeId, List<int> shiftTypeIds)
    {
        var existing = await _context.EmployeeShiftTypes
            .Where(employeeShiftType => employeeShiftType.EmployeeId == employeeId)
            .ToListAsync();

        _context.EmployeeShiftTypes.RemoveRange(existing);

        var uniqueShiftTypeIds = shiftTypeIds.Distinct().ToList();
        foreach (var shiftTypeId in uniqueShiftTypeIds)
        {
            _context.EmployeeShiftTypes.Add(new EmployeeShiftType
            {
                EmployeeId = employeeId,
                ShiftTypeId = shiftTypeId
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public Task<bool> HasAllowedShiftTypeAsync(int employeeId, int shiftTypeId)
    {
        return _context.EmployeeShiftTypes.AnyAsync(employeeShiftType =>
            employeeShiftType.EmployeeId == employeeId &&
            employeeShiftType.ShiftTypeId == shiftTypeId);
    }
}
