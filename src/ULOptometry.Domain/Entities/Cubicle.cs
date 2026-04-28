namespace ULOptometry.Domain.Entities;
public class Cubicle : BaseEntity
{
    public int CubicleNumber { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<CubicleAssignment> Assignments { get; set; } = new List<CubicleAssignment>();
}
