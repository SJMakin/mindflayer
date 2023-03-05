using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace MindFlayer
{
    public class ScrollViewerAttachedProperties
    {
        public static readonly DependencyProperty ScrollToBottomOnChangeProperty = DependencyProperty.RegisterAttached(
            "ScrollToBottomOnChange", typeof(object), typeof(ScrollViewerAttachedProperties), new PropertyMetadata(default(ScrollViewer), OnScrollToBottomOnChangeChanged));

        private static void OnScrollToBottomOnChangeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var scrollViewer = dependencyObject as ScrollViewer;

            if (args.OldValue != null)
            {
                var coll = (INotifyCollectionChanged)args.OldValue;
                coll.CollectionChanged -= (sender, args) => SetScrollToBottomOnChange(sender, args, scrollViewer);
            }

            if (args.NewValue != null)
            {
                var coll = (ObservableCollection<ChatMessage>)args.NewValue;
                // Subscribe to CollectionChanged on the new collection
                coll.CollectionChanged += (sender, args) => SetScrollToBottomOnChange(sender, args, scrollViewer);
            }

            scrollViewer?.ScrollToBottom();
        }

        private static void SetScrollToBottomOnChange(object sender, NotifyCollectionChangedEventArgs e, ScrollViewer? scrollViewer)
        {
            scrollViewer?.ScrollToBottom();
        }

        public static void SetScrollToBottomOnChange(DependencyObject element, object value)
        {
            element.SetValue(ScrollToBottomOnChangeProperty, value);
        }

        public static object GetScrollToBottomOnChange(DependencyObject element)
        {
            return element.GetValue(ScrollToBottomOnChangeProperty);
        }
    }
}
