namespace FlomtManager.Core.Entities;

public interface IEntity
{
    public int Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
}
