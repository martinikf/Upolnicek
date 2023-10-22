using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Upolnicek.Builders
{
    internal class ResultLogBuilder
    {
        private Brush _fontColor;

        public ResultLogBuilder(Brush fontColor)
        {
            _fontColor = fontColor;
        }

        public bool Build(StackPanel container, IEnumerable<string> files, string message)
        {
            try
            {
                container.Children.Clear();

                foreach (var file in files)
                {
                    container.Children.Add(new Label
                    {
                        Content = file,
                        FontSize = 10,
                        Foreground = _fontColor
                    });
                }

                container.Children.Insert(0, CreateTextBlock(message));

                return true;
            }
            catch
            {
                container.Children.Insert(0, CreateTextBlock("Soubory nemusely být odevzdány!"));
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
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }
    }
}
