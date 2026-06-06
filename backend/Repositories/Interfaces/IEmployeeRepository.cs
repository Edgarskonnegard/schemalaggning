using Schemalaggning.Models;

namespace Schemalaggning.Repositories.Interfaces;

public interface IEmployeeRepository
{
    Task<List<Employee>> GetAllAsync();
    Task<List<Employee>> GetByStoreIdAsync(int storeId);
    Task<Employee?> GetByIdAsync(int id);
    Task<Employee?> GetByIdWithDetailsAsync(int id);
    Task<Employee> CreateAsync(Employee employee);
    Task<bool> UpdateAsync(Employee employee);
    Task<bool> DeleteAsync(int id);
    Task<bool> CanWorkShiftTypeAsync(int employeeId, int shiftTypeId);
}
