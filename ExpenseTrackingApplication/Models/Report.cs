﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ExpenseTrackingApplication.Data.Enum;

namespace ExpenseTrackingApplication.Models;

public class Report
{
    [Key]
    public int Id { get; init; }
    public ReportType Type { get; set; }
    
    [MaxLength(100)]
    public string? ReportName { get; set; }
    public DateTime CreatedDate { get; set; }
    
    [ForeignKey("Budget")]
    public int BudgetId { get; set; }
    public Budget Budget { get; set; }
    
    // Two generic date fields to store year/month or date range
    public DateTime? DateOne { get; set; }  // Could store Year or StartDate
    public DateTime? DateTwo { get; set; }  // Could store Month or EndDate
}