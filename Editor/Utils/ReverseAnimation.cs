using System.IO;
using UnityEditor;
using UnityEngine;

public static class ReverseAnimationContext {

    [MenuItem ("Assets/Create/Reversed Clip", false, 14)]
    private static void ReverseClip () {
        string directoryPath = Path.GetDirectoryName (AssetDatabase.GetAssetPath (Selection.activeObject));
        string fileName = Path.GetFileName (AssetDatabase.GetAssetPath (Selection.activeObject));
        string fileExtension = Path.GetExtension (AssetDatabase.GetAssetPath (Selection.activeObject));
        fileName = fileName.Split ('.') [0];

        string copiedFilePath = directoryPath + Path.DirectorySeparatorChar + fileName + "_Reversed" + fileExtension;
        var originalClip = GetSelectedClip ();

        AssetDatabase.CopyAsset (AssetDatabase.GetAssetPath (Selection.activeObject), copiedFilePath);

        var reversedClip = (AnimationClip) AssetDatabase.LoadAssetAtPath (copiedFilePath, typeof (AnimationClip));

        if (reversedClip == null)
            return;
        float clipLength = reversedClip.length;
        var curves = AnimationUtility.GetCurveBindings (reversedClip);
        reversedClip.ClearCurves ();

        foreach (EditorCurveBinding binding in curves) {
            AnimationCurve curve = AnimationUtility.GetEditorCurve (originalClip, binding);
            Keyframe[] keys = curve.keys;
            int keyCount = keys.Length;
            WrapMode postWrapmode = curve.postWrapMode;
            curve.postWrapMode = curve.preWrapMode;
            curve.preWrapMode = postWrapmode;
            for (int i = 0; i < keyCount; i++) {
                Keyframe K = keys[i];
                K.time = clipLength - K.time;
                float tmp = -K.inTangent;
                K.inTangent = -K.outTangent;
                K.outTangent = tmp;
                keys[i] = K;
            }
            curve.keys = keys;
            reversedClip.SetCurve (binding.path, binding.type, binding.propertyName, curve);
        }
        var events = AnimationUtility.GetAnimationEvents (reversedClip);
        if (events.Length > 0) {
            for (int i = 0; i < events.Length; i++) {
                events[i].time = clipLength - events[i].time;
            }
            AnimationUtility.SetAnimationEvents (reversedClip, events);
        }
        Debug.Log ("Animation reversed!");
    }

    [MenuItem ("Assets/Create/Reversed Clip", true)]
    static bool ReverseClipValidation () {
        return Selection.activeObject.GetType () == typeof (AnimationClip);
    }

    public static AnimationClip GetSelectedClip () {
        var clips = Selection.GetFiltered (typeof (AnimationClip), SelectionMode.Assets);
        if (clips.Length > 0) {
            return clips[0] as AnimationClip;
        }
        return null;
    }

}