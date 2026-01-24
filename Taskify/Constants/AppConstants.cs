namespace Taskify.Constants;

public class AppConstants
{
    public static class AppRegex
    {
        public const string Phone = @"^(\+420)?\s?[1-9][0-9]{2}\s?[0-9]{3}\s?[0-9]{3}$";
        public const string Email = @"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$";
    }
}