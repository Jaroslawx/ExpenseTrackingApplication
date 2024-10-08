﻿namespace ExpenseTrackingApplication.ViewModels.UserViewModels
{
    public class UserProfileViewModel
    {
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string? AvatarUrl { get; set; }
    }
}