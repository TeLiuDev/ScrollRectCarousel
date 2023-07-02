using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class AsyncFixedDimensionActivityPayload
{
  public string id;  
  public string icontext;
  public string icon_url;
}


[Serializable]
public class AsyncFixedDimensionPayload
{
    public List<AsyncFixedDimensionActivityPayload> activity_info;
}
