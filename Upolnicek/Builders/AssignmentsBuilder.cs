using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;


namespace Upolnicek.Builders
{
    internal class AssignmentsBuilder
    {
        private Action<Assignment> _assignmentButtonOnClick;

        public AssignmentsBuilder(Action<Assignment> method)
        {
            //Onlick method for assignment button
            _assignmentButtonOnClick = method;
        }

        public bool Build(IEnumerable<Assignment> assignments, StackPanel container)
        { 
            try {
                container.Children.Clear();

                foreach (var assignment in assignments)
                {
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

                        container.Children.Add(button);
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
