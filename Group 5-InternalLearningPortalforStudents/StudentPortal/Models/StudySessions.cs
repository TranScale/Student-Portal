namespace StudentPortal.Models
{
    [Flags]
    public enum StudySessions
    {
        None = 0,
        Ca1 = 1 << 0,
        Ca2 = 1 << 1,
        Ca3 = 1 << 2,
        Ca4 = 1 << 3,
        Ca5 = 1 << 4
    }
}
