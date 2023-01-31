using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using com.te.liu.scrollrectcarousel;
using System.Threading.Tasks;
using System.Threading;
//using TMPro;


public class TestScrollRectCarouselLineItemView : MonoBehaviour, IScrollRectCarouselLineItemView
{
    [SerializeField]
    private RectTransform rectTransform;

    [SerializeField]
    private Text numberLabel;

    [SerializeField]
    private Text bodyLabel;

    [SerializeField]
    private float paddingLeft;

    [SerializeField]
    private float paddingRight;

    [SerializeField]
    private float paddingTop;

    [SerializeField]
    private float paddingBottom;

    [SerializeField]
    private float minWidth;
    [SerializeField]
    private float minHeight;

    [SerializeField]
    private Button button;

    private testData curData;

    private float curHeight;
    private float curWidth;


    [SerializeField]
    private bool verticalScroll;

    private void Awake()
    {
        CalcDimension();
    }

    private void CalcDimension()
    {

        if (verticalScroll == true)
        {
            curWidth = minWidth + paddingLeft + paddingRight;
            float preferredHeight = bodyLabel.preferredHeight;
            curHeight = Mathf.Max(minHeight, preferredHeight);
            bodyLabel.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, curHeight);
            curHeight += paddingTop + paddingBottom;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, curHeight);
        }
        else
        {
            curHeight = minHeight + paddingLeft + paddingRight;
            float preferredWidth = bodyLabel.preferredWidth;
            curWidth = Mathf.Max(minWidth, preferredWidth);
            numberLabel.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, curWidth);
            bodyLabel.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, curWidth);
            curWidth += paddingLeft + paddingRight;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, curWidth);
        }

    }

    public ScrollRectCarouselDataAbstract Data
    {
        get { return curData; }
    }

    public void Clean() { }

    public float Height
    {
        get { return curHeight; }
    }

    public float Width
    {
        get { return curWidth; }
    }

    public float MinHeight
    {
        get { return minHeight; }
    }

    public float MinWidth
    {
        get { return minWidth; }
    }

    public GameObject GameObject
    {
        get { return gameObject; }
    }

    public Button Button
    {
        get { return button; }
    }

    public RectTransform RectTran
    {
        get { return rectTransform; }
    }

    public void UpdateViewBaseOnModel(ScrollRectCarouselDataAbstract data)
    {
        curData = (data as testData);

        if (curData.count >= 0)
        {
            numberLabel.text = curData.count.ToString();
        }

        if (curData.description != null)
        {
            bodyLabel.text = curData.description;
        }

        if (button != null && button.image != null)
        {
            switch (curData.asyncOperationState)
            {
                case 0:
                    button.image.color = Color.gray;
                    break;

                case 1:
                    button.image.color = Color.red;
                    break;

                case 2:
                    button.image.color = Color.green;
                    break;
            }
        }

        if (curData.doClickedTransition.HasValue && curData.doClickedTransition.Value == true)
        {
            Color startColor = button.colors.normalColor;
            Color endColor = Color.blue;

            if (curData.isSelected == true)
            {
                StartCoroutine(TransitionButtonColor(startColor, endColor, .5f));
            }
            else
            {
                StartCoroutine(TransitionButtonColor(endColor, startColor, .5f));
            }
            curData.doClickedTransition = null;
        }

        CalcDimension();
    }

    private IEnumerator TransitionButtonColor(Color startColor, Color endColor, float fadeTime)
    {
        float curTime = 0;

        while (curTime < fadeTime)
        {
            curTime += Time.deltaTime;

            button.image.color = Color.Lerp(startColor, endColor, curTime / fadeTime);

            yield return null;
        }

    }

    public void SilentSelect()
    {
        if (button != null)
        {
            button.image.color = Color.blue;
        }
    }

    public void SilentDeselect()
    {

        if (button != null)
        {
            button.image.color = button.colors.normalColor;
        }

    }

}