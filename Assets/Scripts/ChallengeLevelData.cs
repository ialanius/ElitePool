using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewChallenge", menuName = "Pool/Challenge Level")]
public class ChallengeLevelData : ScriptableObject
{
    [Header("Level Info")]
    public string levelName = "Level 1";
    [TextArea] public string description = "Sink the 8-ball in 1 shot!";
    public int maxShots = 1; // عدد الضربات المسموحة

    [Header("Ball Setup")]
    public Vector3 cueBallPosition = new Vector3(-2, 0, 0); // مكان الكرة البيضاء
    public List<BallPosition> targetBalls; // قائمة بالكرات الأخرى

    [System.Serializable]
    public struct BallPosition
    {
        public Vector3 position;
        public BallType type; // (Solid, Stripe, Eight)
    }
}