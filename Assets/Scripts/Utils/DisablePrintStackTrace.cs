using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.Utils
{
    public class DisablePrintStackTrace : MonoBehaviour
    {
#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void DisableLogs()
        {
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        }
#endif
    }
}
