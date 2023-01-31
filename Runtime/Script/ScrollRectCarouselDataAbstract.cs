namespace com.te.liu.scrollrectcarousel
{
    /// <summary>
    /// The line item data must inherit fromt this abstract to give a clean way to update the view of the item
    /// </summary>
    public abstract class ScrollRectCarouselDataAbstract
    {
        // For holding the index in the data list
        public int index;

        // If the item is selected
        public bool isSelected;
    }
}