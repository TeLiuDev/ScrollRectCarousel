using System.Collections.Generic;
using UnityEngine;
using com.te.liu.scrollrectcarousel;
using System;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine.Networking;

[Serializable]
public class AsyncFixedDimensionRowTestData : ScrollRectCarouselDataAbstract
{
    public AsyncFixedDimensionItemTestData[] thumbnailItems = new AsyncFixedDimensionItemTestData[4];

    public void Clean()
    {
        foreach (AsyncFixedDimensionItemTestData curData in thumbnailItems)
        {
            if (curData != null)
            {
                curData.thumbnail = null;
            }            
        }
    }
}

public class AsyncFixedDimensionItemTestData
{
    public string title;
    public Texture thumbnail;
    public string thumbnailURL;
    public bool waitingForLoad;

    public AsyncFixedDimensionItemTestData(string newTitle, Texture newThumbnail, string newThumbURL)
    {
        title = newTitle;
        thumbnail = newThumbnail;
        thumbnailURL = newThumbURL;
        waitingForLoad = false;
    }


}

/// <summary>
/// A sample data controller to with a simple definition of a testData and a List of test data.
/// Open TeAsyncFixedDimensionScrollListTest scene and see that there are 3 buttons, add/subtract items and refresh view for if you change 
/// ScrollRect setting
/// 
/// The data list can be defined in the editor inspector or via UI button or code by calling Add/Subtract to list
/// </summary>
public class AsyncFixedDimensionCarouselController : MonoBehaviour, IScrollRectCarouselDataController
{
    [SerializeField]
    private ScrollRectCarouselItemList listVert;

    [SerializeField]
    private Animator animator;

    private static int ShrinkVertAnim = Animator.StringToHash("ShrinkVert");
    private static int ShrinkHorzAnim = Animator.StringToHash("ShrinkHorz");

    [SerializeField]
    private List<AsyncFixedDimensionRowTestData> dataList;

    [SerializeField]
    private TextAsset rawPayload;

    [SerializeField]
    private GameObject LineItemTemplate;

    private AsyncFixedDimensionPayload payload;
    private List<AsyncFixedDimensionActivityPayload> activity_info;

    private int finishedDownloadCalls = 0;
    private int startedDownloadCalls = 0;
    private int cancelledDownloadCalls = 0;

    private void Awake()
    {
        listVert.SetLineItemTemplate(CreateLineItem);
        
        payload = JsonUtility.FromJson<AsyncFixedDimensionPayload>(rawPayload.text);
        activity_info = payload.activity_info;

        if (dataList == null)
        {
            dataList = new List<AsyncFixedDimensionRowTestData>();
        }

        if (dataList != null)
        {
            int numRowsNeeded = payload.activity_info.Count / 4;

            if (payload.activity_info.Count % 4 > 0)
            {
                numRowsNeeded++;
            }

            
            for (int i = 0; i < numRowsNeeded; i++)
            {
                int curRowItem = i * 4;
                
                AsyncFixedDimensionRowTestData newData = new AsyncFixedDimensionRowTestData();

                for (int k = 0; k < newData.thumbnailItems.Length; k++)
                {
                    if (curRowItem + k < activity_info.Count)
                    {
                        newData.thumbnailItems[k] = new AsyncFixedDimensionItemTestData(activity_info[curRowItem + k].icontext, null, activity_info[curRowItem + k].icon_url);
                    }
                    else
                    {
                        newData.thumbnailItems[k] = null;
                    }
                }

                // Important to add the data
                newData.index = i;
                dataList.Add(newData);
            }
        }
    }

    private GameObject CreateLineItem(Transform parent)
    {
        return Instantiate(LineItemTemplate, parent);
    }

    private void OnDestroy()
    {
        Clean();
    }
    public void Clean() 
    { 
        foreach (AsyncFixedDimensionRowTestData curData in dataList)
        {
            curData.Clean();
        }

        listVert.Clean();

        Resources.UnloadUnusedAssets();
    }

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
    }

    public void RefreshList()
    {
        listVert.InitCarousel(dataList.Count);
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
        RefreshList();
    }

    // When an item comes into view and scroll velocity falls below a threshold and and past a minimal onscreen time
    public async void ItemViewAsyncUpdateOperation(int index, Func<int, IScrollRectCarouselLineItemView> GetCarouselItemViewByIndex, CancellationToken token)
    {
        // Debug.Log("----------------------- ItemViewAsyncUpdateOperation enter " + index);        
        
        IScrollRectCarouselLineItemView lineItemView = GetCarouselItemViewByIndex(index);
        if (lineItemView != null)
        {
            foreach (AsyncFixedDimensionItemTestData curItemData in dataList[index].thumbnailItems)
            {
                if (curItemData != null)
                {
                    curItemData.waitingForLoad = true;
                }                
            }

            lineItemView.UpdateViewBaseOnModel(dataList[index]);
        }

        // Register cancellation debug
        token.Register(() =>
        {
            cancelledDownloadCalls++;
            foreach (AsyncFixedDimensionItemTestData curItemData in dataList[index].thumbnailItems)
            {
                if (curItemData != null)
                {
                    curItemData.waitingForLoad = false;
                }
            }
            //Debug.Log("------------ cancelled DL calls " + cancelledDownloadCalls);
            //Debug.Log("------------ cancelling download images for row " + index);
        });

        await DoImageDownloaded(index, token);

        lineItemView = GetCarouselItemViewByIndex(index);
        if (lineItemView != null)
        {
            lineItemView.UpdateViewBaseOnModel(dataList[index]);
        }

    }

    private async Task<Texture> GetImage(string url, CancellationToken token)
    {
        if (url == null)
        {
            return null;
        }

        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        UnityWebRequestAsyncOperation getTextureOperation = www.SendWebRequest();

        startedDownloadCalls++;
        //Debug.Log("------------ started DL calls " + startedDownloadCalls);

        while (!getTextureOperation.isDone)
        {
            if (token.IsCancellationRequested == true)
            {
                return null;
            }

            await Task.Yield();
        }

        finishedDownloadCalls++;
        //Debug.Log("------------ finished DL calls " + finishedDownloadCalls);

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
            return null;
        }
        else
        {
            return ((DownloadHandlerTexture)www.downloadHandler).texture;
        }
    }

    private async Task DoImageDownloaded(int index, CancellationToken token)
    {
        int imageIndex = 4 * index;
        
        // Group all the image downloading together
        foreach (AsyncFixedDimensionItemTestData curRowData in dataList[index].thumbnailItems)
        {
            // If we haven't downloaded the image download it
            if (curRowData != null && curRowData.thumbnail == null)
            {                
                curRowData.thumbnail = await GetImage(curRowData.thumbnailURL, token);
                imageIndex++;
            }
        }
    }
}
