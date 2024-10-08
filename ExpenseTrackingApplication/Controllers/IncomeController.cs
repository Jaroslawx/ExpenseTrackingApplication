﻿using System.Security.Claims;
using ExpenseTrackingApplication.Interfaces;
using ExpenseTrackingApplication.Models;
using ExpenseTrackingApplication.ViewModels.TransactionViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTrackingApplication.Controllers;

[Authorize]
public class IncomeController : Controller
{
    private readonly IIncomeRepository _incomeRepository;
    private readonly IBudgetRepository _budgetRepository;
    private readonly INotificationRepository _notificationRepository;
    public IncomeController(IIncomeRepository incomeRepository, IBudgetRepository budgetRepository, INotificationRepository notificationRepository)
    {
        _incomeRepository = incomeRepository;
        _budgetRepository = budgetRepository;
        _notificationRepository = notificationRepository;
    }
    
    // POST: Income/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int budgetId, [Bind("Source,Amount,Date,Category,Description")] Income income)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.BudgetId = budgetId;
            return PartialView("_CreateIncomePartialView", income); // Return the view with the error messages
        }
        
        // Check if the user owns the budget
        var ownershipCheckResult = await CheckUserOwnership(budgetId);
        if (ownershipCheckResult != null)
        {
            return ownershipCheckResult;
        }
        
        income.BudgetId = budgetId;

        if (await _incomeRepository.AddAsync(income))
        {
            var budget = await _budgetRepository.GetByIdAsync(budgetId);
            if (budget != null)
            {
                budget.Balance += income.Amount;
                await _budgetRepository.UpdateAsync(budget);
            }
                
            return RedirectToAction("Edit", "Budget", new { id = budgetId });
        }
        
        // If something went wrong
        ViewBag.BudgetId = budgetId;
        return PartialView("_CreateIncomePartialView", income);
    }
    
    // POST: Income/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, IncomeEditViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            ModelState.AddModelError("", "Failed to edit income.");
            return PartialView("_EditIncomePartialView", viewModel);
        }

        var income = await _incomeRepository.GetByIdAsync(id);
        if (income == null)
        {
            return NotFound();
        }
        
        // Check if the user owns the budget
        var ownershipCheckResult = await CheckUserOwnership(income.BudgetId);
        if (ownershipCheckResult != null)
        {
            return ownershipCheckResult;
        }
        
        var budget = await _budgetRepository.GetByIdAsync(income.BudgetId);
        if (budget == null)
        {
            return NotFound();
        }
        
        // Calculate new balance
        var previousAmount = income.Amount;
        var newAmount = viewModel.Amount;
        
        // Update income details
        income.Source = viewModel.Source;
        income.Amount = viewModel.Amount;
        income.Date = viewModel.Date;
        income.Category = viewModel.Category;
        income.Description = viewModel.Description;
        
        // Update the budget balance
        budget.Balance -= previousAmount - newAmount;
        
        // Update the repositories
        await _incomeRepository.UpdateAsync(income);
        await _budgetRepository.UpdateAsync(budget);
        
        return RedirectToAction("Edit", "Budget", new { id = income.BudgetId });
    }
    
    // POST: Income/Delete/{id}
    [HttpPost, ActionName("DeleteIncome")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var income = await _incomeRepository.GetByIdAsync(id);
        if (income == null)
        {
            return NotFound();
        }
        
        // Check if the user owns the budget
        var ownershipCheckResult = await CheckUserOwnership(income.BudgetId);
        if (ownershipCheckResult != null)
        {
            return ownershipCheckResult;
        }
        
        // Get the budget associated with the income
        var budget = await _budgetRepository.GetByIdAsync(income.BudgetId);
        if (budget == null)
        {
            return NotFound();
        }
        
        var budgetId = income.BudgetId;
        var amount = income.Amount;
        
        if (await _incomeRepository.DeleteAsync(income))
        {
            budget.Balance -= amount;
            await _budgetRepository.UpdateAsync(budget);
            return RedirectToAction("Edit", "Budget", new { id = budgetId });
        }
        
        return RedirectToAction("Error", "Home");
    }
    
    // Check if the user owns the budget
    private async Task<IActionResult?> CheckUserOwnership(int budgetId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return NotFound(); // Return 404 if user is not logged in
        }
        
        if (!await _budgetRepository.UserOwnsBudgetAsync(budgetId, userId))
        {
            return NotFound(); // Return 404 if the user does not own the budget
        }

        return null; // Return null if the user owns the budget
    }
}