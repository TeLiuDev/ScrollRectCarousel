using UnityEngine;

namespace com.te.liu.scrollrectcarousel
{
    /// <summary>
    /// The display item must implement this interface and return either the Height or Width preferably both for the carousel to
    /// calculate the number of display items it needs to instantiate
    /// 
    /// The prefab IScrollListTestLineItem is the example prefab 
    /// </summary>
    public interface IScrollRectCarouselLineItemView
    {
        // Returns the height and width of the line item this is used to calculate how tall our scroll area is and how many line item is needed for display
        float Height { get; }
        float Width { get; }
        
        // Returns the min width & height for calculating how many items we need to fill the scroll rect
        float MinHeight { get; }
        float MinWidth { get; }

        // Returns a reference to the gameObject
        GameObject GameObject { get; }

        UnityEngine.UI.Button Button { get; }

        // Returns a reference to the rect transform
        RectTransform RectTran { get; }

        // Must implement this to refresh the view of the line item
        void UpdateViewBaseOnModel(ScrollRectCarouselDataAbstract data);

        // For handling selecting & deselecting, need a silence way to do this to avoid unnessary visuals as we recyle items
        void SilentSelect();
        void SilentDeselect();

        // Get reference to the current data
        ScrollRectCarouselDataAbstract Data { get; }

        // Clean up
        void Clean();
    }
}


