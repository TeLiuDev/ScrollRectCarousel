using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class AsyncFixedDimensionActivityPayload
{
    public string icon_original;
    public string icontext;
}


[Serializable]
public class AsyncFixedDimensionPayload
{
    public Dictionary<string,AsyncFixedDimensionActivityPayload> activity_info;
}
