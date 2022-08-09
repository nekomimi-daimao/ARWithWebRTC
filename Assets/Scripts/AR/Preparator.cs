using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

// ReSharper disable MergeIntoLogicalPattern

namespace AR
{
    public static class Preparator
    {
        public static async UniTask<bool> Check()
        {
            var permission = await CheckAllowed();
            if (!permission)
            {
                Debug.LogError($"{nameof(Preparator)} {nameof(Check)} no cameras are allowed");
                return false;
            }

            var ar = await CheckAR();
            if (!ar)
            {
                Debug.LogError($"{nameof(Preparator)} {nameof(Check)} cannot launch ARFouncation");
                return false;
            }

            return true;
        }

        #region Permission

        private static async UniTask<bool> CheckAllowed()
        {
#if UNITY_ANDROID
            if (UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                return true;
            }

            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
            // wait resume
            await UniTask.DelayFrame(30);
            return UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera);
#elif UNITY_IOS
            if (Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                return true;
            }

            await Application.RequestUserAuthorization(UserAuthorization.WebCam);
            return Application.HasUserAuthorization(UserAuthorization.WebCam);

#elif UNITY_EDITOR
            return true;
#endif
            return false;
        }

        #endregion

        #region ARFoundation

        private static async UniTask<bool> CheckAR()
        {
            await ARSession.CheckAvailability();

            if (ARSession.state == ARSessionState.Unsupported)
            {
                return false;
            }

            if (ARSession.state == ARSessionState.NeedsInstall)
            {
                await ARSession.Install();
                if (ARSession.state == ARSessionState.NeedsInstall)
                {
                    return false;
                }
            }

            return ARSession.state == ARSessionState.Ready
                   || ARSession.state == ARSessionState.SessionInitializing
                   || ARSession.state == ARSessionState.SessionTracking;
        }

        #endregion
    }
}
