namespace AuthApi.Entities;

public class Company
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Navigation property
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}
