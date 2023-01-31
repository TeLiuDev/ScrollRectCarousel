using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Threading;
using System;
using UnityEngine.Events;

namespace com.te.liu.scrollrectcarousel
{
    /// <summary>
    /// The ScrollRectCarousel is a UGUI scrollRect companion that displays the data in an efficient manner.
    /// Instead of instantiating the total number of your data, the carousel only instantiate the number needed to
    /// visually cover your scrollRect display area.  Then it carousels through the data items for display.
    /// 
    /// This is the core of the carousel scroll rect and it's corresponding example prefab CarouselScrollRect.  
    /// This is used in conjunction with the UGUI scrollRect and must use various ScrollRectCarousel interfaces and
    /// the base dataAbstract class
    /// 
    /// The CarouselScrollRect uses the UGUI ScrollRect along with a ScrollContent gameObject and ItemContainer GameObject 
    /// to facilitate the drag & scroll then the CarouselItemList container to display the carousel items
    /// 
    /// </summary>
    public class ScrollRectCarouselItemList : MonoBehaviour
    {
        public enum ScrollDirection { Undefined = -1, None, Vertical, Horizontal, VertAndHorz}

        // Template to the item to display in the scroll list template must implement IScrollRectCarouselLineItemView
        [SerializeField]
        private GameObject lineItemTemplate;

        private IScrollRectCarouselLineItemView lineItemInterface;
        public IScrollRectCarouselLineItemView LineItemTemplate { get { return lineItemInterface; } }
        private GameObject lineItemTemplateCopy;

        // Link to the data controller and must implement IScrollRectCarouselDataController
        [SerializeField]
        private GameObject dataControllerObj;

        private IScrollRectCarouselDataController dataController;

        // Link to the data controller can be either linked in editor or use this set function to do it in code
        public void SetDataController(IScrollRectCarouselDataController controller)
        {
            dataController = controller;
        }

        // The UGUI scrollRect
        [SerializeField]
        private ScrollRect scrollRect;

        // This contains the instantiated line items for the carousel
        [SerializeField]
        private RectTransform carouselListContainer;

        // Spacing between line items
        [SerializeField]
        private float lineSpacing;

        // Display the list in reverse order
        [SerializeField]
        private bool reverseOrder;
        public bool ReverseOrder { get { return reverseOrder; }  set { reverseOrder = value; } }

        // If the items are fixed sized
        [SerializeField]
        private bool fixedLineItemDimension;
        public bool FixedLineItemDimension { get { return fixedLineItemDimension; } set { fixedLineItemDimension = value; } }

        // Pooling mechanism for the line items
        private List<IScrollRectCarouselLineItemView> visualCarouselActive = new List<IScrollRectCarouselLineItemView>();
        private List<IScrollRectCarouselLineItemView> visualCarouselPool = new List<IScrollRectCarouselLineItemView>();
        
        // Internal values for scrolling through the scroll rect and what index range to display
        private int numLineItems;
        private int lineItemsNeededForViewport;
        private float totalScrollContentHeight;
        private float lineItemHeight;
        private float itemHeight;

        private float totalScrollContentWidth;
        private float lineItemWidth;
        private float itemWidth;

        private int firstViewPortItemIndex = -1;
        private int lastViewPortItemIndex = -1;

        private float currentY = 0f;
        private float currentX = 0f;

        private Vector2 curViewPortSize;

        private ScrollDirection scrollDirection = ScrollDirection.Vertical;

        private List<Vector2> cumulativeItemPosition = new List<Vector2>();

        // For async loading and cancellation
        private Dictionary<int, CancellationTokenSource> activeAsyncUpdateOperation = new Dictionary<int, CancellationTokenSource>();

        [SerializeField]
        private bool useAsyncOperationUpdate;

        [SerializeField, Tooltip("Item needs to be onscreen fo long than this threshold(sec) before we do async update on the item"), Min(0f)]
        private float asyncOperationOnScreenDurationTresh = 1f;

