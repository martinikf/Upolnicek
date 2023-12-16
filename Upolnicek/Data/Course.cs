
namespace Upolnicek.Data
{
    internal class Course
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public Course(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
