using System;
using System.Windows;
using System.Windows.Controls;

namespace NextGen.Controls
{
    public class NxgScrollItems
    {
        public static int itemScrollIndex = -1;

        /// <summary>
        ///  Method For Scroll items Next based on given Count
        /// </summary>
        /// <param name="listBox"></param>
        /// <param name="prevButton"></param>
        /// <param name="nextButton"></param>
        public static void ScrollNext(ListBox listBox, int _itemScrollIndex, int itemsToBeScrolled, UIElement prevButton, UIElement nextButton)
        {
            try
            {
                if (itemScrollIndex == -1)
                {
                    itemScrollIndex = itemScrollIndex + (2 * itemsToBeScrolled);
                }
                else
                {
                    itemScrollIndex = itemScrollIndex % itemsToBeScrolled != 0 ? itemScrollIndex + itemsToBeScrolled : itemScrollIndex + ((2 * itemsToBeScrolled) - 1);
                }

                prevButton.Visibility = Visibility.Visible;
                nextButton.Visibility = Visibility.Visible;

                if (itemScrollIndex != -1)
                {
                    if (itemScrollIndex >= listBox.Items.Count - 1)
                    {
                        itemScrollIndex = listBox.Items.Count - 1;
                        nextButton.Visibility = Visibility.Collapsed;
                    }
                    listBox.ScrollIntoView(listBox.Items[itemScrollIndex]);
                }

                if (itemScrollIndex <= (itemsToBeScrolled - 1))
                    prevButton.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex) { ex.InsertException(); }
        }

        /// <summary>
        /// Method For Scroll items Previous based on given Count
        /// </summary>
        /// <param name="listBox"></param>
        /// <param name="prevButton"></param>
        /// <param name="nextButton"></param>
        public static void ScrollPrevious(ListBox listBox, int _itemScrollIndex, int itemsToBeScrolled, UIElement prevButton, UIElement nextButton)
        {
            try
            {
                if (itemScrollIndex == listBox.Items.Count - 1)
                {
                    itemScrollIndex = itemScrollIndex - (((listBox.Items.Count - 1) % itemsToBeScrolled) + itemsToBeScrolled);
                }
                else
                {
                    itemScrollIndex = (itemScrollIndex + 1) % itemsToBeScrolled == 0 ? itemScrollIndex - ((2 * itemsToBeScrolled) - 1) : itemScrollIndex - (itemsToBeScrolled - 1);
                }

                if (itemScrollIndex <= -1)
                    itemScrollIndex = 0;

                if (itemScrollIndex != -1)
                    listBox.ScrollIntoView(listBox.Items[itemScrollIndex]);

                nextButton.Visibility = Visibility.Visible;
                prevButton.Visibility = Visibility.Visible;

                if (itemScrollIndex == 0)
                {
                    prevButton.Visibility = Visibility.Collapsed;
                    itemScrollIndex = -1;
                }
            }
            catch (Exception ex) { ex.InsertException(); }
        }

        /// <summary>
        /// To scroll next single item
        /// </summary>
        /// <param name="myListbox"></param>
        /// <param name="previous"></param>
        /// <param name="next"></param>
        public static void ScrollNext(ListBox myListbox, Canvas previous, Canvas next)
        {
            if (myListbox.SelectedIndex == -1)
                myListbox.SelectedIndex = 0;
            myListbox.SelectedIndex = myListbox.SelectedIndex + 1;
            myListbox.ScrollIntoView(myListbox.SelectedItem);
            previous.Visibility = Visibility.Visible;
            if (myListbox.SelectedIndex == myListbox.Items.Count - 1)
                next.Visibility = Visibility.Hidden;
            if (myListbox.Items.Count == 1)
            {
                previous.Visibility = Visibility.Hidden;
                next.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// To scroll previous single item
        /// </summary>
        /// <param name="myListbox"></param>
        /// <param name="previous"></param>
        /// <param name="next"></param>
        public static void ScrollPrevious(ListBox myListbox, Canvas previous, Canvas next)
        {
            if (myListbox.SelectedIndex > 0)
            {
                myListbox.SelectedIndex = myListbox.SelectedIndex - 1;
                myListbox.ScrollIntoView(myListbox.SelectedItem);
            }
            next.Visibility = Visibility.Visible;
            if (myListbox.SelectedIndex < 1)
                previous.Visibility = Visibility.Hidden;
            if (myListbox.Items.Count == 1)
            {
                previous.Visibility = Visibility.Hidden;
                next.Visibility = Visibility.Hidden;
            }
        }
    }
}
