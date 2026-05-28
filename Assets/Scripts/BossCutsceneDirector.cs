using System.Collections;
using UnityEngine;

public class BossCutsceneDirector : MonoBehaviour
{
    [Header("Refs")]
    public Transform cameraTransform;
    public PatrickAI patrick;
    public MonoBehaviour movementController;
    public MonoBehaviour weapon;
    public Transform playerBody;

    [Header("Hide during cutscene")]
    public GameObject gunVisual;
    public GameObject crosshair;

    [Header("Framing")]
    public float lookAtHeight = 1.6f;
    public float framingDistance = 5f;
    public float framingHeight = 2.5f;

    [Header("Timing")]
    public float zoomInTime = 1.0f;
    public float holdTime = 8f;
    public float returnTime = 1.0f;

    private bool _played;

    public void Play()
    {
        if (_played) return;
        _played = true;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        if (movementController != null) movementController.enabled = false;
        if (weapon != null) weapon.enabled = false;
        if (gunVisual != null) gunVisual.SetActive(false);
        if (crosshair != null) crosshair.SetActive(false);

        Vector3 startLocalPos = cameraTransform.localPosition;
        Quaternion startLocalRot = cameraTransform.localRotation;
        Vector3 startWorldPos = cameraTransform.position;
        Quaternion startWorldRot = cameraTransform.rotation;

        Vector3 patrickPos = patrick.transform.position;
        Vector3 lookTarget = patrickPos + Vector3.up * lookAtHeight;

        Vector3 facing = patrick.transform.forward;
        Vector3 framePos = patrickPos + facing * framingDistance + Vector3.up * framingHeight;
        Quaternion frameRot = Quaternion.LookRotation((lookTarget - framePos).normalized);

        yield return Tween(startWorldPos, startWorldRot, framePos, frameRot, zoomInTime, true);

        float t = 0f;
        while (t < holdTime)
        {
            cameraTransform.position = framePos;
            cameraTransform.rotation = Quaternion.LookRotation((patrick.transform.position + Vector3.up * lookAtHeight - framePos).normalized);
            t += Time.deltaTime;
            yield return null;
        }

        if (playerBody != null)
        {
            Vector3 toPat = patrick.transform.position - playerBody.position; toPat.y = 0f;
            if (toPat.sqrMagnitude > 0.01f) playerBody.rotation = Quaternion.LookRotation(toPat);
        }

        Vector3 endWorldPos = playerBody != null ? playerBody.TransformPoint(startLocalPos) : startWorldPos;
        Quaternion endWorldRot = playerBody != null ? playerBody.rotation * startLocalRot : startWorldRot;
        yield return Tween(cameraTransform.position, cameraTransform.rotation, endWorldPos, endWorldRot, returnTime, true);

        cameraTransform.localPosition = startLocalPos;
        cameraTransform.localRotation = startLocalRot;
        if (movementController != null) movementController.enabled = true;
        if (weapon != null) weapon.enabled = true;
        if (gunVisual != null) gunVisual.SetActive(true);
        if (crosshair != null) crosshair.SetActive(true);

        patrick.BeginFight();
    }

    IEnumerator Tween(Vector3 fromP, Quaternion fromR, Vector3 toP, Quaternion toR, float dur, bool world)
    {
        float t = 0f;
        while (t < dur)
        {
            float u = Mathf.SmoothStep(0f, 1f, t / dur);
            cameraTransform.position = Vector3.Lerp(fromP, toP, u);
            cameraTransform.rotation = Quaternion.Slerp(fromR, toR, u);
            t += Time.deltaTime;
            yield return null;
        }
        cameraTransform.position = toP;
        cameraTransform.rotation = toR;
    }
}
