namespace Upolnicek.Data
{
    internal class Assignment
    {
        public int Id;
        public string Name { get; set; }
        public string Description { get; set; }
        public string Deadline { get; set; }
        public string CourseName { get; set; }

        public Assignment(int id, string name, string description, string deadline, string courseName)
        {
            Id = id;
            Name = name;
            Description = description;
            Deadline = deadline;
            CourseName = courseName;
        }
    }
}
