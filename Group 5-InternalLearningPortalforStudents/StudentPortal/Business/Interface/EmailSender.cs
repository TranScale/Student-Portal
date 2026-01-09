using Microsoft.AspNetCore.Identity.UI.Services;

namespace StudentPortal.Business.Interface
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Thay vì gửi mail, ta in ra cửa sổ Output của Visual Studio
            System.Diagnostics.Debug.WriteLine($"\n================ EMAIL START ================");
            System.Diagnostics.Debug.WriteLine($"TO: {email}");
            System.Diagnostics.Debug.WriteLine($"SUBJECT: {subject}");
            System.Diagnostics.Debug.WriteLine($"CONTENT: {htmlMessage}");
            System.Diagnostics.Debug.WriteLine($"================ EMAIL END ================\n");

            return Task.CompletedTask;
        }
    }
}
