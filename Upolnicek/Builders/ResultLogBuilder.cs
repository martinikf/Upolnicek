using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Upolnicek.Builders
{
    internal class ResultLogBuilder : IBuilder
    {
        private StackPanel _container;
        private IEnumerable<string> _files;
        private string _message;
        private Brush _fontColor;

        public ResultLogBuilder(StackPanel container, IEnumerable<string> files, string message, Brush fontColor)
        {
            _fontColor = fontColor;
            _container = container;
            _files = files;
            _message = message;
        }

        public bool Build()
        {
            try
            {
                _container.Children.Clear();

                foreach (var file in _files)
                {
                    _container.Children.Add(new Label
                    {
                        Content = file,
                        FontSize = 10,
                        Foreground = _fontColor
                    });
                }

                _container.Children.Insert(0, CreateTextBlock(_message));

                return true;
            }
            catch
            {
                _container.Children.Insert(0, CreateTextBlock("Soubory nemusely být odevzdány!"));
                return false;
            }
        }

        private TextBlock CreateTextBlock(string text)
        {
            return new TextBlock()
            {
                Text = text,   
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                Foreground = _fontColor,
                HorizontalAlignment = HorizontalAlignment.Left
            };
        }
    }
}
