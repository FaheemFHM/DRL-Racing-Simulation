using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Transform[] cameraPivots;
    private int carIndex;
    private Coroutine cameraCoroutine;
    [SerializeField] private float camMoveSpeed;
    [SerializeField] private float camRotSpeed;
    [SerializeField] [Range(0.1f, 2f)] private float cameraSwitchDuration = 0.5f;

    public void SetStartValues(Transform[] pivots, int index)
    {
        cameraPivots = pivots;
        carIndex = index;
        SetCamera();
    }

    public void SwitchToCar(int newIndex)
    {
        carIndex = newIndex;
        SetCamera();
    }

    void SetCamera()
    {
        if (cameraCoroutine != null) StopCoroutine(cameraCoroutine);
        cameraCoroutine = StartCoroutine(MoveCamera());
    }

    IEnumerator MoveCamera()
    {
        // Adds a smoother transition effect rather than snapping camera between agents
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float elapsed = 0f;
        while (elapsed < cameraSwitchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / cameraSwitchDuration;
            transform.position = Vector3.Lerp(startPos, cameraPivots[carIndex].position, t);
            transform.rotation = Quaternion.Slerp(startRot, cameraPivots[carIndex].rotation, t);
            yield return null;
        }
        transform.position = cameraPivots[carIndex].position;
        transform.rotation = cameraPivots[carIndex].rotation;
        transform.parent = cameraPivots[carIndex];
        cameraCoroutine = null;
    }
}