        // For creating template in runtime
        private Func<Transform, GameObject> createLineItemFunc;

        private void Awake()
        {
            InitializeTemplateCreation();
            if (dataControllerObj != null)
            {
                dataController = dataControllerObj.GetComponent<IScrollRectCarouselDataController>();
            }
            
            scrollRect.onValueChanged.AddListener(OnValueChanged);
            Vector2 newViewPortSize = new Vector2(scrollRect.viewport.rect.width, scrollRect.viewport.rect.height);
            CheckScrollDirection(newViewPortSize);
        }

        private void ResetInternalValues()
        {
            numLineItems = lineItemsNeededForViewport = 0;
            totalScrollContentHeight = lineItemHeight = itemHeight = totalScrollContentWidth = lineItemWidth = itemWidth = currentY = currentX = 0.0f;
            firstViewPortItemIndex = lastViewPortItemIndex = -1;
            curViewPortSize = Vector2.zero;
            cumulativeItemPosition.Clear();
        }

        private void InitializeTemplateCreation()
        {
            if (createLineItemFunc != null || lineItemTemplate == null)
            {
                return;
            }
                
            SetLineItemTemplate((parentTran) => {
                if (parentTran == null)
                {
                    return Instantiate(lineItemTemplate);
                }
                else
                {
                    return Instantiate(lineItemTemplate, parentTran);
                }
            });
        }

        public void SetLineItemTemplate(Func<Transform, GameObject> newCreateLineItemFunc)
        {
            createLineItemFunc = newCreateLineItemFunc;
            lineItemTemplateCopy = createLineItemFunc(null);
            lineItemInterface = lineItemTemplateCopy.GetComponent<IScrollRectCarouselLineItemView>();
            lineItemTemplateCopy.SetActive(false);
        }

        private void OnDestroy()
        {
            Clean();
            scrollRect.onValueChanged.RemoveListener(new UnityAction<Vector2>(this.OnValueChanged));
        }

        public void Clean()
        {
            ClearActiveAsyncOperations();

            if (dataControllerObj == null)
            {
                dataController = null;
            }
                
            ClearActiveCarouselPool();

            foreach (IScrollRectCarouselLineItemView carouselLineItemView in visualCarouselPool)
            {
                if (carouselLineItemView.Button != null)
                {
                    carouselLineItemView.Button.onClick.RemoveAllListeners();
                }
                    
                carouselLineItemView.Clean();
                Destroy(carouselLineItemView.GameObject);
            }

            visualCarouselPool.Clear();
            createLineItemFunc = null;

            if (lineItemTemplateCopy != null)
            {
                Destroy(lineItemTemplateCopy);
                lineItemTemplateCopy = null;
            }

            ResetInternalValues();
        }


        private void Update()
        {
            Vector2 newViewPortSize = new Vector2(scrollRect.viewport.rect.width, scrollRect.viewport.rect.height);

            if (newViewPortSize != curViewPortSize)
            {
                CheckScrollDirection(newViewPortSize);
            }
        }

        private void CheckScrollDirection(Vector2 viewPortSize)
        {
            curViewPortSize = viewPortSize;

            int scrollDir = (int)ScrollDirection.None;

            if (scrollRect.vertical == true)
            {
                scrollDir = scrollDir | 1;
            }

            if (scrollRect.horizontal == true)
            {
                scrollDir = scrollDir | 1 << 1;
            }

            // Reset our scroll position if we are changing scroll direction
            if (scrollDirection != (ScrollDirection)scrollDir)
            {
                scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, curViewPortSize.y);
                scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, curViewPortSize.x);
                carouselListContainer.anchoredPosition = new Vector2(0, 0);
            }

