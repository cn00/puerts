using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Codice.Client.BaseCommands;
using PlasticGui.WorkspaceWindow.ExternalTools;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

[ScriptedImporter(1, new []{"ts"})]
public class TsImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var ists = ctx.assetPath.EndsWith(".ts");
        var outPath = ists ? ctx.assetPath.Replace("Assets/abres/", "Assets/JsOut~/").Replace(".ts", ".js") : ctx.assetPath;
        // if(File.Exists(outPath)){
        //     File.Delete(outPath);
        // }
        
        TsImporter.Compile("bash", "-c 'export PATH=\"$PATH:/opt/homebrew/bin\";tsc;'");

        var tsAsset = ScriptableObject.CreateInstance<TsAsset>();
        if(File.Exists(outPath)){
            tsAsset.data = Encoding.UTF8.GetBytes(File.ReadAllText(outPath).Replace("export {};", "")
                .Replace("Object.defineProperty(exports, \"__esModule\", { value: true });", ""));
        }
        else
        {
            Debug.LogError($"{outPath} not generate.");
        }        
        tsAsset.tsName = outPath.Replace("Assets/JsOut~/", "");
        ctx.AddObjectToAsset("main obj", tsAsset, LoadIconTexture());
        ctx.SetMainObject(tsAsset);
    }
    private static Texture2D LoadIconTexture()
    {
        return AssetDatabase.LoadAssetAtPath("Assets/Puerts/Src/Editor/puerts-icon.png", typeof(Texture2D)) as
            Texture2D;
    }
    public static void Compile(string exe, string prmt = "", string WorkingDirectory = "./")
    {
        var process = new System.Diagnostics.Process();
        try
        {
            var pi = new System.Diagnostics.ProcessStartInfo(exe, prmt);
            pi.WorkingDirectory = WorkingDirectory;
            pi.RedirectStandardInput = false;
            pi.RedirectStandardOutput = true;
            pi.RedirectStandardError = true;
            pi.UseShellExecute = false;
            pi.CreateNoWindow = true;

            process.OutputDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                    return;
                UnityEngine.Debug.Log(e.Data);
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    UnityEngine.Debug.LogError(e.GetType() + ": " + e.Data);
            };
            process.Exited += (sender, e) =>
            {
                UnityEngine.Debug.Log($"{exe} {prmt} Exit");
            };

            process.StartInfo = pi;
            process.EnableRaisingEvents = true;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"msg:[{e.Message}] e:[{e}]");
        }

        // UnityEngine.Debug.Log("finished: " + process.ExitCode);
        EditorUtility.ClearProgressBar();
    }
}

public class ListenAllAssetImport:AssetPostprocessor
{
    public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        var tss = importedAssets.Where(i => i.EndsWith(".ts"));
        if (tss.Count() > 0)
        {
            // TsImporter.Compile("tsc");
            Debug.Log($"import ts: {string.Join(";", tss)}");
        }
    }
}

[CustomEditor(typeof(TsAsset))]
public class TsAssetEditor : UnityEditor.Editor
{
    private static bool decode = true;
    private TsAsset mTarget;
    private void OnEnable()
    {
        mTarget = target as TsAsset;
    }

    static string TemplatePath = UnityEditor.EditorApplication.applicationContentsPath + "/Resources/ScriptTemplates/89-TsBehaviour-NewTsBehaviour.ts.txt";
    [MenuItem("Puerts/UpdateTsTemplate")]
    public static void UpdateTsTemplate()
    {
        var distDir = TemplatePath;
        var s = File.ReadAllText("./Assets/Puerts/Src/Editor/TsBehaviour-Template.ts");
        File.WriteAllText(distDir, s);
    }

    public override void OnInspectorGUI()
    {
        GUI.enabled = true;
        
        EditorGUILayout.SelectableLabel(TemplatePath);
        if (GUI.Button(EditorGUILayout.GetControlRect(), "Copy/Update TsBehaviour template to Editor"))
        {
            UpdateTsTemplate();
        }
        if (GUI.Button(EditorGUILayout.GetControlRect(), "Open js file"))
        {
            var b = Unity.CodeEditor.CodeEditor.CurrentEditor.OpenProject($"Assets/JsOut~/{mTarget.tsName}", 1, 1);
            Debug.Log($"{Unity.CodeEditor.CodeEditor.CurrentEditorInstallation} open Assets/JsOut~/{mTarget.tsName} {b}");
        }

        // EditorGUILayout.LabelField("Import Config(重新导入时生效)");
        {
            // ++EditorGUI.indentLevel;
            // TsImporter.compile = EditorGUILayout.Toggle("compile(编译为字节码)", TsImporter.compile);
            // if (TsImporter.compile)
            // {
            //     ++EditorGUI.indentLevel;
            //     TsImporter.strip = EditorGUILayout.Toggle("strip", TsImporter.strip);
            //     --EditorGUI.indentLevel;
            // }
            //
            // TsImporter.encode = EditorGUILayout.Toggle("encode(加密)", TsImporter.encode);
            // --EditorGUI.indentLevel;
        }
        // EditorGUILayout.Space();
        
        // EditorGUILayout.LabelField("Display Config");
        EditorGUILayout.LabelField(mTarget.tsName);
        {
            // ++EditorGUI.indentLevel;
            // GUI.enabled = false;
            // EditorGUILayout.Toggle("encoded(加密)", mTarget.encode);
            // GUI.enabled = true;
            //
            // if(mTarget.encode)
            //     decode = EditorGUILayout.Toggle("decode", decode);
            // --EditorGUI.indentLevel;
        }
        
        var text = string.Empty;
        // if (mTarget.encode && decode)
        // {
        //     // TODO: your decode function
        //     text = Encoding.UTF8.GetString(Security.XXTEA.Decrypt(mTarget.data, TsAsset.TsDecodeKey));
        // }else
        {
            text = Encoding.UTF8.GetString(mTarget.data);
        }
        // var MaxTextPreviewLength = 4096;
        // if (text.Length > MaxTextPreviewLength + 3)
        // {
        //     text = text.Substring(0, MaxTextPreviewLength) + "...";
        // }

        GUIStyle style = "ScriptText";
        var rect = GUILayoutUtility.GetRect(new GUIContent(text), style);
        rect.x = 0f;
        rect.y -= 3f;
        rect.width = EditorGUIUtility.currentViewWidth + 1f;
        GUI.Box(rect, text, style);
    }
}
