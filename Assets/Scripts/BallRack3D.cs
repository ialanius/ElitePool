using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallRack3D : MonoBehaviour
{
    [Header("Old Refs & Settings (Do Not Change)")]
    public Transform rackPoint;
    public List<Transform> ballsTransforms;
    public Transform cueBall;

    [Header("Dimensions")]
    public float ballRadius = 0.25f;
    public float spacing = 0.01f;
    public bool rackPointsDown = false;
    public float centerXOffset = 0f;
    public float cueBallDistance = 5f;

    [Header("Safety Settings")]
    public float verticalSafetyOffset = 0.02f;

    [Header("Auto Radius")]
    public bool autoRadiusFromCueBall = false;

    [Header("Professional Features")]
    public bool animateRacking = true;
    public float animationDelay = 0.05f;
    public float rackDuration = 1.0f;
    public bool randomizeOrder = true;

    [Header("Timing Adjustments")]
    public float colliderEnableDelay = 0.5f;
    public float settlingTime = 0.3f;  // ✅ وقت إضافي للاستقرار

    [Header("Movement Lift")]
    public float liftHeight = 1.0f;

    [Header("Advanced Settings")]
    public bool forceExactPositions = true;  // ✅ إجبار المواقع الدقيقة
    public int positionEnforcementFrames = 5; // ✅ عدد الفريمات لتأكيد المواقع

    private List<Vector3> targetPositions = new List<Vector3>();

    void Start()
    {
        if (autoRadiusFromCueBall && cueBall)
            ballRadius = GetRadiusFromBall(cueBall, ballRadius);

        RackBalls();
    }

    public void RackBalls()
    {
        CalculatePositionsDynamic();

        if (randomizeOrder)
        {
            ShuffleBallsPreservingRules();
        }

        StopAllCoroutines();
        StartCoroutine(RackRoutine());
    }

    void CalculatePositionsDynamic()
    {
        targetPositions.Clear();
        int rows = 5;

        if (!rackPoint) return;

        Vector3 startPos = rackPoint.position;
        Vector3 backwardDir = -rackPoint.right;
        Vector3 rightDir = rackPoint.forward;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col <= row; col++)
            {
                float rowDepth = row * (ballRadius * 2f + spacing) * 0.866f;
                float rowWidth = row * (ballRadius * 2f + spacing);
                float colOffset = (col * (ballRadius * 2f + spacing)) - (rowWidth * 0.5f);

                Vector3 pos = startPos + (backwardDir * rowDepth) + (rightDir * colOffset);
                pos.y = rackPoint.position.y + ballRadius + verticalSafetyOffset;

                targetPositions.Add(pos);
            }
        }
    }

    void ShuffleBallsPreservingRules()
    {
        if (ballsTransforms == null || ballsTransforms.Count < 15) return;
        int index1 = -1, index8 = -1;
        for (int i = 0; i < ballsTransforms.Count; i++)
        {
            Ball3D b = ballsTransforms[i].GetComponent<Ball3D>();
            if (b) { if (b.number == 1) index1 = i; else if (b.number == 8) index8 = i; }
        }
        if (index1 != -1 && index1 != 0) { SwapBalls(0, index1); if (index8 == 0) index8 = index1; }
        if (index8 != -1 && index8 != 4) SwapBalls(4, index8);
        for (int i = 0; i < ballsTransforms.Count; i++)
        {
            if (i == 0 || i == 4) continue;
            int r = Random.Range(0, ballsTransforms.Count);
            while (r == 0 || r == 4 || r == i) r = Random.Range(0, ballsTransforms.Count);
            SwapBalls(i, r);
        }
    }
    void SwapBalls(int a, int b) { Transform t = ballsTransforms[a]; ballsTransforms[a] = ballsTransforms[b]; ballsTransforms[b] = t; }

    IEnumerator RackRoutine()
    {
        // ✅ 1. تجميد كامل وتعطيل كل شيء
        SetKinematicAll(true);
        SetCollidersEnabled(false);
        ResetAllVelocities();

        // ✅ 2. ضع الكرة البيضاء
        if (cueBall)
        {
            Vector3 forwardDir = rackPoint.right;
            cueBall.position = rackPoint.position + (forwardDir * cueBallDistance)
                               + Vector3.up * (ballRadius + verticalSafetyOffset);
            cueBall.rotation = Quaternion.identity;
        }

        // ✅ 3. حرك كل الكرات مع الانيميشن
        for (int i = 0; i < ballsTransforms.Count; i++)
        {
            if (i >= targetPositions.Count) break;
            Transform ball = ballsTransforms[i];
            Vector3 target = targetPositions[i];

            ball.gameObject.SetActive(true);
            Ball3D bScript = ball.GetComponent<Ball3D>();
            if (bScript) bScript.inPocket = false;

            if (animateRacking)
            {
                yield return new WaitForSeconds(animationDelay);
                StartCoroutine(MoveBallTo(ball, target, rackDuration));
            }
            else
            {
                ball.position = target;
                ball.rotation = Quaternion.identity;
            }
        }

        // ✅ 4. انتظر حتى تنتهي الانيميشنات
        if (animateRacking)
        {
            yield return new WaitForSeconds(rackDuration + settlingTime);
        }

        // ✅ 5. إجبار المواقع الدقيقة (مهم جداً!)
        if (forceExactPositions)
        {
            for (int frame = 0; frame < positionEnforcementFrames; frame++)
            {
                for (int i = 0; i < ballsTransforms.Count; i++)
                {
                    if (i >= targetPositions.Count) break;
                    ballsTransforms[i].position = targetPositions[i];
                    ballsTransforms[i].rotation = Quaternion.identity;
                }
                yield return new WaitForFixedUpdate();
            }
        }

        // ✅ 6. صفّر كل السرعات (والكرات لسه kinematic)
        ResetAllVelocities();

        // ✅ 7. انتظر فريمات إضافية
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // ✅ 8. تأخير إضافي قبل تفعيل الكولايدر
        if (colliderEnableDelay > 0)
        {
            yield return new WaitForSeconds(colliderEnableDelay);
        }

        // ✅ 9. فعّل الكولايدرات (الكرات في أماكنها الدقيقة)
        SetCollidersEnabled(true);

        // ✅ 10. انتظر حتى تتسجل الكولايدرات
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // ✅ 11. صفّر السرعات مرة أخرى
        ResetAllVelocities();

        // ✅ 12. انتظر فريم
        yield return new WaitForFixedUpdate();

        // ✅ 13. فك التجميد
        SetKinematicAll(false);

        // ✅ 14. انتظر فريمات
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // ✅ 15. صفّر السرعات مرة نهائية
        ResetAllVelocities();

        // ✅ 16. نوّم كل الكرات
        SleepAll();

        // ✅ 17. تأكيد نهائي على المواقع
        if (forceExactPositions)
        {
            yield return new WaitForFixedUpdate();
            for (int i = 0; i < ballsTransforms.Count; i++)
            {
                if (i >= targetPositions.Count) break;

                // لو الكرة انحرفت عن مكانها
                float distance = Vector3.Distance(ballsTransforms[i].position, targetPositions[i]);
                if (distance > 0.001f)
                {
                    ballsTransforms[i].position = targetPositions[i];
                    var rb = ballsTransforms[i].GetComponent<Rigidbody>();
                    if (rb)
                    {
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.Sleep();
                    }
                }
            }
        }

        Debug.Log("✅ Balls racked successfully - No overlap guaranteed!");
    }

    IEnumerator MoveBallTo(Transform ball, Vector3 target, float duration)
    {
        Vector3 startPos = ball.position;
        startPos.y = target.y + liftHeight;
        ball.position = startPos;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float curved = Mathf.SmoothStep(0, 1, t);
            ball.position = Vector3.Lerp(startPos, target, curved);
            yield return null;
        }

        // ✅ تأكيد نهائي
        ball.position = target;
    }

    // ================== Helpers ==================

    void SetKinematicAll(bool k)
    {
        if (cueBall) SetKinematic(cueBall, k);
        foreach (var b in ballsTransforms) SetKinematic(b, k);
    }

    void SetKinematic(Transform t, bool k)
    {
        if (t)
        {
            var rb = t.GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = k;
        }
    }

    void SetCollidersEnabled(bool enabled)
    {
        if (cueBall) EnableCollider(cueBall, enabled);
        foreach (var b in ballsTransforms) EnableCollider(b, enabled);
    }

    void EnableCollider(Transform t, bool enabled)
    {
        if (!t) return;
        var col = t.GetComponent<Collider>();
        if (col) col.enabled = enabled;
    }

    void ResetAllVelocities()
    {
        if (cueBall) ResetPhysicsSafe(cueBall);
        foreach (var b in ballsTransforms) ResetPhysicsSafe(b);
    }

    void ResetPhysicsSafe(Transform ball)
    {
        if (ball)
        {
            var rb = ball.GetComponent<Rigidbody>();
            if (rb && !rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    void SleepAll()
    {
        if (cueBall) SleepBall(cueBall);
        foreach (var b in ballsTransforms) SleepBall(b);
    }

    void SleepBall(Transform ball)
    {
        if (ball)
        {
            var rb = ball.GetComponent<Rigidbody>();
            if (rb && !rb.isKinematic)
            {
                rb.Sleep();
            }
        }
    }

    void WakeUpAll()
    {
        if (cueBall) WakeUpBall(cueBall);
        foreach (var b in ballsTransforms) WakeUpBall(b);
    }

    void WakeUpBall(Transform ball)
    {
        if (ball)
        {
            var rb = ball.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.WakeUp();
            }
        }
    }

    float GetRadiusFromBall(Transform ball, float fallback)
    {
        if (!ball) return fallback;
        var sc = ball.GetComponent<SphereCollider>();
        if (!sc) return fallback;
        float s = Mathf.Max(ball.lossyScale.x, Mathf.Max(ball.lossyScale.y, ball.lossyScale.z));
        return sc.radius * s;
    }

    void OnDrawGizmos()
    {
        if (!rackPoint) return;
        Gizmos.color = Color.yellow;
        Vector3 startPos = rackPoint.position;
        Vector3 backwardDir = -rackPoint.right;
        Vector3 rightDir = rackPoint.forward;

        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col <= row; col++)
            {
                float rowDepth = row * (ballRadius * 2f + spacing) * 0.866f;
                float rowWidth = row * (ballRadius * 2f + spacing);
                float colOffset = (col * (ballRadius * 2f + spacing)) - (rowWidth * 0.5f);
                Vector3 pos = startPos + (backwardDir * rowDepth) + (rightDir * colOffset);
                pos.y = rackPoint.position.y + ballRadius + verticalSafetyOffset;
                Gizmos.DrawWireSphere(pos, ballRadius);
            }
        }
    }
}