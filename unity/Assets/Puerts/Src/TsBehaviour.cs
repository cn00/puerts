using UnityEngine;
using Puerts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using PuertsStaticWrap;
using UnityEngine.UI;

public delegate JSObject ModuleInit(TsBehaviour mono);
//但从性能角度这并不是最佳实践，会导致过多的跨语言调用
public class TsBehaviour : MonoBehaviour
{
    public TsAsset tsAsset;
    [Serializable]
    public class Injection
    {
        public GameObject obj;
        public int exportComIdx = -1;
        public int envMask = 0;
        public Component com;
        #if UNITY_EDITOR
        public List<string> envList;
        #endif
    }
    [HideInInspector]
    public List<Injection> Injections;

    public Action JsAwake, JsStart;
    public Action JsUpdate;
    public Action JsOnDestroy;

    private static JsEnv _jsEnv = null;
    static JsEnv jsEnv {
        get {
            if (_jsEnv == null)
            {
                _jsEnv = new JsEnv(new DefaultLoader());
                // PuertsStaticWrap.AutoStaticCodeRegister.Register(_jsEnv);
                _jsEnv.AutoUsing();
            }
            return _jsEnv;
        }
    }//;
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Puerts/ResetJsEnv")]
    public static void ResetJsEnv()
    {
        jsEnv.Dispose();
        // jsEnv = null;
    }
    #endif

    public JSObject jsObject;
    void Awake()
    {
        // var sl = tsAsset.tsName.Replace(".js", "").Split('/');
        // var ts = $"var m = require ('{tsAsset.tsName.Replace(".js", "")}');m.{sl[sl.Length - 1]}_init;";
        // var init = jsEnv.Eval<ModuleInit>(ts, tsAsset.tsName);// this ok
        // Debug.Log($"TsBehavour init: [{ts}] jsObject:{jsObject}");
        var init = jsEnv.Eval<ModuleInit>(tsAsset.text, tsAsset.tsName);// TODO: use this solution

        if (init != null) 
            jsObject = init(this);
        Debug.Log($"TsBehavour Awake init:{init} jsObject:{jsObject}");
        if (JsAwake != null) JsAwake();
    }

    void Start()
    {
        if (JsStart != null) JsStart();
    }

    void Update()
    {
        jsEnv.Tick();
        if (JsUpdate != null) JsUpdate();
    }

    void OnDestroy()
    {
        if (JsOnDestroy != null) JsOnDestroy();
        JsStart = null;
        JsUpdate = null;
        JsOnDestroy = null;
    }

    public IEnumerator Coroutine()
    {
        yield return new WaitForSeconds(1);
        UnityEngine.Debug.Log("coroutine done");
    }
}

