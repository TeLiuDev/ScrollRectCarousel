using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AsyncFixedDimensionCarouselLineItemView : MonoBehaviour
{
    [SerializeField]
    private TMP_Text title;

    [SerializeField]
    private RawImage thumbnail;

    [SerializeField]
    private TMP_Text loadingText;

    public void UpdateView(string newTitle = null, Texture newThumbnail = null, bool? hideObject = null, bool? doLoading = null, bool? waitingToLoad = null)
    {
        if (hideObject != null)
        {
            gameObject.SetActive(!hideObject.Value);
        }        

        if (newTitle != null)
        {
            title.text = newTitle;
        }

        if (newThumbnail != null)
        {
            thumbnail.texture = newThumbnail;
        }
        
        if (doLoading != null)
        {
            if (doLoading == true)
            {                
                loadingText.text = "Waiting before load";

                if (waitingToLoad != null && waitingToLoad == true)
                {
                    loadingText.text = "Loading...";
                }
            }
            
            loadingText.gameObject.SetActive(doLoading.Value);
            thumbnail.gameObject.SetActive(doLoading.Value == false);
        }

        /*if (waitingToLoad != null)
        {            
            // Haven't loaded a image yet
            if (waitingToLoad == true && thumbnail.texture == null && doLoading.HasValue == true && doLoading.Value == true)
            {
                loadingText.text = "Waiting before load";
                loadingText.gameObject.SetActive(true);
                thumbnail.gameObject.SetActive(false);
            }            
        }*/

    }

    public void ReleaseThumbnail()
    {
        thumbnail = null;
    }
}
