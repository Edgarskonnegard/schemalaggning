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
            .Include(employee => employee.Store)
            .Include(employee => employee.Role)
            .Include(employee => employee.UserAccount)
            .OrderBy(employee => employee.Name)
            .ToListAsync();
    }

    public Task<List<Employee>> GetByStoreIdAsync(int storeId)
    {
        return _context.Employees
            .AsNoTracking()
            .Include(employee => employee.Store)
            .Include(employee => employee.Role)
            .Include(employee => employee.UserAccount)
            .Where(employee => employee.StoreId == storeId)
            .OrderBy(employee => employee.Name)
            .ToListAsync();
    }

    public Task<Employee?> GetByIdAsync(int id)
    {
        return _context.Employees
            .Include(employee => employee.Store)
            .Include(employee => employee.Role)
            .Include(employee => employee.UserAccount)
            .FirstOrDefaultAsync(employee => employee.Id == id);
    }

    public Task<Employee?> GetByIdWithDetailsAsync(int id)
    {
        return _context.Employees
            .AsNoTracking()
            .Include(employee => employee.Store)
            .Include(employee => employee.Role)
            .Include(employee => employee.UserAccount)
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

    public Task<bool> CanWorkShiftTypeAsync(int employeeId, int shiftTypeId)
    {
        return _context.Employees
            .Where(employee => employee.Id == employeeId)
            .AnyAsync(employee => _context.RoleShiftTypes.Any(roleShiftType =>
                roleShiftType.RoleId == employee.RoleId &&
                roleShiftType.ShiftTypeId == shiftTypeId));
    }
}
