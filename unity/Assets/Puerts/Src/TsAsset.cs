using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class TsAsset : ScriptableObject
{
    public byte[] data;
    private string _text = null;
    public string text => Encoding.UTF8.GetString(data);
    public string tsName;
}
