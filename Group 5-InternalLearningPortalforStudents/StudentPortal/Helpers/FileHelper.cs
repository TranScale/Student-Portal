namespace StudentPortal.Helpers
{
    public static class FileHelper
    {
        public static string FormatFileSize(double bytes)
        {
            // Chia cho 1024 hai lần để ra MB
            double mb = bytes / 1024f / 1024f;
            return mb.ToString("0.00") + " MB";
        }
    }
}