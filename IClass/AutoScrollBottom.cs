﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RFID.Utility.IClass
{
    public static class AutoScrollBottom
    {
        private static ResourceManager stringManager = new ResourceManager("en-US", Assembly.GetExecutingAssembly());
        static readonly Dictionary<ListBox, Capture> Associations =
           new Dictionary<ListBox, Capture>();

        public static bool GetScrollOnNewItem(DependencyObject objj)
        {
            if (objj == null)
                throw new ArgumentNullException(stringManager.GetString("DependencyObject argument is null", CultureInfo.CurrentCulture));
            return (bool)objj.GetValue(ScrollOnNewItemProperty);
        }

        public static void SetScrollOnNewItem(DependencyObject objj, bool value)
        {
            if (objj == null)
                throw new ArgumentNullException(stringManager.GetString("DependencyObject argument is null", CultureInfo.CurrentCulture));
            objj.SetValue(ScrollOnNewItemProperty, value);
        }

        public static readonly DependencyProperty ScrollOnNewItemProperty =
        DependencyProperty.RegisterAttached(
            "ScrollOnNewItem",
            typeof(bool),
            typeof(AutoScrollBottom),
            new UIPropertyMetadata(false, OnScrollOnNewItemChanged));

        public static void OnScrollOnNewItemChanged(DependencyObject d,DependencyPropertyChangedEventArgs e) {
            if (!(d is ListBox listBox)) return;
            bool oldValue = (bool)e.OldValue, newValue = (bool)e.NewValue;
            if (newValue == oldValue) return;
            if (newValue)
            {
                listBox.Loaded += ListBox_Loaded;
                listBox.Unloaded += ListBox_Unloaded;
                var itemsSourcePropertyDescriptor = TypeDescriptor.GetProperties(listBox)["ItemsSource"];
                itemsSourcePropertyDescriptor.AddValueChanged(listBox, ListBox_ItemsSourceChanged);
            }
            else
            {
                listBox.Loaded -= ListBox_Loaded;
                listBox.Unloaded -= ListBox_Unloaded;
                if (Associations.ContainsKey(listBox))
                    Associations[listBox].Dispose();
                var itemsSourcePropertyDescriptor = TypeDescriptor.GetProperties(listBox)["ItemsSource"];
                itemsSourcePropertyDescriptor.RemoveValueChanged(listBox, ListBox_ItemsSourceChanged);
            }
        }

        private static void ListBox_ItemsSourceChanged(object sender, EventArgs e)
        {
            var listBox = (ListBox)sender;
            if (Associations.ContainsKey(listBox))
                Associations[listBox].Dispose();
            Associations[listBox] = new Capture(listBox);
        }

        static void ListBox_Unloaded(object sender, RoutedEventArgs e)
        {
            var listBox = (ListBox)sender;
            if (Associations.ContainsKey(listBox))
                Associations[listBox].Dispose();
            listBox.Unloaded -= ListBox_Unloaded;
        }

        static void ListBox_Loaded(object sender, RoutedEventArgs e)
        {
            var listBox = (ListBox)sender;
            if (!(listBox.Items is INotifyCollectionChanged incc)) return;
            listBox.Loaded -= ListBox_Loaded;
            Associations[listBox] = new Capture(listBox);
        }

        class Capture : IDisposable
        {
            private readonly ListBox listBox;
            private readonly INotifyCollectionChanged incc;

            public Capture(ListBox listBox)
            {
                this.listBox = listBox;
                incc = listBox.ItemsSource as INotifyCollectionChanged;
                if (incc != null)
                {
                    incc.CollectionChanged += Incc_CollectionChanged;
                }
            }

            void Incc_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    listBox.ScrollIntoView(e.NewItems[0]);
                    listBox.SelectedItem = e.NewItems[0];
                }
            }

            public void Dispose()
            {
                if (incc != null)
                    incc.CollectionChanged -= Incc_CollectionChanged;
            }
        }






        public static bool GetAutoScroll(DependencyObject objj)
        {
            if (objj == null)
                throw new ArgumentNullException(stringManager.GetString("DependencyObject argument is null", CultureInfo.CurrentCulture));
            return (bool)objj.GetValue(AutoScrollProperty);
        }

        public static void SetAutoScroll(DependencyObject objj, bool value)
        {
            if (objj == null)
                throw new ArgumentNullException(stringManager.GetString("DependencyObject argument is null", CultureInfo.CurrentCulture));
            objj.SetValue(AutoScrollProperty, value);
        }

        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.RegisterAttached(
                "AutoScroll", 
                typeof(bool), 
                typeof(AutoScrollBottom), 
                new PropertyMetadata(false, AutoScrollPropertyChanged));

        private static void AutoScrollPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            if (d is ScrollViewer scrollViewer && (bool)e.NewValue)
            {
                scrollViewer.ScrollToBottom();
            }
        }
    }
}
