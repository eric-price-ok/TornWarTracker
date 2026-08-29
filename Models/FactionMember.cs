namespace TornWarTracker.Models
{
    public class FactionMember
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public LastActionInfo LastAction { get; set; } = new();
        public MemberStatus Status { get; set; } = new();
    }
}