            scrollDirection = (ScrollDirection)scrollDir;
        }

        private void ClearActiveCarouselPool()
        {
            foreach (IScrollRectCarouselLineItemView curItem in visualCarouselActive)
            {
                curItem.GameObject.SetActive(false);
                visualCarouselPool.Add(curItem);
            }

            visualCarouselActive.Clear();
        }

        public void ClearActiveAsyncOperations()
        {
            foreach (CancellationTokenSource curToken in activeAsyncUpdateOperation.Values)
            {
                if (curToken != null && curToken.Token.CanBeCanceled == true)
                {
                    curToken.Cancel();
                    curToken.Dispose();
                }
            }

            activeAsyncUpdateOperation.Clear();
        }

        private void CalcListDimension()
        {
            totalScrollContentHeight = 0f;
            totalScrollContentWidth = 0f;
            cumulativeItemPosition.Clear();
            
            int totalItems = dataController.GetDataListCount();

            for (int i = 0; i < totalItems ; i++)
            {
                // Store the position of our item
                cumulativeItemPosition.Add(new Vector2(totalScrollContentWidth, totalScrollContentHeight));

                if (fixedLineItemDimension == false || totalItems == 1)
                {
                    dataController.UpdateLineItemView(i, LineItemTemplate);
                }
                
                totalScrollContentHeight += LineItemTemplate.Height;
                totalScrollContentWidth += LineItemTemplate.Width;

                if (i < totalItems - 1)
                {
                    totalScrollContentHeight += lineSpacing;
                    totalScrollContentWidth += lineSpacing;
                }
            }
        }

        private void AppendToListDimension()
        {
            int currentItemCount = cumulativeItemPosition.Count;
            int totalItems = dataController.GetDataListCount();

            // If we are appending a new value we have to remember to add line spacing to the very last position stored
            if (currentItemCount > 0 && currentItemCount == totalItems - 1)
            {
                totalScrollContentHeight += lineSpacing;
                totalScrollContentWidth += lineSpacing;
            }

            for (int i = currentItemCount; i < totalItems; i++)
            {
                // Store the position of our item
                cumulativeItemPosition.Add(new Vector2(totalScrollContentWidth, totalScrollContentHeight));

                if (fixedLineItemDimension == false || totalItems == 1)
                {
                    dataController.UpdateLineItemView(i, LineItemTemplate);
                }
                
                totalScrollContentHeight += LineItemTemplate.Height;
                totalScrollContentWidth += LineItemTemplate.Width;

                if (i < totalItems - 1)
                {
                    totalScrollContentHeight += lineSpacing;
                    totalScrollContentWidth += lineSpacing;
                }
            }
        }

        /// <summary>
        /// This initialize the carousel scroll list and depending on number of data items and the 
        /// UGUI scroll rect's horizontal/vertical setting it'll initialize the item pool and scroll content sizing
        /// </summary>
        /// <param name="totalLineItem">Total numer of line times</param>
        /// <param name="recalcListDimension">If we want to recalculate the list dimension or just calcuate dimension from num of existing list items to new totalLineItem</param>
        public void InitCarousel(int totalLineItem, bool recalcListDimension = true)
        {
            InitializeTemplateCreation();

            numLineItems = totalLineItem;
            firstViewPortItemIndex = -1;
            lastViewPortItemIndex = -1;

            ClearActiveCarouselPool();
            ClearActiveAsyncOperations();

            if (recalcListDimension == true)
            {
                CalcListDimension();
            }
            else
            {
                AppendToListDimension();
            }
            

            if (totalScrollContentHeight <= 0 || totalScrollContentWidth <= 0)
            {
                return;
            }

            CheckScrollDirection(curViewPortSize);

            FindLineItemsNeeded();

            switch (scrollDirection)
            {
                case ScrollDirection.Vertical:
                    scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalScrollContentHeight);
                    break;

                case ScrollDirection.Horizontal:
                    scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, totalScrollContentWidth);
                    break;
            }

            GenerateLineItems(lineItemsNeededForViewport);

            // If we don't need to scroll position our list at the top
            if (lineItemsNeededForViewport == totalLineItem)
            {
                currentY = 0;
                currentX = 0;

                firstViewPortItemIndex = 0;
                lastViewPortItemIndex = lineItemsNeededForViewport - 1;

                if (reverseOrder == true)
                {
                    firstViewPortItemIndex = (numLineItems - 1) - firstViewPortItemIndex;
                    lastViewPortItemIndex = (numLineItems - 1) - lastViewPortItemIndex;
                }
            }

            if (lineItemsNeededForViewport == 0)
            {
                firstViewPortItemIndex = -1;
                lastViewPortItemIndex = -1;
            }

            UpdateItemListBaseOnPosition(currentX, currentY, true);
        }

        private void FindLineItemsNeeded()
        {
            // Use an average size to calculate how many items we need to be viewable at once
            itemHeight = lineItemInterface.MinHeight;
            itemWidth = lineItemInterface.MinWidth;

            // If rect tran is set to strech we'll use the parent's dimension
            if (itemWidth <= 0 && lineItemInterface.RectTran.anchorMin.x == 0 && lineItemInterface.RectTran.anchorMax.x == 1f)
            {
                itemWidth = scrollRect.viewport.rect.width;
            }

            if (itemHeight <= 0 && lineItemInterface.RectTran.anchorMin.y == 0 && lineItemInterface.RectTran.anchorMax.y == 1f)
            {
                itemHeight = scrollRect.viewport.rect.height;
            }

            // Adjust total content height by spacing and padding before assigning
            lineItemHeight = itemHeight + lineSpacing;
            lineItemWidth = itemWidth + lineSpacing;

            int maxLineItemsNeedForViewport = 0;

            switch (scrollDirection)
            {
                case ScrollDirection.Vertical:
                    maxLineItemsNeedForViewport = Mathf.CeilToInt(scrollRect.viewport.rect.height / lineItemHeight);
                    break;

                case ScrollDirection.Horizontal:
                    maxLineItemsNeedForViewport = Mathf.CeilToInt(scrollRect.viewport.rect.width / lineItemWidth);
                    break;
            }


            if (numLineItems > maxLineItemsNeedForViewport + 2)
            {
                lineItemsNeededForViewport = maxLineItemsNeedForViewport + 2;
            }
            else
            {
                lineItemsNeededForViewport = numLineItems;
            }
        }

        private void GenerateLineItems(int numOfItems)
        {
            for (int i = 0; i < numOfItems; i++)
            {
                IScrollRectCarouselLineItemView newLine;

                if (visualCarouselPool.Count > 0)
                {
                    int lastIndex = visualCarouselPool.Count - 1;
                    newLine = visualCarouselPool[lastIndex];
                    visualCarouselPool.RemoveAt(lastIndex);
                }
                else
                {
                    newLine = createLineItemFunc(carouselListContainer.transform).GetComponent<IScrollRectCarouselLineItemView>();

                    if (newLine.Button != null)
                    {
                        newLine.Button.onClick.AddListener(()=> { dataController.OnItemClicked(newLine.Data.index, newLine); });
                    }
                }

                // We position the item with top left pivot
                newLine.RectTran.pivot = new Vector2(0, 1);
                newLine.RectTran.anchoredPosition = new Vector2(0, 0);                
                newLine.GameObject.name = i.ToString();

                visualCarouselActive.Add(newLine);
            }

        }

        private void OnValueChanged(Vector2 value)
        {
            float yVal = value.y;

            // No responce to drag if the list is shorter than viewport length
            if (totalScrollContentHeight <= scrollRect.viewport.rect.height)
            {
                yVal = 1f;
            }

            float newY = (1f - yVal) * (totalScrollContentHeight - scrollRect.viewport.rect.height);

            float xVal = value.x;

            // No responce to drag if the list is shorter than viewport length
            if (totalScrollContentWidth <= scrollRect.viewport.rect.width)
            {
                xVal = 1f;
            }

            float newX = xVal * (totalScrollContentWidth - scrollRect.viewport.rect.width);

            float xPos = carouselListContainer.anchoredPosition.x;
            float yPos = carouselListContainer.anchoredPosition.y;

            switch (scrollDirection)
            {
                case ScrollDirection.Vertical:
                    yPos = newY;
                    break;

                case ScrollDirection.Horizontal:
                    xPos = newX;
                    break;
            }
            
            carouselListContainer.anchoredPosition = new Vector2(-xPos, yPos);

            UpdateItemListBaseOnPosition(xPos, yPos);

            currentY = yPos;
            currentX = xPos;

        }

        
        private bool RefreshIndex(float newX, float newY, bool forceUpdate = false)
        {
            int startIndex, endIndex;
            startIndex = endIndex = -1;

            int bestStartIndex = firstViewPortItemIndex;
            int increment = -1;

            if (reverseOrder == true)
            {
                increment = 1;
            }
            
            // First go from firstDrawIndex and go backward to find the appropriate start index
            for (int i = firstViewPortItemIndex; i >= 0 && i < numLineItems; i += increment)
            {
                float testVal = 0f;
                float valToUse = 0f;

                switch (scrollDirection)
                {
                    // CumulativeItemPosition only stores the start of the last item which is the start of the 2nd item in the reverse list
                    // The first item in the list is always 0,0
                    case ScrollDirection.Vertical:
                        if (reverseOrder == true)
                        {
                            testVal = GetReverseCumulativePos(i).y;
                        }
                        else
                        {
                            testVal = cumulativeItemPosition[i].y;
                        }
                        valToUse = newY;
                        break;

                    case ScrollDirection.Horizontal:
                        if (reverseOrder == true)
                        {
                            testVal = GetReverseCumulativePos(i).x;
                        }
                        else
                        {
                            testVal = cumulativeItemPosition[i].x;
                        }
                        valToUse = newX;
                        break;
                }

                if (valToUse > testVal)
                {
                    break;
                }
                else
                {                    
                    bestStartIndex = i + increment;
                }
            }

            bestStartIndex = Mathf.Clamp(bestStartIndex, 0, numLineItems - 1);

            increment = 1;

            if (reverseOrder == true)
            {
                increment = -1;
            }

            bool findEndIndex = false;
            
            for (int i = bestStartIndex; i >= 0 && i < numLineItems; i += increment)
            {
                float testVal = 0f;
                float nextTestVal = 0f;
                float valToUse = 0f;
                int testIndex = i;
                int nextIndex = i + increment;
                
                nextIndex = Mathf.Clamp(nextIndex, 0, numLineItems - 1);

                switch (scrollDirection)
                {
                    case ScrollDirection.Vertical:

                        if (reverseOrder == true)
                        {
                            testVal = GetReverseCumulativePos(testIndex).y;

                            if (nextIndex != testIndex)
                            {
                                nextTestVal = GetReverseCumulativePos(nextIndex).y;
                            }

                        }
                        else
                        {
                            testVal = cumulativeItemPosition[testIndex].y;
                            nextTestVal = cumulativeItemPosition[nextIndex].y;
                        }

                        if (nextIndex == i && dataController != null)
                        {
                            if (fixedLineItemDimension == false)
                            {
                                dataController.UpdateLineItemView(testIndex, lineItemInterface);
                            }
                            
                            nextTestVal += lineItemInterface.Height;
                        }

                        valToUse = newY;
                        if (findEndIndex == true)
                        {
                            valToUse += scrollRect.viewport.rect.height;
                        }
                        break;

                    case ScrollDirection.Horizontal:

                        if (reverseOrder == true)
                        {
                            testVal = GetReverseCumulativePos(testIndex).x;

                            if (nextIndex != testIndex)
                            {
                                nextTestVal = GetReverseCumulativePos(nextIndex).x;
                            }
                        }
                        else
                        {
                            testVal = cumulativeItemPosition[testIndex].x;
                            nextTestVal = cumulativeItemPosition[nextIndex].x;
                        }

                        if (nextIndex == testIndex && dataController != null)
                        {
                            if (fixedLineItemDimension == false)
                            {
                                dataController.UpdateLineItemView(testIndex, lineItemInterface);
                            }

                            nextTestVal += lineItemInterface.Width;
                        }

                        valToUse = newX;
                        if (findEndIndex == true)
                        {
                            valToUse += scrollRect.viewport.rect.width;
                        }
                        break;
                }

                if (valToUse >= testVal && valToUse <= nextTestVal)
                {
                    if (findEndIndex == false)
                    {
                        if (reverseOrder == true)
                        {
                            startIndex = testIndex + 1;
                        }
                        else
                        {
                            startIndex = i - 1;
                        }
                        
                        findEndIndex = true;
                    }
                    else
                    {
                        if (reverseOrder == true)
                        {
                            endIndex = testIndex - 2;
                        }
                        else
                        {
                            endIndex = i + 2;
                        }

                        break;
                    }
                }

            }

            if (reverseOrder == true)
            {
                if (endIndex < 0)
                {
                    endIndex = 0;
                }

                if (startIndex >= numLineItems || (numLineItems > 0 && startIndex < 0))
                {
                    startIndex = numLineItems - 1;
                }
            }
            else
            {
                if (startIndex < 0)
                {
                    startIndex = 0;
                }

                if (endIndex >= numLineItems || (numLineItems > 0 && endIndex < 0))
                {
                    endIndex = numLineItems - 1;
                }
            }

            //Debug.Log("--------------------- startIndex " + startIndex + " endIndex " + endIndex);

            if (startIndex != firstViewPortItemIndex || endIndex != lastViewPortItemIndex || forceUpdate == true)
            {
                firstViewPortItemIndex = startIndex;
                lastViewPortItemIndex = endIndex;
                return true;
            }

            return false;
        }

        private void CancelAsyncOperation(int index)
        {
            CancellationTokenSource targetToken;
            if (activeAsyncUpdateOperation.TryGetValue(index, out targetToken) == true && targetToken != null)
            {
                //Debug.Log("----------------- CancelAsyncOperation 1 index " + index);
                targetToken.Cancel();
                targetToken.Dispose();
                activeAsyncUpdateOperation[index] = null;
            }
        }

        private void UpdateItemListBaseOnPosition(float newX, float newY, bool forceUpdate = false)
        {
            if (numLineItems > 0 && RefreshIndex(newX, newY, forceUpdate) == true)
            {
                // Optimization turn off all object, update then turn back on
                foreach (IScrollRectCarouselLineItemView curItem in visualCarouselActive)
                {
                    curItem.GameObject.SetActive(false);
                    curItem.SilentDeselect();

                    if (useAsyncOperationUpdate == true && curItem.Data != null)
                    {
                        // If the item went outside of our display range then cancel its async operation
                        int curIndex = curItem.Data.index;

                        if (IsIndexOffScreen(curIndex) == true)
                        {
                            CancelAsyncOperation(curIndex);
                        }
                    }
                }

                int i = firstViewPortItemIndex;
                int increment = 1;

                if (reverseOrder == true)
                {
                    increment = -1;
                }

                int k = 0;

                for (k = 0; k < visualCarouselActive.Count; k++)
                {
                    // If we are outside of our range
                    if ((reverseOrder == true && i < lastViewPortItemIndex) || (reverseOrder == false && i > lastViewPortItemIndex))
                    {
                        break;
                    }
                    
                    IScrollRectCarouselLineItemView curLineItemView = visualCarouselActive[k];
                    RectTransform curObjectRectTran = curLineItemView.RectTran;

                    // Update the view
                    if (dataController != null)
                    {
                        // This will calculate the size for this particular data
                        dataController.UpdateLineItemView(i, curLineItemView);

                        if (dataController.IsDataSelected(i) == true)
                        {
                            curLineItemView.SilentSelect();
                        }

                        // If we are below the velocity threshold we can check for async update operation
                        if (useAsyncOperationUpdate == true)
                        {
                            WaitThenDoAsyncUpdate(i, curLineItemView, k);
                        }
                    }

                    // We position the item with top left pivot
                    float anchoredY = curObjectRectTran.anchoredPosition.y;
                    float anchoredX = curObjectRectTran.anchoredPosition.x;

                    switch (scrollDirection)
                    {
                        case ScrollDirection.Vertical:
                            if (reverseOrder == true)
                            {
                                anchoredY = GetReverseCumulativePos(i).y;
                            }
                            else
                            {
                                anchoredY = cumulativeItemPosition[i].y;
                            }
                            
                            break;

                        case ScrollDirection.Horizontal:
                            if (reverseOrder == true)
                            {
                                anchoredX = GetReverseCumulativePos(i).x;
                            }
                            else
                            {
                                anchoredX = cumulativeItemPosition[i].x;
                            }
                            
                            break;
                    }

                    curObjectRectTran.anchoredPosition = new Vector2(anchoredX, -anchoredY);
                    i += increment;
                }

                // Turn object back on at once to minimize panel recalculation onlyn turn back on the ones we rendered
                for (int j = 0; j < k; j++)
                {
                    visualCarouselActive[j].GameObject.SetActive(true);
                }

            }
        }

        private Vector2 GetReverseCumulativePos(int index)
        {
            // CumulativeItemPosition only stores the start of the last item which is the start of the 2nd item in the reverse list
            // The first item in the list is always 0,0
            Vector2 compare = Vector2.zero;

            if (index >= numLineItems - 1)
            {
                compare = new Vector2(totalScrollContentWidth, totalScrollContentHeight);
            }
            else
            {
                compare = new Vector2(cumulativeItemPosition[index + 1].x, cumulativeItemPosition[index + 1].y);

                // Since no line spacing on the first or last item need to adjust them
                compare.x -= lineSpacing;
                compare.y -= lineSpacing;
            }

            Vector2 result = Vector2.zero;

            result.x = totalScrollContentWidth - compare.x;
            result.y = totalScrollContentHeight - compare.y;

            return result;
        }

        private bool IsIndexOffScreen(int index)
        {
            if (reverseOrder == true)
            {
               return index < lastViewPortItemIndex || index > firstViewPortItemIndex;
            }
            else
            {
                return index > lastViewPortItemIndex || index < firstViewPortItemIndex;
            }
        }

        private IScrollRectCarouselLineItemView GetActiveLineViewByDataIndex(int dataIndex)
        {
            foreach (IScrollRectCarouselLineItemView curLineItemView in visualCarouselActive)
            {
                if (curLineItemView.Data.index == dataIndex)
                {
                    return curLineItemView;
                }
            }

            return null;
        }

        private async void WaitThenDoAsyncUpdate(int index, IScrollRectCarouselLineItemView targetLineItemView, int activeCarouselIndex)
        {
            // Wait for a threshold then check to see if still onscreen
            await Task.Delay(TimeSpan.FromSeconds(asyncOperationOnScreenDurationTresh));

            // If the lineitem is no longer onscreen or lineitem is holding a different index then abort
            if (dataController == null || IsIndexOffScreen(index) == true)
            {
                CancelAsyncOperation(index);
                return;
            }

            IScrollRectCarouselLineItemView resultLineItemView = targetLineItemView;

            // If our list scrolled and the carousel moved, see if the target is still in the active list
            if (targetLineItemView.Data.index != index)
            {
                resultLineItemView = GetActiveLineViewByDataIndex(index);

                if (resultLineItemView == null)
                {
                    // cancel the old 
                    CancelAsyncOperation(index);
                    return;
                }                
            }
            

            // Do async operation with cancellation and add cancellation to our tracking dictionary
            // First make sure we dispose of the existing tokenSource
            CancellationTokenSource targetToken;            
            if (activeAsyncUpdateOperation.TryGetValue(index, out targetToken) == true && targetToken != null)
            {
                targetToken.Dispose();
            }

            targetToken = new CancellationTokenSource();
            activeAsyncUpdateOperation[index] = targetToken;

            dataController.ItemViewAsyncUpdateOperation(index, GetActiveLineViewByDataIndex, targetToken.Token);
        }
    }
}