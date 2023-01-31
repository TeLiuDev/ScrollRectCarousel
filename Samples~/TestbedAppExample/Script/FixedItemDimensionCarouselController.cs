using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using com.te.liu.scrollrectcarousel;
using System;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Threading;

/// <summary>
/// A sample data controller to with a simple definition of a testData and a List of test data.
/// Open TeFixedDimensionScrollListTest scene and see that there are 3 buttons, add/subtract items and refresh view for if you change 
/// ScrollRect setting
/// 
/// The data list can be defined in the editor inspector or via UI button or code by calling Add/Subtract to list
/// </summary>

public class FixedItemDimensionCarouselController : MonoBehaviour, IScrollRectCarouselDataController
{
    [SerializeField]
    private ScrollRectCarouselItemList listVert;
    [SerializeField]
    private ScrollRectCarouselItemList listHorz;

    [SerializeField]
    private Animator animator;

    private static int ShrinkVertAnim = Animator.StringToHash("ShrinkVert");
    private static int ShrinkHorzAnim = Animator.StringToHash("ShrinkHorz");

    [SerializeField]
    private List<testData> dataList;

    private void Awake()
    {
        if (dataList == null)
        {
            dataList = new List<testData>();
        }

        if (dataList != null)
        {
            for (int i = 0; i < 100; i++)
            {
                AddNewDataItem();
            }
        }
    }

    public void Clean() { }

    public bool IsDataSelected(int index)
    {
        return dataList[index].isSelected;
    }

    public void SetDataSelected(int index, bool newIsSelected)
    {
        dataList[index].isSelected = newIsSelected;
    }

    public void ToggleDataSelected(int index)
    {
        dataList[index].isSelected = !dataList[index].isSelected;
    }

    public void OnItemClicked(int index, IScrollRectCarouselLineItemView lineItemView)
    {
        ToggleDataSelected(index);
        dataList[index].doClickedTransition = true;
        UpdateLineItemView(index, lineItemView);
    }

    public int GetDataListCount()
    {
        return dataList.Count;
    }

    public void UpdateLineItemView(int index, IScrollRectCarouselLineItemView lineItemView)
    {
        lineItemView.UpdateViewBaseOnModel(dataList[index]);
    }

    private void Start()
    {
        listVert.InitCarousel(dataList.Count);
        listHorz.InitCarousel(dataList.Count);
    }

    private void AddNewDataItem()
    {
        testData newData = new testData();
        newData.count = dataList.Count + 1;
        newData.description = UnityEngine.Random.insideUnitCircle.ToString();
        newData.index = newData.count - 1;
        newData.asyncOperationState = 0;

        newData.description += "\n Fixed dimension \n Line item";

        dataList.Add(newData);
    }

    public void AddToList()
    {
        AddNewDataItem();

        listVert.InitCarousel(dataList.Count, false);
        listHorz.InitCarousel(dataList.Count, false);
    }

    public void SubtractFromList()
    {
        if (dataList.Count > 0)
        {
            dataList.RemoveAt(dataList.Count - 1);
            RefreshList();
        }
    }

    public void RefreshList()
    {
        listVert.InitCarousel(dataList.Count);
        listHorz.InitCarousel(dataList.Count);
    }

    public void ShrinkVert()
    {
        animator.ResetTrigger(ShrinkVertAnim);
        animator.SetTrigger(ShrinkVertAnim);
    }

    public void ShrinkHorz()
    {
        animator.ResetTrigger(ShrinkHorzAnim);
        animator.SetTrigger(ShrinkHorzAnim);
    }

    public void ToggleReverse()
    {
        listVert.ReverseOrder = !listVert.ReverseOrder;
        listHorz.ReverseOrder = !listHorz.ReverseOrder;
        RefreshList();
    }

    // When an item comes into view and scroll velocity falls below a threshold and and past a minimal onscreen time
    public async void ItemViewAsyncUpdateOperation(int index, Func<int, IScrollRectCarouselLineItemView> GetCarouselItemViewByIndex, CancellationToken token)
    {
        // Debug.Log("----------------------- ItemViewAsyncUpdateOperation enter " + index);        
        dataList[index].asyncOperationState = 1;

        IScrollRectCarouselLineItemView lineItemView = GetCarouselItemViewByIndex(index);
        if (lineItemView != null)
        {
            lineItemView.UpdateViewBaseOnModel(dataList[index]);
        }

        // Register our cancellation
        token.Register(() => {
            //Debug.Log("----------------------- ItemViewAsyncUpdateOperation cancel " + index);
            dataList[index].asyncOperationState = 0;
        });

        await Task.Delay(TimeSpan.FromSeconds(5f), token);

        // Async task completed
        //Debug.Log("----------------------- ItemViewAsyncUpdateOperation success " + index);
        dataList[index].asyncOperationState = 2;

        // After async operation the lineItemView might have moved get the new one
        lineItemView = GetCarouselItemViewByIndex(index);
        if (lineItemView != null)
        {
            lineItemView.UpdateViewBaseOnModel(dataList[index]);
        }
    }
}
