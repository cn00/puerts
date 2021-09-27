
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine.Events;

#if UNITY_EDITOR
namespace UnityEditor
{
    using System;
    using System.IO;
    using System.Linq;
    using UnityEngine;
    using UnityEditor;
    using System.Text.RegularExpressions;

    [CustomEditor(typeof(TsBehaviour))]
    public class TsBehaviourEditor : Editor
    {
        const string Tag = "TsBebaviourEdirot";
        TsBehaviour mTarget = null;
        string mTsText = "";
        private string assetPath;
        
        public void OnEnable()
        {
            mTarget = (TsBehaviour) target;

            if (mTarget.tsAsset)
            {
                assetPath = AssetDatabase.GetAssetPath(mTarget.tsAsset);
                mTsText = File.ReadAllText(assetPath);
                refreshAutoGen();
            }
        }

        static bool mShowInjections = true;
        static bool mShowInjectionValues = true;
        static bool mShowTsAutogens = true;
        string TsMemberDecl;
        string TsMemberValue;
        string AutoGenFunc;

        void refreshAutoGen()
        {
            AutoGenFunc = "";
            TsMemberDecl = "";
            TsMemberValue = "\n\tconstructor(mono:TsBehaviour) {\n\t\tthis.mono = mono;\n\t\tthis.gameObject = mono.gameObject;\n\t\tvar root = mono.gameObject;";
            foreach (var i in mTarget.Injections.Where(o => o != null && o.com != null))
            {
                var comType = i.com.GetComponents<Component>()[i.exportComIdx].GetType().ToString();
                var Tskey = i.com.name + "_" + comType.Substring(comType.LastIndexOf('.') + 1);
                if (i.com == mTarget.gameObject)
                {
                    Tskey = comType.Substring(comType.LastIndexOf('.') + 1);
                }
                
                TsMemberDecl += "\n\t" + Tskey + ":" +  comType + ";";

                TsMemberValue += $"\n\t\tthis.{Tskey} = <{comType}>mono.Injections.get_Item({mTarget.Injections.IndexOf(i)}).com;";
                // TsMemberValue += $"\n\t\tthis.{Tskey} = <{comType}>this.GetObjByPath(root,\"{GetChildPath(mTarget.transform, i.obj.transform)}\")?.GetComponent($typeof({comType}));";
                if (i.envMask > 0)
                {
                    var actions = getItemActions(i);
                    int flag = 0x01;
                    for (int j = 0; j < actions.Length; j++)
                    {
                        if ((flag & i.envMask)>0)
                        {
                            TsMemberValue += $"\n\t\t\tthis.{Tskey}.{actions[j]}.AddListener(this.{Tskey}_{actions[j]}.bind(this));";
                            if (!mTsText.Contains($"private {Tskey}_{actions[j]}("))
                            {
                                AutoGenFunc += $"\n\tprivate {Tskey}_{actions[j]}(){{console.log(this.gameObject, \"ts on {Tskey}_{actions[j]}\");}}";
                            }
                        }
                    }
                }
            }

            TsMemberValue += "\n\t\tthis.BindMono(mono);\n\t}";
        }

        static string[] getItemActions(TsBehaviour.Injection item)
        {
            var t = item.com.GetType();
            var facts = t.GetFields(System.Reflection.BindingFlags.Instance |
                                    System.Reflection.BindingFlags.Public)
                .Where(i =>
                {
                    var b = i.FieldType.BaseType.ToString().Contains("UnityEvent");
                    // Debug.Log($"field {i.Name}:{i.FieldType} {b}");
                    return b;
                }).Select(i=>i.Name);
            var pacts = t.GetProperties(System.Reflection.BindingFlags.Instance |
                                        System.Reflection.BindingFlags.Public | BindingFlags.SetProperty)
                .Where(i =>
                {
                    var b = i.PropertyType.BaseType.ToString().Contains("UnityEvent")
                                && null == i.GetAttribute<ObsoleteAttribute>();
                    // Debug.Log($"property {i.Name}:{i.PropertyType} {b}");
                    return b;
                }).Select(i=>i.Name);
            var actions = facts.Concat(pacts).ToArray();
            return actions;
        }

