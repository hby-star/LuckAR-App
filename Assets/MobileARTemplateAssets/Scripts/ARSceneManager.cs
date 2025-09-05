using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARSceneManager : MonoBehaviour
{
    ARSession session;

    void Awake()
    {
        session = FindObjectOfType<ARSession>();
    }

    public void OnBeforeSceneUnload()
    {
        if (session != null)
        {
            session.Reset(); // 重置 AR 会话
            session.enabled = false; // 停掉
        }
    }
}