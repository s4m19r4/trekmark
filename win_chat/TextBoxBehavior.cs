using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace win_chat
{
    public static class TextBoxBehavior
    {
        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.RegisterAttached(
                "AutoScroll", typeof(bool), typeof(TextBoxBehavior),
                new PropertyMetadata(false, OnAutoScrollChanged));

        public static void SetAutoScroll(DependencyObject obj, bool value) =>
            obj.SetValue(AutoScrollProperty, value);
        public static bool GetAutoScroll(DependencyObject obj) =>
            (bool)obj.GetValue(AutoScrollProperty);

        private static void OnAutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox tb && (bool)e.NewValue)
            {
                tb.TextChanged += (s, ev) =>
                {
                    tb.CaretIndex = tb.Text.Length;
                    tb.ScrollToEnd();
                };
            }
        }
    }
}