        static string GetChildPath(Transform root, Transform child)
        {
            string path = child.name;
            Transform obj = child;
            while (obj != root)
            {
                obj = obj.parent;
                path = $"{obj.name}/{path}";
            }
            return path;
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            assetPath = AssetDatabase.GetAssetPath(mTarget.tsAsset);
            
            #region Injections
            {
                if (mTarget.Injections == null)
                {
                    mTarget.Injections = new List<TsBehaviour.Injection>();
                }

                var size = mTarget.Injections.Count;
                EditorGUILayout.BeginHorizontal();
                {
                    mShowInjections = EditorGUILayout.Foldout(mShowInjections, "Injections", true);
                    size = EditorGUILayout.DelayedIntField(size);
                }
                EditorGUILayout.EndHorizontal();

                if(mTarget.Injections.Count != size)
                {
                    while (mTarget.Injections.Count < size)
                    {
                        mTarget.Injections.Add(new TsBehaviour.Injection());
                    }
                    if (mTarget.Injections.Count > size)
                    {
                        mTarget.Injections.RemoveRange(size, mTarget.Injections.Count-size);
                    }
                }

                if (mShowInjections)
                {
                    for (var i = 0; i < mTarget.Injections.Count; ++i)
                    {
                        var item = mTarget.Injections[i] ?? new TsBehaviour.Injection();
                        EditorGUILayout.BeginHorizontal();
                        {
                            item.obj = (GameObject) EditorGUILayout.ObjectField(item.obj, typeof(GameObject), true);
                            if (item.obj)
                            {
                                var obj = item.obj;
                                var nname = EditorGUILayout.DelayedTextField(
                                    Regex.Replace(obj.name, "[])(,. -+=]+", "_").TrimEnd('_'));
                                var coms = obj.GetComponents<Component>();
                                var comss = coms.Select(e =>
                                {
                                    var name = e.GetType().ToString();
                                    name = name.Substring(name.LastIndexOf('.') + 1);
                                    return name;
                                }).ToArray();
                                if (item.exportComIdx == -1 || item.exportComIdx >= comss.Length)
                                    item.exportComIdx = comss.Length - 1;
                                item.exportComIdx = EditorGUILayout.Popup(item.exportComIdx, comss);
                                item.com = coms[item.exportComIdx];
                                EditorGUILayout.ObjectField(item.com, item.com.GetType(), true);
                                
                                // action bind
                                var actions = getItemActions(item);
                                if(actions.Length > 0)item.envMask = EditorGUILayout.MaskField(item.envMask, actions);
                                else
                                {
                                    EditorGUILayout.DelayedTextField("");
                                }

                                if (obj.name != nname)
                                {
                                    //rename in Ts
                                    var comType = obj.GetComponents<Component>()[item.exportComIdx].GetType()
                                        .ToString();
                                    var comName = comType.Substring(comType.LastIndexOf('.') + 1);
                                    var oldExp = obj.name + "_" + comName;
                                    string newExp = nname + "_" + comName;
                                    mTsText = mTsText.Replace("." + oldExp, "." + newExp);
                                    obj.name = nname;
                                }
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            #endregion

            #region InjectValues
            {
                // if (mTarget.InjectValues == null)
                // {
                //     mTarget.InjectValues = new List<TsBehaviour.InjectValue>();
                // }
                //
                // var size = mTarget.InjectValues.Count;
                // EditorGUILayout.BeginHorizontal();
                // {
                //     mShowInjectionValues = EditorGUILayout.Foldout(mShowInjectionValues, "InjectionValues", true);
                //     size = EditorGUILayout.DelayedIntField(size);
                // }
                // EditorGUILayout.EndHorizontal();
                //
                // if(mTarget.InjectValues.Count != size)
                // {
                //     while (mTarget.InjectValues.Count < size)
                //     {
                //         mTarget.InjectValues.Add(new TsBehaviour.InjectValue());
                //     }
                //     if (mTarget.InjectValues.Count > size)
                //     {
                //         mTarget.InjectValues.RemoveRange(size, mTarget.InjectValues.Count-size);
                //     }
                // }
                //
                // if (mShowInjectionValues)
                // {
                //     for (var i = 0; i < mTarget.InjectValues.Count; ++i)
                //     {
                //         var item = mTarget.InjectValues[i] ?? new TsBehaviour.InjectValue();
                //         EditorGUILayout.BeginHorizontal();
                //         {
                //             item.k = EditorGUILayout.DelayedTextField(item.k);
                //             item.v = EditorGUILayout.DelayedTextField(item.v);
                //         }
                //         EditorGUILayout.EndHorizontal();
                //     }
                // }
            }            
            #endregion
            
            // gen Ts code
            if (GUI.changed)
            {
                mTarget.Injections.Sort((a, b) =>
                {
                    if (a.obj != null && b.obj != null)
                        return a.obj.name.CompareTo(b.obj.name);
                    else if (a.obj == null && b.obj != null)
                        return 1;
                    else
                        return -1;
                });
                refreshAutoGen();
                EditorUtility.SetDirty(mTarget);
            }
            var rect = EditorGUILayout.GetControlRect();
            var code = $"{AutoGenFunc}\n\t// AutoGen Begin{TsMemberDecl}\n{TsMemberValue}\n\t// AutoGen End";
            if (GUI.Button(rect, "Wtrite to ts"))
            {
                var pattern = Regex.Match(mTsText, "// AutoGen Begin(.|\r|\n)*// AutoGen End",
                    RegexOptions.Multiline).ToString();
                mTsText = mTsText.Replace(pattern, code);
                File.WriteAllText(assetPath, mTsText);
                Debug.Log(assetPath + " updated");
            }

            mShowTsAutogens = EditorGUILayout.Foldout(mShowTsAutogens, "TsAutogen", true);
            if (mShowTsAutogens)
                GUILayout.TextArea(code);

            // // Ts inspector
            // if (mTarget.Ts != null)
            // {
            //     mTarget.Ts.Draw();
            // }
        }
    }
}
#endif
