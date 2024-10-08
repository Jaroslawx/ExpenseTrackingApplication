﻿using ExpenseTrackingApplication.Models;

namespace ExpenseTrackingApplication.Interfaces;

public interface IIncomeRepository
{
    Task<IEnumerable<Income?>> GetAllAsync();
    Task<Income?> GetByIdAsync(int id);
    Task<IEnumerable<Income>> GetByBudgetAsync(int budgetId);
    Task<IEnumerable<Income>> GetByDateRangeAsync(int budgetId, DateTime startDate, DateTime endDate);
    Task<decimal> GetBudgetMonthIncomeAsync(int budgetId);
    Task<int> GetBudgetMonthIncomesCountAsync(int budgetId);
    Task<bool> AddAsync(Income income);
    Task<bool> DeleteAsync(Income income);
    Task<bool> UpdateAsync(Income income);
    Task<bool> SaveAsync();
}