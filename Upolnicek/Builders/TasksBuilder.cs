using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using Upolnicek.Data;

namespace Upolnicek.Builders
{
    internal class TasksBuilder : IBuilder
    {
        private Action<CourseTask> _taskButtonOnClick;
        private StackPanel _container;
        private IEnumerable<CourseTasks> _courseTasks;
        private Brush _fontColor;


        public TasksBuilder(StackPanel container, IEnumerable<CourseTasks> courseTasks, Action<CourseTask> method, Brush fontColor)
        {
            _taskButtonOnClick = method;
            _container = container;
            _courseTasks = courseTasks;
            _fontColor = fontColor;
        }

        public bool Build()
        {
            try
            {
                _container.Children.Clear();

                if(_courseTasks.Any() == false || _courseTasks.Select(x=>x.Tasks.Count()).Sum() == 0)
                {
                    var label = new Label()
                    {
                        Name= "TasksNoTasksLabel",
                        Content = "Žádné úkoly",
                        FontSize = 16,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Foreground = _fontColor
                    };

                    _container.Children.Add(label);
                    return true;
                }
               
                foreach (var cTask in _courseTasks)
                {
                    if (cTask.Tasks.Any() == false) 
                        continue;

                    try
                    {
                        var courseStackPanel = new StackPanel
                        {
                            Orientation = Orientation.Vertical,
                            Margin = new Thickness(2, 2, 2, 2)
                        };
                        _container.Children.Add(courseStackPanel);

                        var courseLabel = new Label
                        {
                            Content = cTask.course.Name,
                            FontSize = 14,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Center,
                            Foreground = _fontColor
                        };
                        courseStackPanel.Children.Add(courseLabel);

                        foreach (var task in cTask.Tasks) {
                            var button = new Button
                            {
                                Content = task.Name,
                                Margin = new Thickness(2, 2, 2, 2),
                                Padding = new Thickness(5, 2, 5, 2),
                                HorizontalContentAlignment = HorizontalAlignment.Left,
                                VerticalAlignment = VerticalAlignment.Center
                            };

                            button.Click += (s, e) =>
                            {
                                _taskButtonOnClick(task);
                            };

                            courseStackPanel.Children.Add(button);
                        }
                    }
                    catch
                    {
                        var label = new Label()
                        {
                            Content = $"Chyba: {cTask.course.Name}",
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
