using UnityEngine;
using Puerts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;

public delegate JSObject ModuleInit(TsBehaviour mono);
//但从性能角度这并不是最佳实践，会导致过多的跨语言调用
public class TsBehaviour : MonoBehaviour
{
    
    public class TsLoader : ILoader
    {
        private string mJsRoot;
        private string mAornJsRoot;

        public TsLoader(string jsRoot)
        {
            mJsRoot = jsRoot;
            mAornJsRoot = jsRoot + "Aorn/";
        }

        private bool isPuertsModule(string filePath){
            return filePath.StartsWith("puerts/");
        }

        public bool FileExists(string filePath)
        {
            string rootpath = isPuertsModule(filePath) ? mJsRoot : mAornJsRoot;
            string asset = string.Empty;
            string path = path = PathUnified(rootpath, filePath);
            var b = Resources.Load(path.Replace(".js", ""));
            return b != null;
        }

        public string ReadFile(string filePath, out string debugPath)
        {
            string rootpath = isPuertsModule(filePath) ? mJsRoot : mAornJsRoot; 
            debugPath = PathUnified(Application.dataPath, "Resources/", rootpath, filePath);
            #if UNITY_EDITOR_WIN
            debugPath = debugPath.Replace("/", "\\");
            #endif
            string path = PathUnified(rootpath, filePath);
            return ((TextAsset)Resources.Load(path.Replace(".js", ""))).text;
            return Resources.Load<TsAsset>(path.Replace(".js", "")).text;
        }

        private string PathUnified(params string[] args){
            return Path.Combine(args).Replace("\\","/");
        }
    }

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

    static JsEnv jsEnv => new JsEnv(new DefaultLoader());//;
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Puerts/ResetJsEnv")]
    public static void ResetJsEnv()
    {
        jsEnv.Dispose();
        // jsEnv = null;
    }
    #endif

    private JSObject jsObject;
    void Awake()
    {
        var sl = tsAsset.tsName.Replace(".js", "").Split('/');
        var ts = $"var m = require ('{tsAsset.tsName.Replace(".js", "")}');m.{sl[sl.Length - 1]}_init;";
        Debug.Log($"TsBehavour init: [{ts}]");
        // var init = jsEnv.Eval<ModuleInit>(ts, tsAsset.tsName);// this ok
        var init = jsEnv.Eval<ModuleInit>(tsAsset.text.Replace("export {};", ""), tsAsset.tsName);// TODO: use this solution

        if (init != null) 
            jsObject = init(this);
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

