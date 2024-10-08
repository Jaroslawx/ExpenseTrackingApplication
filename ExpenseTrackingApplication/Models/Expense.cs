﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ExpenseTrackingApplication.Data.Enum;

namespace ExpenseTrackingApplication.Models;

public class Expense
{
    [Key]
    public int Id { get; init; }
    [MaxLength(100)]
    public string? Recipient { get; set; }
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }
    [Required(ErrorMessage = "Date is required.")]
    public DateTime Date { get; set; }
    [Required(ErrorMessage = "Category is required.")]
    public ExpenseCategory Category { get; set; }
    [MaxLength(100)]
    public string? Description { get; set; }
    
    [ForeignKey("Budget")]
    public int BudgetId { get; set; }
    public Budget? Budget { get; set; }
}