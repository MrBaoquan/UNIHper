using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UNIHper.UI;

namespace UNIHper.Editor
{
    public class CodeTemplateGenerator
    {
        const string bundleName = "com.parful.unihper";

        /// Inherits from EndNameAction, must override EndNameAction.Action
        public class DoCreateCodeFile : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                Object o = CreateScript(pathName, resourceFile);
                ProjectWindowUtil.ShowCreatedAsset(o);
            }
        }

        public class DoCreateUICodeFile : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                Object o = CreateScript(pathName, resourceFile);
                ProjectWindowUtil.ShowCreatedAsset(o);

                // var _scriptName = Path.GetFileNameWithoutExtension(pathName);

                // TextAsset _uiAsset = Resources.Load<TextAsset>("UNIHper/uis");
                // var _jsonObj = JsonConvert.DeserializeObject<
                //     Dictionary<string, Dictionary<string, UIConfig>>
                // >(_uiAsset.text);
                // var _sceneUIs = _jsonObj["Persistence"];
                // if (!_sceneUIs.ContainsKey(_scriptName))
                // {
                //     _sceneUIs.Add(
                //         Path.GetFileNameWithoutExtension(pathName),
                //         new UIConfig { Asset = _scriptName, ShowType = UIShowType.Normal }
                //     );
                //     var _content = JsonConvert.SerializeObject(
                //         _jsonObj,
                //         Formatting.Indented,
                //         new JsonSerializerSettings
                //         {
                //             DefaultValueHandling = DefaultValueHandling.Ignore
                //         }
                //     );
                //     File.WriteAllText(
                //         Path.Combine(Application.dataPath, "Resources/UNIHper/uis.json"),
                //         JToken.Parse(_content).ToString(Formatting.Indented)
                //     );
                //     AssetDatabase.SaveAssets();
                //     AssetDatabase.Refresh();
                // }

                //JsonUtility.FromJson (_uiAsset.text);
                // Debug.Log(pathName);
            }
        }

        private class DoCreateAssemblyDefinition : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                Object o = CreateScript(pathName, resourceFile);
                ProjectWindowUtil.ShowCreatedAsset(o);
            }
        }

        private const string REPLACABLE_NAME_TAG = "##CLASSNAME##";

        ///< <summary>NAME's replacement tag.</summary>
        private const string REPLACABLE_TABSPACE_TAG = "##TABSPACE##";

        ///< <summary>TABSPACE's replacement tag.</summary>

        /// <summary>C#'s Script Icon [The one MonoBhevaiour Scripts have].</summary>
        private static Texture2D scriptIcon = (
            EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D
        );

        private static Texture2D assemblyIcon = (
            EditorGUIUtility.IconContent("AssemblyDefinitionAsset Icon").image as Texture2D
        );

        /// <summary>Creates a new C# Class.</summary>
        [MenuItem("Assets/Create/📦 UNIHper Framework/SceneScript", priority = 51)]
        [MenuItem("UNIHper/Create/SceneScript", priority = 11)]
        private static void CreateSceneScript()
        {
            CreateCodeFileFromTemplate(
                "NewSceneScript.cs", // Class's temporal name.
                $@"Packages\{bundleName}\Editor\Templates\SceneScriptTemplate.txt" // Template's path.
            );
        }

        [MenuItem("Assets/Create/📦 UNIHper Framework/UIScript", priority = 52)]
        [MenuItem("UNIHper/Create/UIScript", priority = 12)]
        private static void CreateUIScript()
        {
            CreateCodeFileFromTemplate(
                "NewUI.cs",
                $@"Packages/{bundleName}/Editor/Templates/UIScriptTemplate.txt",
                ScriptableObject.CreateInstance<DoCreateUICodeFile>()
            );
        }

        [MenuItem("Assets/Create/📦 UNIHper Framework/ConfigScript", priority = 53)]
        [MenuItem("UNIHper/Create/ConfigScript", priority = 13)]
        private static void CreateConfigScript()
        {
            CreateCodeFileFromTemplate(
                "NewConfig.cs",
                $@"Packages\{bundleName}\Editor\Templates\ConfigScriptTemplate.txt"
            );
        }

        [MenuItem("Assets/Create/📦 UNIHper Framework/Game Assembly Definition", priority = 54)]
        [MenuItem("UNIHper/Create/Game Assembly Definition", priority = 14)]
        private static void CreateGameAssemblyDefinition()
        {
            CreateGameAssemblyDefinitionFromTemplate(
                "NewGame.asmdef",
                $@"Packages\{bundleName}\Editor\Templates\GameAssemblyDefinitionTemplate.txt"
            );
        }

        public static void CreateSceneScriptIfNotExists(string InScriptName)
        {
            string _relativePath = string.Format(
                "Assets/Develop/Scripts/{0}Script.cs",
                InScriptName
            );
            string _scriptPath = Path.GetFullPath(_relativePath);
            if (File.Exists(_scriptPath))
                return;

            var _directory = Path.GetDirectoryName(_scriptPath);
            if (!Directory.Exists(_directory))
            {
                Directory.CreateDirectory(_directory);
            }
            Object o = CreateScript(
                _relativePath,
                $@"Packages\{bundleName}\Editor\Templates\SceneScriptTemplate.txt"
            );
            ProjectWindowUtil.ShowCreatedAsset(o);
        }

        public static void CreateGameMainAssemblyIfNotExists()
        {
            string _relativePath = "Assets/Develop/Scripts/GameMain.asmdef";
            string _scriptPath = Path.GetFullPath(_relativePath);

            if (File.Exists(_scriptPath))
                return;

            var _directory = Path.GetDirectoryName(_scriptPath);
            if (!Directory.Exists(_directory))
            {
                Directory.CreateDirectory(_directory);
            }
            Object o = CreateScript(
                _relativePath,
                $@"Packages\{bundleName}\Editor\Templates\GameAssemblyDefinitionTemplate.txt"
            );
            ProjectWindowUtil.ShowCreatedAsset(o);
        }

        /// <summary>Creates Script from Template's path.</summary>
        internal static UnityEngine.Object CreateScript(string pathName, string templatePath)
        {
            /// Subtract spaces [" "].
            string className = NormalizeClassName(Path.GetFileNameWithoutExtension(pathName));
            string templateText = string.Empty;

            UTF8Encoding encoding = new UTF8Encoding(true, false);

            if (File.Exists(templatePath))
            {
                /// Read procedures.
                StreamReader reader = new StreamReader(templatePath);
                templateText = reader.ReadToEnd();
                reader.Close();

                templateText = templateText.Replace(REPLACABLE_NAME_TAG, className);
                templateText = templateText.Replace(REPLACABLE_TABSPACE_TAG, string.Empty);
                /// You can replace as many tags you make on your templates, just repeat Replace function
                /// e.g.:
                /// templateText = templateText.Replace("#NEWTAG#", "MyText");

                /// Write procedures.
                StreamWriter writer = new StreamWriter(Path.GetFullPath(pathName), false, encoding);
                writer.Write(templateText);
                writer.Close();

                AssetDatabase.ImportAsset(pathName);
                return AssetDatabase.LoadAssetAtPath(pathName, typeof(Object));
            }
            else
            {
                Debug.LogError(string.Format("The template file was not found: {0}", templatePath));
                return null;
            }
        }

        /// <summary>Creates a new code file from a template file.</summary>
        /// <param name="initialName">The initial name to give the file in the UI</param>
        /// <param name="templatePath">The full path of the template file to use</param>
        public static void CreateCodeFileFromTemplate(
            string initialName,
            string templatePath,
            EndNameEditAction endNameEditAction = null
        )
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                endNameEditAction ?? ScriptableObject.CreateInstance<DoCreateCodeFile>(),
                initialName,
                scriptIcon,
                templatePath
            );
        }

        public static void CreateGameAssemblyDefinitionFromTemplate(
            string initialName,
            string templatePath,
            EndNameEditAction endNameEditAction = null
        )
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                endNameEditAction ?? ScriptableObject.CreateInstance<DoCreateAssemblyDefinition>(),
                initialName,
                assemblyIcon,
                templatePath
            );
        }

        /// <summary>Subtracts white spaces [" "] for Class's name.</summary>
        private static string NormalizeClassName(string fileName)
        {
            return fileName.Replace(" ", string.Empty);
        }
    }
}
