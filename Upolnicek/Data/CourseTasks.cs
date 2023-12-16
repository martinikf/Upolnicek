using System.Collections.Generic;

namespace Upolnicek.Data
{
    internal class CourseTasks
    {
        public Course course;

        public IEnumerable<CourseTask> Tasks { get; set; }

        public CourseTasks(Course course, IEnumerable<CourseTask> tasks)
        {
            this.course = course;
            Tasks = tasks;
        }
    }
}
