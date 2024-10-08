﻿using System.Security.Claims;
using ExpenseTrackingApplication.Data.Enum;
using ExpenseTrackingApplication.Interfaces;
using ExpenseTrackingApplication.Models;
using ExpenseTrackingApplication.ViewModels.TransactionViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTrackingApplication.Controllers;

public class BillController : Controller
{
    private readonly IBillRepository _billRepository;
    private readonly IBudgetRepository _budgetRepository;
    private readonly INotificationRepository _notificationRepository;
    public BillController(IBillRepository billRepository, INotificationRepository notificationRepository, IBudgetRepository budgetRepository)
    {
        _billRepository = billRepository;
        _budgetRepository = budgetRepository;
        _notificationRepository = notificationRepository;
    }
    
    // POST: Bill/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int budgetId, [Bind("Name,Amount,DueDate,Frequency")] Bill bill)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.BudgetId = budgetId;
            return PartialView("_CreateBillPartialView", bill); // Return the view with the error messages
        }
        
        // Check if the user owns the budget
        var ownershipCheckResult = await CheckUserOwnership(budgetId);
        if (ownershipCheckResult != null)
        {
            return ownershipCheckResult;
        }
        
        bill.BudgetId = budgetId;

        if (await _billRepository.AddAsync(bill))
        {
            var budget = await _budgetRepository.GetByIdAsync(budgetId);
            if (budget != null)
            {
                await _budgetRepository.UpdateAsync(budget);
            }
                
            return RedirectToAction("Edit", "Budget", new { id = budgetId });
        }
        
        // If something went wrong
        ViewBag.BudgetId = budgetId;
        return PartialView("_CreateBillPartialView", bill);
    }
    
    // POST: Bill/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BillEditViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            ModelState.AddModelError("", "Failed to update the bill. Please correct the errors and try again.");
            return PartialView("_EditBillPartialView", viewModel);
        }
        
        var bill = await _billRepository.GetByIdAsync(id);
        if (bill == null)
        {
            return NotFound();
        }
        
        // Check if the user owns the budget
        var ownershipCheckResult = await CheckUserOwnership(bill.BudgetId);
        if (ownershipCheckResult != null)
        {
            return ownershipCheckResult;
        }
        
        bill.Name = viewModel.Name;
        bill.Amount = viewModel.Amount;
        bill.DueDate = viewModel.DueDate;
        bill.Frequency = viewModel.Frequency;

        await _billRepository.UpdateAsync(bill);
        return RedirectToAction("Edit", "Budget", new { id = bill.BudgetId });
    }
    
    // POST: Bill/Delete/{id}
    [HttpPost, ActionName("DeleteBill")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var bill = await _billRepository.GetByIdAsync(id);
        if (bill == null)
        {
            return NotFound();
        }
        
        // Check if the user owns the budget
        var ownershipCheckResult = await CheckUserOwnership(bill.BudgetId);
        if (ownershipCheckResult != null)
        {
            return ownershipCheckResult;
        }
        
        if (await _billRepository.DeleteAsync(bill))
        {
            return RedirectToAction("Edit", "Budget", new { id = bill.BudgetId });
        }
        
        return RedirectToAction("Error", "Home");
    }
    
    // POST: Bill/Pay/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(int id)
    {
        var bill = await _billRepository.GetByIdAsync(id);
        if (bill == null)
        {
            return NotFound();
        }
        
        // Check if the user owns the budget
        var ownershipCheckResult = await CheckUserOwnership(bill.BudgetId);
        if (ownershipCheckResult != null)
        {
            return ownershipCheckResult;
        }
        
        // Set the bill as paid
        bill.IsPaid = true;

        // Update the bill in the database
        if (await _billRepository.UpdateAsync(bill))
        {
            // Send a notification to the user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _notificationRepository.SendNotificationAsync(userId, "Bill Payment", $"Bill {bill.Name} has been paid.", NotificationType.Bill);

            // Check the frequency and create a new bill if it's recurring
            if (bill.Frequency != BillFrequency.None)
            {
                // Calculate the new due date based on the frequency
                DateTime newDueDate = bill.DueDate;
                switch (bill.Frequency)
                {
                    case BillFrequency.Weekly:
                        newDueDate = newDueDate.AddDays(7);
                        break;
                    case BillFrequency.Monthly:
                        newDueDate = newDueDate.AddMonths(1);
                        break;
                    case BillFrequency.Yearly:
                        newDueDate = newDueDate.AddYears(1);
                        break;
                }

                // Create a new bill based on the existing one
                var newBill = new Bill
                {
                    Name = bill.Name,
                    Amount = bill.Amount,
                    DueDate = newDueDate,
                    Frequency = bill.Frequency,
                    IsPaid = false,
                    ReminderSent = false,
                    OverdueReminderSent = false,
                    BudgetId = bill.BudgetId
                };

                // Add the new bill to the repository
                await _billRepository.AddAsync(newBill);
            }

            return RedirectToAction("Details", "Budget", new { id = bill.BudgetId });
        }

        return View("Error");
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