using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace MindFlayer;

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

        if (args.NewValue != null && args.NewValue is INotifyCollectionChanged c)
        {
            c.CollectionChanged += (sender, args) => SetScrollToBottomOnChange(sender, args, scrollViewer);
        }

        GoToEnd(scrollViewer);
    }

    public void AttachCollectionChangedEventHandler(object observableCollection, NotifyCollectionChangedEventHandler handler)
    {
        var collectionChangedEventInfo = observableCollection.GetType().GetEvent("CollectionChanged");
        var delegateHandler = Delegate.CreateDelegate(collectionChangedEventInfo.EventHandlerType, handler.Target, handler.Method);
        collectionChangedEventInfo.AddEventHandler(observableCollection, delegateHandler);
    }

    private static void SetScrollToBottomOnChange(object? sender, NotifyCollectionChangedEventArgs e, ScrollViewer? scrollViewer)
    {
        GoToEnd(scrollViewer);
    }

    private static void GoToEnd(ScrollViewer? scrollViewer)
    {
        if (scrollViewer == null) return;
        
        scrollViewer.ScrollToRightEnd();
        scrollViewer.ScrollToBottom();
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
