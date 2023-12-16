
namespace Upolnicek.Data
{
    internal class CourseTask
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public CourseTask(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
