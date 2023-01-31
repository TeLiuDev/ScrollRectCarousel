using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using com.te.liu.scrollrectcarousel;
using System.Threading.Tasks;
using System.Threading;
using TMPro;
using System;
using System.Collections.Generic;

public class AsyncFixedDimensionCarouselRowItemView : MonoBehaviour, IScrollRectCarouselLineItemView
{
    [SerializeField]
    private RectTransform rectTransform;

    [SerializeField]
    private float minWidth;
    [SerializeField]
    private float minHeight;

    [SerializeField]
    private List<AsyncFixedDimensionCarouselLineItemView> lineItemViews;

    private AsyncFixedDimensionRowTestData curData;

    public float Height { get { return minHeight; } }

    public float Width { get { return RectTran.sizeDelta.x; } }

    public float MinHeight { get { return minHeight; } }

    public float MinWidth { get { return minWidth; } }

    public GameObject GameObject { get { return this.gameObject; } }

    public Button Button { get { return null; } }

    public RectTransform RectTran { get { return rectTransform; } }

    public ScrollRectCarouselDataAbstract Data { get { return curData; } }

    public void SilentDeselect()
    {
        return;
    }

    public void SilentSelect()
    {
        return;
    }

    public void UpdateViewBaseOnModel(ScrollRectCarouselDataAbstract data)
    {
        curData = data as AsyncFixedDimensionRowTestData;

        for (int i = 0; i < curData.thumbnailItems.Length; i++)
        {
            if (curData.thumbnailItems[i] != null)
            {
                // If we downloaded the texture already then hide the loading text
                bool doLoading = curData.thumbnailItems[i].thumbnail == null;
                lineItemViews[i].UpdateView(curData.thumbnailItems[i].title, curData.thumbnailItems[i].thumbnail, false, doLoading, curData.thumbnailItems[i].waitingForLoad);
            }
            else
            {
                lineItemViews[i].UpdateView(null, null, true);
            }
            
        }
    }

    public void Clean() {

       for (int i = 0; i < curData.thumbnailItems.Length; i++)
        {
            if (curData.thumbnailItems[i] != null)
            {
                lineItemViews[i].ReleaseThumbnail();
            }
        }
    }
}
