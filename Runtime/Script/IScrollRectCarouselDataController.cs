using System;
using System.Threading;
using System.Threading.Tasks;

namespace com.te.liu.scrollrectcarousel
{
    /// <summary>
    /// Data controller that creates a way to update the item by index in the data array the implemented function would look like this
    ///
    ///     public void UpdateLineItemView(int index, IScrollRectCarouselLineItemView lineItemView)
    ///     {
    ///         lineItemView.UpdateViewBaseOnModel(dataList[index]);
    ///     }
    ///     
    /// </summary>
    public interface IScrollRectCarouselDataController
    {
        /// <summary>
        /// A way to update the line item template base on an index
        /// </summary>
        /// <param name="index">index of the data you want to render</param>
        /// <param name="lineItemView">reference to the template view</param>
        void UpdateLineItemView(int index, IScrollRectCarouselLineItemView lineItemView);

        /// <summary>
        /// Returns the length of the data list
        /// </summary>
        /// <returns>length of the data list</returns>
        int GetDataListCount();

        /// <summary>
        /// Returns if the data is selected
        /// </summary>
        /// <param name="index">index of the data</param>
        /// <returns></returns>
        bool IsDataSelected(int index);

        /// <summary>
        /// Selects an item, should set the data's IsSelected to true
        /// </summary>
        /// <param name="index">index of the data</param>
        void SetDataSelected(int index, bool newIsSelected);

        /// <summary>
        /// Clicks a item
        /// </summary>
        /// <param name="index">index of the data</param>
        void OnItemClicked(int index, IScrollRectCarouselLineItemView lineItemView);

        /// <summary>
        /// When an item comes into view and scroll velocity falls below a threshold and and past a minimal onscreen time
        /// </summary>
        /// <param name="index">The data index</param>
        /// <param name="GetCarouselItemViewByIndex">This function will return you the up to date lineItemView interface base on your index</param>
        /// <param name="token">A cancellation token to handle when the operation is cancelled when user scroll past the index</param>
        // THIS SHOULD BE AN ASYNC IMPLEMENTATION OR ELSE YOU CAN JUST USE THE UpdateLineItemView for updating
        void ItemViewAsyncUpdateOperation(int index, Func<int, IScrollRectCarouselLineItemView> GetCarouselItemViewByIndex, CancellationToken token);

        /// <summary>
        /// Clean up the scrollRect Carousel
        /// </summary>
        void Clean();
    }
}