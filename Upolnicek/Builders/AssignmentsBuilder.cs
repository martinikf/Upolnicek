using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Upolnicek.Data;


namespace Upolnicek.Builders
{
    internal class AssignmentsBuilder : IBuilder
    {
        private Action<Assignment> _assignmentButtonOnClick;
        private StackPanel _container;
        private IEnumerable<Assignment> _assignments;

        public AssignmentsBuilder(StackPanel container, IEnumerable<Assignment> assignments, Action<Assignment> method)
        {
            //Onlick method for assignment button
            _assignmentButtonOnClick = method;
            _container = container;
            _assignments = assignments;
        }

        public bool Build()
        { 
            try {
                _container.Children.Clear();

                var lastCourse = String.Empty;

                foreach (var assignment in _assignments.OrderBy(x=>x.CourseName))
                {
                    if(lastCourse != assignment.CourseName)
                    {
                        lastCourse = assignment.CourseName;

                        var courseLabel = new Label
                        {
                            Content = assignment.CourseName,
                            FontSize = 14,
                            HorizontalAlignment = HorizontalAlignment.Left
                        };
                        _container.Children.Add(courseLabel);
                    }

                    try
                    {
                        var button = new Button
                        {
                            Content = assignment.CourseName + " : " + assignment.Name,
                            Margin = new Thickness(2, 2, 2, 2),
                            Padding = new Thickness(5, 2, 5, 2),
                            HorizontalContentAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Center
                        };

                        button.Click += (s, e) =>
                        {
                            _assignmentButtonOnClick(assignment);
                        };

                        _container.Children.Add(button);
                    }
                    catch
                    {
                        var label = new Label()
                        {
                            Content = $"Chyba: {assignment.Name}",
                            FontSize = 16,
                            HorizontalAlignment = HorizontalAlignment.Center
                        };
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
